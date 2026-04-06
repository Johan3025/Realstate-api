using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using RealState.Data.Entities;
using RealState.Data.Persistence;
using RealState.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace RealState.Tests
{
    public class PropertyRepositoryTests
    {
        // -------------------- Helpers --------------------

        private static Mock<IMongoCollection<Property>> CreateCollectionMock()
        {
            var mock = new Mock<IMongoCollection<Property>>();
            mock.SetupGet(c => c.CollectionNamespace)
                .Returns(new CollectionNamespace(new DatabaseNamespace("db"), "properties"));
            mock.SetupGet(c => c.DocumentSerializer)
                .Returns(MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry.GetSerializer<Property>());
            mock.SetupGet(c => c.Settings).Returns(new MongoCollectionSettings());
            return mock;
        }

        private static PropertyRepository BuildRepository(
            Mock<IMongoCollection<Property>> collectionMock)
        {
            var ctx = new Mock<IMongoContext>();

            // Tu IMongoContext expone GetCollection<T>(string name) con 1 parámetro
            ctx.Setup(c => c.GetCollection<Property>(It.IsAny<string>()))
               .Returns(collectionMock.Object);

            return new PropertyRepository(ctx.Object);
        }

        /// <summary>
        /// Crea un IAsyncCursor<BsonDocument> que devuelve una sola tanda con "items".
        /// Si "items" está vacío, MoveNext/MoveNextAsync devuelven false de entrada.
        /// </summary>
        private static Mock<IAsyncCursor<BsonDocument>> CreateCursor(params BsonDocument[] items)
        {
            var cursor = new Mock<IAsyncCursor<BsonDocument>>();
            var firstBatchReturned = false;

            cursor.Setup(c => c.Current)
                  .Returns(() => (IEnumerable<BsonDocument>)items);

            cursor.Setup(c => c.MoveNext(It.IsAny<CancellationToken>()))
                  .Returns(() =>
                  {
                      if (firstBatchReturned || items.Length == 0) return false;
                      firstBatchReturned = true;
                      return true;
                  });

            cursor.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(() =>
                  {
                      if (firstBatchReturned || items.Length == 0) return false;
                      firstBatchReturned = true;
                      return true;
                  });

            return cursor;
        }

        // -------------------- Tests --------------------

        [Fact]
        public async Task GetPropertiesAsync_NoFilters_MapsAndPagesCorrectly()
        {
            // Arrange
            var collectionMock = CreateCollectionMock();

            // CountDocumentsAsync
            collectionMock
                .Setup(c => c.CountDocumentsAsync(
                    It.IsAny<FilterDefinition<Property>>(),
                    (CountOptions?)null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(2);

            // Docs proyectados (lo que devuelve tu proyección a BsonDocument)
            var docs = new[]
            {
                    new BsonDocument
                    {
                        { "IdOwner", ObjectId.GenerateNewId() },
                        { "Name", "Loft A" },
                        { "Address", "Main St 123" },
                        { "Price", 125000 },
                        { "Image", "a.jpg" }
                    },
                    new BsonDocument
                    {
                        { "IdOwner", "owner-2" },
                        { "Name", "House B" },
                        { "Address", "2nd Ave 456" },
                        { "Price", new Decimal128(299999.99m) },
                        { "Image", BsonNull.Value }
                    }
                };

            var cursor = CreateCursor(docs);
            collectionMock
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Property>>(),
                    It.IsAny<FindOptions<Property, BsonDocument>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(cursor.Object);

            var repo = BuildRepository(collectionMock);

            // Act
            var result = await repo.GetPropertiesAsync(page: 1, pageSize: 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);
            Assert.Equal(2, result.Data.Count);

            var first = result.Data[0];
            Assert.False(string.IsNullOrWhiteSpace(first.IdOwner));
            Assert.Equal("Loft A", first.Name);
            Assert.Equal("Main St 123", first.Address);
            Assert.Equal(125000m, first.Price);
            Assert.Equal("a.jpg", first.Image);

            var second = result.Data[1];
            Assert.Equal("owner-2", second.IdOwner);
            Assert.Equal("House B", second.Name);
            Assert.Equal("2nd Ave 456", second.Address);
            Assert.Equal(299999.99m, second.Price);
            Assert.Null(second.Image);

            collectionMock.Verify(c => c.CountDocumentsAsync(
                It.IsAny<FilterDefinition<Property>>(),
                (CountOptions?)null,
                It.IsAny<CancellationToken>()), Times.Once);

            collectionMock.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<Property>>(),
                It.IsAny<FindOptions<Property, BsonDocument>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetPropertiesAsync_WithFilters_BuildsFindAndRespectsBounds()
        {
            // Arrange
            var collectionMock = CreateCollectionMock();

            collectionMock
                .Setup(c => c.CountDocumentsAsync(
                    It.IsAny<FilterDefinition<Property>>(),
                    (CountOptions?)null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var docs = new[]
            {
                    new BsonDocument
                    {
                        { "IdOwner", "o1" },
                        { "Name", "Nice Flat" },
                        { "Address", "Evergreen 742" },
                        { "Price", 1500 },
                        { "Image", "flat.jpg" }
                    }
                };

            var cursor = CreateCursor(docs);
            collectionMock
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Property>>(),
                    It.Is<FindOptions<Property, BsonDocument>>(o =>
                        // Validamos que se haya aplicado paginación en opciones
                        o.Skip == 5 && o.Limit == 5),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(cursor.Object);

            var repo = BuildRepository(collectionMock);

            // Act
            var result = await repo.GetPropertiesAsync(
                name: "nice",
                address: "evergreen",
                minPrice: 1000,
                maxPrice: 2000,
                page: 2,
                pageSize: 5);

            // Assert
            Assert.Equal(1, result.TotalCount);
            Assert.Equal(2, result.Page);
            Assert.Equal(5, result.PageSize);
            Assert.Single(result.Data);
            Assert.Equal("o1", result.Data[0].IdOwner);

            collectionMock.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<Property>>(),
                It.Is<FindOptions<Property, BsonDocument>>(o => o.Skip == 5 && o.Limit == 5),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenFound_MapsTypesDefensively()
        {
            // Arrange
            var collectionMock = CreateCollectionMock();

            var oid = ObjectId.GenerateNewId();
            var doc = new BsonDocument
                {
                    { "Owner", new BsonDocument { { "Id", oid } } },
                    { "Name", "Penthouse" },
                    { "Address", "Sunset Blvd 10" },
                    { "Price", new Decimal128(1234567.89m) },
                    { "Image", new BsonDocument { { "File", "pent.jpg" } } }
                };

            var cursor = CreateCursor(doc);
            collectionMock
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Property>>(),
                    It.Is<FindOptions<Property, BsonDocument>>(o => o.Limit == 1),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(cursor.Object);

            var repo = BuildRepository(collectionMock);

            // Act
            var result = await repo.GetByIdAsync("some-id");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(oid.ToString(), result!.IdOwner);
            Assert.Equal("Penthouse", result.Name);
            Assert.Equal("Sunset Blvd 10", result.Address);
            Assert.Equal(1234567.89m, result.Price);
            Assert.Equal("pent.jpg", result.Image);

            collectionMock.Verify(c => c.FindAsync(
                It.IsAny<FilterDefinition<Property>>(),
                It.Is<FindOptions<Property, BsonDocument>>(o => o.Limit == 1),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
        {
            // Arrange
            var collectionMock = CreateCollectionMock();

            // Cursor vacío: MoveNext/MoveNextAsync devolverán false desde el inicio
            var emptyCursor = CreateCursor(Array.Empty<BsonDocument>());
            collectionMock
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Property>>(),
                    It.IsAny<FindOptions<Property, BsonDocument>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(emptyCursor.Object);

            var repo = BuildRepository(collectionMock);

            // Act
            var result = await repo.GetByIdAsync("does-not-exist");

            // Assert
            Assert.Null(result);
        }
    }
}
