using Microsoft.AspNetCore.Mvc;
using RealState.Services.Abstractions;
using RealState.Services.Dtos;
using System.Net;

namespace RealState.Api.Controllers
{
    /// <summary>
    /// Exposes endpoints for retrieving property listings and detail.
    /// Thin controller: delegates all logic to <see cref="IPropertyService"/>.
    /// Exceptions are handled globally by <c>ExceptionHandlingMiddleware</c>.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PropertiesController : ControllerBase
    {
        private readonly IPropertyService _propertyService;

        public PropertiesController(IPropertyService propertyService)
        {
            _propertyService = propertyService;
        }

        /// <summary>
        /// Retrieves a paginated list of properties based on provided filters.
        /// </summary>
        /// <param name="name">Partial property name (optional).</param>
        /// <param name="address">Partial property address (optional).</param>
        /// <param name="minPrice">Minimum property price (optional).</param>
        /// <param name="maxPrice">Maximum property price (optional).</param>
        /// <param name="page">Page number (default is 1).</param>
        /// <param name="pageSize">Number of items per page (default 10, max 100).</param>
        /// <returns>A paginated list of properties matching the filter criteria.</returns>
        /// <response code="200">Properties retrieved successfully.</response>
        /// <response code="400">Invalid query parameters.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<PropertyDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetProperties(
            [FromQuery] string? name,
            [FromQuery] string? address,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _propertyService.GetPropertiesAsync(name, address, minPrice, maxPrice, page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves a specific property by its unique identifier.
        /// </summary>
        /// <param name="id">The property unique ID.</param>
        /// <returns>Property details.</returns>
        /// <response code="200">Property found successfully.</response>
        /// <response code="400">Invalid or missing property ID.</response>
        /// <response code="404">No property found with the specified ID.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PropertyDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetPropertyById(string id)
        {
            var property = await _propertyService.GetByIdAsync(id);
            return Ok(property);
        }
    }
}
