using Asp.Versioning;
using Bazario.Core.DTO.Location.City;
using Bazario.Core.DTO.Location.Country;
using Bazario.Core.DTO.Location.Governorate;
using Bazario.Core.ServiceContracts.Location;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bazario.Api.Controllers.v1.Location
{
    /// <summary>
    /// Public API for browsing countries, governorates, and cities
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/locations")]
    [Tags("Public - Locations")]
    public class PublicLocationController : ControllerBase
    {
        private readonly ICountryManagementService _countryService;
        private readonly IGovernorateManagementService _governorateService;
        private readonly ICityManagementService _cityService;
        private readonly ILogger<PublicLocationController> _logger;

        public PublicLocationController(
            ICountryManagementService countryService,
            IGovernorateManagementService governorateService,
            ICityManagementService cityService,
            ILogger<PublicLocationController> logger)
        {
            _countryService = countryService;
            _governorateService = governorateService;
            _cityService = cityService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all active countries
        /// </summary>
        [HttpGet("countries")]
        [ProducesResponseType(typeof(List<CountryResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CountryResponse>>> GetActiveCountries(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var countries = await _countryService.GetActiveCountriesAsync(cancellationToken);
                return Ok(countries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching countries");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching countries" });
            }
        }

        /// <summary>
        /// Gets governorates by country ID
        /// </summary>
        [HttpGet("countries/{countryId:guid}/governorates")]
        [ProducesResponseType(typeof(List<GovernorateResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<GovernorateResponse>>> GetGovernoratesByCountry(
            Guid countryId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var governorates = await _governorateService.GetActiveGovernoratesByCountryAsync(
                    countryId, cancellationToken);
                return Ok(governorates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching governorates for country: {CountryId}", countryId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching governorates" });
            }
        }

        /// <summary>
        /// Gets cities by governorate ID
        /// </summary>
        [HttpGet("governorates/{governorateId:guid}/cities")]
        [ProducesResponseType(typeof(List<CityResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CityResponse>>> GetCitiesByGovernorate(
            Guid governorateId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var cities = await _cityService.GetActiveCitiesByGovernorateAsync(
                    governorateId, cancellationToken);
                return Ok(cities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching cities for governorate: {GovernorateId}", governorateId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while fetching cities" });
            }
        }
    }
}
