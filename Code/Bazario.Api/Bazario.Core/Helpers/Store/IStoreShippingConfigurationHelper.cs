using System.Collections.Generic;
using Bazario.Core.Domain.Entities.Location;
using Bazario.Core.DTO.Store;

namespace Bazario.Core.Helpers.Store
{
    /// <summary>
    /// Helper interface for store shipping configuration operations
    /// </summary>
    public interface IStoreShippingConfigurationHelper
    {
        /// <summary>
        /// Maps a collection of StoreGovernorateSupport entities to GovernorateShippingInfo DTOs
        /// </summary>
        /// <param name="governorates">Collection of StoreGovernorateSupport entities</param>
        /// <returns>List of GovernorateShippingInfo DTOs</returns>
        List<GovernorateShippingInfo> MapGovernorateShippingInfo(IEnumerable<StoreGovernorateSupport> governorates);

        /// <summary>
        /// Validates the shipping configuration request for business rules
        /// </summary>
        /// <param name="request">Configuration request to validate</param>
        /// <exception cref="System.ArgumentException">Thrown when business rules are violated</exception>
        void ValidateConfigurationBusinessRules(StoreShippingConfigurationRequest request);
    }
}
