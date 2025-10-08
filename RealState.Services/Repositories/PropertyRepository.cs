using MongoDB.Bson;
using MongoDB.Driver;
using RealState.Data.Entities;
using RealState.Data.Persistence;
using RealState.Services.Abstractions;
using RealState.Services.Dtos;

namespace RealState.Services.Repositories
{

    /// <summary>
    /// Repository for querying and projecting <see cref="Property"/> documents
    /// from MongoDB into lightweight <see cref="PropertyDto"/> results,
    /// including filtering and pagination capabilities.
    /// </summary>
    public class PropertyRepository : IPropertyRepository
    {
        private const string CollectionName = "properties";
        private readonly IMongoCollection<Property> _collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyRepository"/> class.
        /// </summary>
        /// <param name="context">An <see cref="IMongoContext"/> used to access MongoDB collections.</param>
        public PropertyRepository(IMongoContext context)
        {
            _collection = context.GetCollection<Property>(CollectionName);
            Console.WriteLine($"[MongoDB] Conectado a la colección: {CollectionName}");
        }


        /// <summary>
        /// Retrieves a paged list of properties as <see cref="PropertyDto"/> using optional filters.
        /// </summary>
        /// <param name="name">Optional case-insensitive regex filter applied to the property name.</param>
        /// <param name="address">Optional case-insensitive regex filter applied to the address.</param>
        /// <param name="minPrice">Optional lower bound for property price (inclusive).</param>
        /// <param name="maxPrice">Optional upper bound for property price (inclusive).</param>
        /// <param name="page">1-based page index. Defaults to 1.</param>
        /// <param name="pageSize">Number of items per page. Defaults to 10.</param>
        /// <returns>
        /// A <see cref="PagedResult{T}"/> containing the current page of <see cref="PropertyDto"/>,
        /// total count, page index, and page size.
        /// </returns>
        /// <remarks>
        /// This method builds a composable MongoDB filter, counts matching documents for pagination,
        /// then projects only the fields required by <see cref="PropertyDto"/>.
        /// Robust parsing helpers are used to protect against inconsistent types in stored documents.
        /// </remarks>
        public async Task<PagedResult<PropertyDto>> GetPropertiesAsync(
            string? name = null,
            string? address = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int page = 1,
            int pageSize = 10)
        {
            var fb = Builders<Property>.Filter;
            var filters = new List<FilterDefinition<Property>>();

            if (!string.IsNullOrWhiteSpace(name))
                filters.Add(fb.Regex(p => p.Name, new BsonRegularExpression(name, "i")));

            if (!string.IsNullOrWhiteSpace(address))
                filters.Add(fb.Regex(p => p.Address, new BsonRegularExpression(address, "i")));

            if (minPrice.HasValue)
                filters.Add(fb.Gte(p => p.Price, minPrice.Value));

            if (maxPrice.HasValue)
                filters.Add(fb.Lte(p => p.Price, maxPrice.Value));

            var filter = filters.Count == 0 ? FilterDefinition<Property>.Empty : fb.And(filters);

            // Conteo total para la paginación
            var totalCount = await _collection.CountDocumentsAsync(filter);

            // Proyección robusta: saca los campos anidados y controla tipos
            var projection = new BsonDocument
            {
                { "IdOwner", "$Owner.Id" },
                { "Name", "$Name" },
                { "Address", "$Address" },
                { "Price", "$Price" },
                { "Image", "$Image.File" }
            };

            var raw = await _collection
                .Find(filter)
                .Project<BsonDocument>(projection)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            // Helpers de parseo seguro
            static string? GetString(BsonDocument d, string field)
            {
                if (!d.TryGetValue(field, out var v) || v.IsBsonNull) return null;
                if (v.IsString) return v.AsString;
                if (v.IsObjectId) return v.AsObjectId.ToString();
                if (v.IsGuid) return v.AsGuid.ToString();
                return v.ToString();
            }

            static decimal GetDecimal(BsonDocument d, string field)
            {
                if (!d.TryGetValue(field, out var v) || v.IsBsonNull) return 0m;
                if (v.IsDecimal128) return Decimal128.ToDecimal(v.AsDecimal128);
                if (v.IsDouble) return (decimal)v.AsDouble;
                if (v.IsInt64) return v.AsInt64;
                if (v.IsInt32) return v.AsInt32;
                if (v.IsString && decimal.TryParse(v.AsString, out var parsed)) return parsed;
                return 0m;
            }

            var data = raw.Select(doc => new PropertyDto
            {
                IdOwner = GetString(doc, "IdOwner"),
                Name = GetString(doc, "Name") ?? string.Empty,
                Address = GetString(doc, "Address") ?? string.Empty,
                Price = GetDecimal(doc, "Price"),
                Image = GetString(doc, "Image")
            }).ToList();

            return new PagedResult<PropertyDto>
            {
                Data = data,
                TotalCount = (int)totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Retrieves a single property by its string identifier and maps it to <see cref="PropertyDto"/>.
        /// </summary>
        /// <param name="id">The string identifier of the property document.</param>
        /// <returns>
        /// A populated <see cref="PropertyDto"/> if found; otherwise <c>null</c>.
        /// </returns>
        /// <remarks>
        /// This method intentionally reads as a raw <see cref="BsonDocument"/> to tolerate
        /// inconsistent field types and nested shapes (e.g., Owner.Id being a string or ObjectId).
        /// </remarks>
        public async Task<PropertyDto?> GetByIdAsync(string id)
        {
            var filter = Builders<Property>.Filter.Eq(p => p.Id, id);

            var raw = await _collection.Find(filter).As<BsonDocument>().FirstOrDefaultAsync();

            if (raw is null) return null;

            string? idOwner = null;
            if (raw.TryGetValue("Owner", out var ownerVal) && ownerVal.IsBsonDocument)
            {
                var ownerDoc = ownerVal.AsBsonDocument;

                if (ownerDoc.TryGetValue("Id", out var idVal))
                    idOwner = idVal.IsString ? idVal.AsString :
                              idVal.IsObjectId ? idVal.AsObjectId.ToString() :
                              idVal.ToString();
                else if (ownerDoc.TryGetValue("_id", out var oidVal))
                    idOwner = oidVal.IsObjectId ? oidVal.AsObjectId.ToString() : oidVal.ToString();
            }

            decimal price = 0m;
            if (raw.TryGetValue("Price", out var pv))
            {
                if (pv.IsDecimal128) price = Decimal128.ToDecimal(pv.AsDecimal128);
                else if (pv.IsInt64) price = pv.AsInt64;
                else if (pv.IsInt32) price = pv.AsInt32;
                else if (pv.IsDouble) price = (decimal)pv.AsDouble;
                else if (pv.IsString && decimal.TryParse(pv.AsString, out var dec))
                    price = dec;
            }

            string? imageFile = null;
            if (raw.TryGetValue("Image", out var imgVal) && imgVal.IsBsonDocument)
            {
                var imgDoc = imgVal.AsBsonDocument;
                if (imgDoc.TryGetValue("File", out var fileVal) && fileVal.IsString)
                    imageFile = fileVal.AsString;
            }

            var dto = new PropertyDto
            {
                IdOwner = idOwner ?? string.Empty,
                Name = raw.GetValue("Name", "").AsString,
                Address = raw.GetValue("Address", "").AsString,
                Price = price,
                Image = imageFile
            };

            return dto;
        }
    }
}