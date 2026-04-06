using Microsoft.Extensions.Logging;
using RealState.Services.Abstractions;
using RealState.Services.Dtos;
using RealState.Services.Exceptions;

namespace RealState.Services.Services
{
    /// <summary>
    /// Application service that orchestrates property queries.
    /// Responsible for input validation, logging and delegation to <see cref="IPropertyRepository"/>.
    /// </summary>
    public sealed class PropertyService : IPropertyService
    {
        private readonly IPropertyRepository _repository;
        private readonly ILogger<PropertyService> _logger;

        public PropertyService(IPropertyRepository repository, ILogger<PropertyService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<PagedResult<PropertyDto>> GetPropertiesAsync(
            string? name,
            string? address,
            decimal? minPrice,
            decimal? maxPrice,
            int page,
            int pageSize)
        {
            if (page <= 0)
                throw new ValidationException("Page number must be greater than zero.");

            if (pageSize <= 0 || pageSize > 100)
                throw new ValidationException("Page size must be between 1 and 100.");

            if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
                throw new ValidationException("Minimum price cannot be greater than maximum price.");

            _logger.LogInformation(
                "Fetching properties — name:{Name} address:{Address} price:[{Min},{Max}] page:{Page}/{PageSize}",
                name, address, minPrice, maxPrice, page, pageSize);

            return await _repository.GetPropertiesAsync(name, address, minPrice, maxPrice, page, pageSize);
        }

        /// <inheritdoc/>
        public async Task<PropertyDto> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ValidationException("The property ID is required.");

            _logger.LogInformation("Fetching property by id: {Id}", id);

            var property = await _repository.GetByIdAsync(id);

            if (property is null)
            {
                _logger.LogWarning("Property not found: {Id}", id);
                throw new NotFoundException($"No property found with ID '{id}'.");
            }

            return property;
        }
    }
}
