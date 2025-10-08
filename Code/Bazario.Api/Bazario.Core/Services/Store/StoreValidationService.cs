using System;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts.Store;
using Bazario.Core.Domain.RepositoryContracts.UserManagement;
using Bazario.Core.Models.Store;
using Bazario.Core.ServiceContracts.Store;
using Microsoft.Extensions.Logging;

namespace Bazario.Core.Services.Store
{
    /// <summary>
    /// Service implementation for store validation operations
    /// Handles business rule validation for store operations
    /// </summary>
    public class StoreValidationService : IStoreValidationService
    {
        private readonly IStoreRepository _storeRepository;
        private readonly ISellerRepository _sellerRepository;
        private readonly ILogger<StoreValidationService> _logger;

        public StoreValidationService(
            IStoreRepository storeRepository,
            ISellerRepository sellerRepository,
            ILogger<StoreValidationService> logger)
        {
            _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
            _sellerRepository = sellerRepository ?? throw new ArgumentNullException(nameof(sellerRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<StoreValidationResult> ValidateStoreCreationAsync(Guid sellerId, string storeName, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Validating store creation for seller: {SellerId}, StoreName: {StoreName}", sellerId, storeName);

            try
            {
                var result = new StoreValidationResult();

                // Check if seller exists
                var seller = await _sellerRepository.GetSellerByIdAsync(sellerId, cancellationToken);
                result.SellerEligible = seller != null;

                if (!result.SellerEligible)
                {
                    result.ValidationErrors.Add("Seller not found or not eligible");
                }

                // Check seller's current store count
                var sellerStores = await _storeRepository.GetStoresBySellerIdAsync(sellerId, cancellationToken);
                result.SellerStoreCount = sellerStores.Count;

                if (result.SellerStoreCount >= result.MaxAllowedStores)
                {
                    result.ValidationErrors.Add($"Seller has reached maximum allowed stores ({result.MaxAllowedStores})");
                }

                // Check if store name is available for this seller
                result.NameAvailable = !sellerStores.Any(s => 
                    string.Equals(s.Name, storeName, StringComparison.OrdinalIgnoreCase));

                if (!result.NameAvailable)
                {
                    result.ValidationErrors.Add($"Store name '{storeName}' is already used by this seller");
                }

                // Validate store name
                if (string.IsNullOrWhiteSpace(storeName))
                {
                    result.ValidationErrors.Add("Store name cannot be empty");
                }
                else if (storeName.Length > 30)
                {
                    result.ValidationErrors.Add("Store name cannot exceed 30 characters");
                }

                result.IsValid = result.ValidationErrors.Count == 0;

                _logger.LogDebug("Store creation validation completed for seller: {SellerId}. IsValid: {IsValid}, Errors: {ErrorCount}", 
                    sellerId, result.IsValid, result.ValidationErrors.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate store creation for seller: {SellerId}", sellerId);
                throw;
            }
        }
    }
}
