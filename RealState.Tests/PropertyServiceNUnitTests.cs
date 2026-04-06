using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using RealState.Services.Abstractions;
using RealState.Services.Dtos;
using RealState.Services.Exceptions;
using RealState.Services.Services;

namespace RealState.Tests
{
    /// <summary>
    /// NUnit tests for <see cref="PropertyService"/>.
    /// Covers input validation and delegation to the repository.
    /// </summary>
    [TestFixture]
    public class PropertyServiceNUnitTests
    {
        private Mock<IPropertyRepository> _repoMock = default!;
        private PropertyService _sut = default!;

        [SetUp]
        public void SetUp()
        {
            _repoMock = new Mock<IPropertyRepository>();
            _sut = new PropertyService(_repoMock.Object, NullLogger<PropertyService>.Instance);
        }

        // ── GetPropertiesAsync ────────────────────────────────────────────────

        [Test]
        public async Task GetPropertiesAsync_ValidParams_DelegatesToRepository()
        {
            // Arrange
            var expected = new PagedResult<PropertyDto>
            {
                Data = [new PropertyDto { Id = "abc", Name = "House A", Address = "St 1", Price = 100000m }],
                TotalCount = 1,
                Page = 1,
                PageSize = 10
            };

            _repoMock
                .Setup(r => r.GetPropertiesAsync("house", null, null, null, 1, 10))
                .ReturnsAsync(expected);

            // Act
            var result = await _sut.GetPropertiesAsync("house", null, null, null, 1, 10);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TotalCount, Is.EqualTo(1));
            Assert.That(result.Data[0].Name, Is.EqualTo("House A"));
            _repoMock.Verify(r => r.GetPropertiesAsync("house", null, null, null, 1, 10), Times.Once);
        }

        [Test]
        public void GetPropertiesAsync_PageZero_ThrowsValidationException()
        {
            var ex = Assert.ThrowsAsync<ValidationException>(
                () => _sut.GetPropertiesAsync(null, null, null, null, page: 0, pageSize: 10));

            Assert.That(ex!.Message, Does.Contain("greater than zero"));
        }

        [Test]
        public void GetPropertiesAsync_PageSizeOver100_ThrowsValidationException()
        {
            var ex = Assert.ThrowsAsync<ValidationException>(
                () => _sut.GetPropertiesAsync(null, null, null, null, page: 1, pageSize: 101));

            Assert.That(ex!.Message, Does.Contain("between 1 and 100"));
        }

        [Test]
        public void GetPropertiesAsync_MinPriceGreaterThanMaxPrice_ThrowsValidationException()
        {
            var ex = Assert.ThrowsAsync<ValidationException>(
                () => _sut.GetPropertiesAsync(null, null, minPrice: 500m, maxPrice: 100m, page: 1, pageSize: 10));

            Assert.That(ex!.Message, Does.Contain("cannot be greater"));
        }

        [Test]
        public async Task GetPropertiesAsync_NoFilters_ReturnsRepositoryResult()
        {
            // Arrange
            var expected = new PagedResult<PropertyDto> { Data = [], TotalCount = 0, Page = 1, PageSize = 10 };
            _repoMock
                .Setup(r => r.GetPropertiesAsync(null, null, null, null, 1, 10))
                .ReturnsAsync(expected);

            // Act
            var result = await _sut.GetPropertiesAsync(null, null, null, null, 1, 10);

            // Assert
            Assert.That(result.TotalCount, Is.EqualTo(0));
            Assert.That(result.Data, Is.Empty);
        }

        // ── GetByIdAsync ──────────────────────────────────────────────────────

        [Test]
        public async Task GetByIdAsync_ExistingId_ReturnsDto()
        {
            // Arrange
            var dto = new PropertyDto { Id = "507f1f77bcf86cd799439011", Name = "Loft", Address = "Main St", Price = 250000m };
            _repoMock.Setup(r => r.GetByIdAsync("507f1f77bcf86cd799439011")).ReturnsAsync(dto);

            // Act
            var result = await _sut.GetByIdAsync("507f1f77bcf86cd799439011");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo("507f1f77bcf86cd799439011"));
            Assert.That(result.Name, Is.EqualTo("Loft"));
        }

        [Test]
        public void GetByIdAsync_NullId_ThrowsValidationException()
        {
            var ex = Assert.ThrowsAsync<ValidationException>(
                () => _sut.GetByIdAsync(null!));

            Assert.That(ex!.Message, Does.Contain("required"));
        }

        [Test]
        public void GetByIdAsync_WhitespaceId_ThrowsValidationException()
        {
            var ex = Assert.ThrowsAsync<ValidationException>(
                () => _sut.GetByIdAsync("   "));

            Assert.That(ex!.Message, Does.Contain("required"));
        }

        [Test]
        public void GetByIdAsync_IdNotFound_ThrowsNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync("nonexistent")).ReturnsAsync((PropertyDto?)null);

            var ex = Assert.ThrowsAsync<NotFoundException>(
                () => _sut.GetByIdAsync("nonexistent"));

            Assert.That(ex!.Message, Does.Contain("nonexistent"));
        }
    }
}
