using RealState.Services.Dtos;

namespace RealState.Services.Abstractions
{
    public interface IPropertyRepository
    {
        Task<PagedResult<PropertyDto>> GetPropertiesAsync(string? name = null, string? address = null,
                                                decimal? minPrice = null, decimal? maxPrice = null, int page = 0, int pageSize = 0);

        Task<PropertyDto?> GetByIdAsync(string id);
    }
}
