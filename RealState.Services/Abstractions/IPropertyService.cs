using RealState.Services.Dtos;

namespace RealState.Services.Abstractions
{
    /// <summary>
    /// Defines application-level operations for property retrieval.
    /// Implementations handle validation, logging, and delegation to the repository.
    /// </summary>
    public interface IPropertyService
    {
        /// <summary>
        /// Returns a paginated list of properties matching the given filters.
        /// </summary>
        /// <exception cref="Exceptions.ValidationException">
        /// Thrown when pagination parameters are invalid or price range is inconsistent.
        /// </exception>
        Task<PagedResult<PropertyDto>> GetPropertiesAsync(
            string? name,
            string? address,
            decimal? minPrice,
            decimal? maxPrice,
            int page,
            int pageSize);

        /// <summary>
        /// Returns the property with the given identifier.
        /// </summary>
        /// <exception cref="Exceptions.ValidationException">Thrown when <paramref name="id"/> is null or empty.</exception>
        /// <exception cref="Exceptions.NotFoundException">Thrown when no property is found with the given id.</exception>
        Task<PropertyDto> GetByIdAsync(string id);
    }
}
