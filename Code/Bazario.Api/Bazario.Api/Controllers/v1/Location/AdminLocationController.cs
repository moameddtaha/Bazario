using Asp.Versioning;
using Bazario.Core.DTO.Location.City;
using Bazario.Core.DTO.Location.Country;
using Bazario.Core.DTO.Location.Governorate;
using Bazario.Core.ServiceContracts.Location;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bazario.Api.Controllers.v1.Location
{
    /// <summary>
    /// Admin API for managing countries, governorates, and cities
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/admin/locations")]
    [Authorize(Roles = "Admin")]
    [Tags("Admin - Locations")]
    public class AdminLocationController : ControllerBase
    {
        private readonly ICountryManagementService _countryService;
        private readonly IGovernorateManagementService _governorateService;
        private readonly ICityManagementService _cityService;
        private readonly ILogger<AdminLocationController> _logger;

        public AdminLocationController(
            ICountryManagementService countryService,
            IGovernorateManagementService governorateService,
            ICityManagementService cityService,
            ILogger<AdminLocationController> logger)
        {
            _countryService = countryService;
            _governorateService = governorateService;
            _cityService = cityService;
            _logger = logger;
        }

        // Country Management

        /// <summary>
        /// Creates a new country
        /// </summary>
        [HttpPost("countries")]
        [ProducesResponseType(typeof(CountryResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<CountryResponse>> CreateCountry(
            [FromBody] CountryAddRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                var country = await _countryService.CreateCountryAsync(request, userId, cancellationToken);

                return CreatedAtAction(
                    nameof(GetCountryById),
                    new { countryId = country.CountryId, version = "1.0" },
                    country);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating country");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while creating the country" });
            }
        }

        /// <summary>
        /// Gets a country by ID
        /// </summary>
        [HttpGet("countries/{countryId:guid}")]
        [ProducesResponseType(typeof(CountryResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<CountryResponse>> GetCountryById(
            Guid countryId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var country = await _countryService.GetCountryByIdAsync(countryId, cancellationToken);

                if (country == null)
                {
                    return NotFound(new { message = "Country not found" });
                }

                return Ok(country);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching country");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching the country" });
            }
        }

        /// <summary>
        /// Updates a country
        /// </summary>
        [HttpPut("countries/{countryId:guid}")]
        [ProducesResponseType(typeof(CountryResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<CountryResponse>> UpdateCountry(
            Guid countryId,
            [FromBody] CountryUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request.CountryId != countryId)
                {
                    return BadRequest(new { message = "Country ID mismatch" });
                }

                var userId = GetCurrentUserId();
                var country = await _countryService.UpdateCountryAsync(request, userId, cancellationToken);

                return Ok(country);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating country");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while updating the country" });
            }
        }

        // Governorate Management

        /// <summary>
        /// Creates a new governorate
        /// </summary>
        [HttpPost("governorates")]
        [ProducesResponseType(typeof(GovernorateResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<GovernorateResponse>> CreateGovernorate(
            [FromBody] GovernorateAddRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                var governorate = await _governorateService.CreateGovernorateAsync(request, userId, cancellationToken);

                return CreatedAtAction(
                    nameof(GetGovernorateById),
                    new { governorateId = governorate.GovernorateId, version = "1.0" },
                    governorate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating governorate");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while creating the governorate" });
            }
        }

        /// <summary>
        /// Gets a governorate by ID
        /// </summary>
        [HttpGet("governorates/{governorateId:guid}")]
        [ProducesResponseType(typeof(GovernorateResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<GovernorateResponse>> GetGovernorateById(
            Guid governorateId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var governorate = await _governorateService.GetGovernorateByIdAsync(governorateId, cancellationToken);

                if (governorate == null)
                {
                    return NotFound(new { message = "Governorate not found" });
                }

                return Ok(governorate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching governorate");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching the governorate" });
            }
        }

        /// <summary>
        /// Updates a governorate
        /// </summary>
        [HttpPut("governorates/{governorateId:guid}")]
        [ProducesResponseType(typeof(GovernorateResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<GovernorateResponse>> UpdateGovernorate(
            Guid governorateId,
            [FromBody] GovernorateUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request.GovernorateId != governorateId)
                {
                    return BadRequest(new { message = "Governorate ID mismatch" });
                }

                var userId = GetCurrentUserId();
                var governorate = await _governorateService.UpdateGovernorateAsync(request, userId, cancellationToken);

                return Ok(governorate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating governorate");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while updating the governorate" });
            }
        }

        // City Management

        /// <summary>
        /// Creates a new city
        /// </summary>
        [HttpPost("cities")]
        [ProducesResponseType(typeof(CityResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<CityResponse>> CreateCity(
            [FromBody] CityAddRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                var city = await _cityService.CreateCityAsync(request, userId, cancellationToken);

                return CreatedAtAction(
                    nameof(GetCityById),
                    new { cityId = city.CityId, version = "1.0" },
                    city);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating city");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while creating the city" });
            }
        }

        /// <summary>
        /// Gets a city by ID
        /// </summary>
        [HttpGet("cities/{cityId:guid}")]
        [ProducesResponseType(typeof(CityResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<CityResponse>> GetCityById(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var city = await _cityService.GetCityByIdAsync(cityId, cancellationToken);

                if (city == null)
                {
                    return NotFound(new { message = "City not found" });
                }

                return Ok(city);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching city");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching the city" });
            }
        }

        /// <summary>
        /// Updates a city
        /// </summary>
        [HttpPut("cities/{cityId:guid}")]
        [ProducesResponseType(typeof(CityResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<CityResponse>> UpdateCity(
            Guid cityId,
            [FromBody] CityUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request.CityId != cityId)
                {
                    return BadRequest(new { message = "City ID mismatch" });
                }

                var userId = GetCurrentUserId();
                var city = await _cityService.UpdateCityAsync(request, userId, cancellationToken);

                return Ok(city);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating city");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while updating the city" });
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return userId;
        }
    }
}
