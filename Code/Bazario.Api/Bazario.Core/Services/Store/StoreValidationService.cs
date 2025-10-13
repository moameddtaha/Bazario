using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bazario.Core.Domain.RepositoryContracts.Store;
using Bazario.Core.Domain.RepositoryContracts.UserManagement;
using Bazario.Core.DTO.Store;
using Bazario.Core.Enums.Catalog;
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

        // Validation constants
        private const int MinStoreNameLength = 3;
        private const int MaxStoreNameLength = 30;

        // Pre-compiled regex for performance (avoid recompiling on every call)
        private static readonly Regex StoreNameRegex = new(@"^[a-zA-Z0-9\s\-_]+$", RegexOptions.Compiled);

        // Reserved store names (static to avoid recreating array on every call)
        private static readonly string[] ReservedStoreNames = { "admin", "support", "system", "api", "store", "shop", "bazario" };

        public StoreValidationService(
            IStoreRepository storeRepository,
            ISellerRepository sellerRepository,
            ILogger<StoreValidationService> logger)
        {
            _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
            _sellerRepository = sellerRepository ?? throw new ArgumentNullException(nameof(sellerRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Helper method to validate store name format
        /// Checks length, characters, and reserved names
        /// </summary>
        private void ValidateStoreNameFormat(string storeName, StoreValidationResult result)
        {
            // Validate minimum length
            if (storeName.Length < MinStoreNameLength)
            {
                result.ValidationErrors.Add($"Store name must be at least {MinStoreNameLength} characters long");
                _logger.LogDebug("Validation failed: Store name too short ({Length} chars)", storeName.Length);
            }

            // Validate maximum length
            if (storeName.Length > MaxStoreNameLength)
            {
                result.ValidationErrors.Add($"Store name cannot exceed {MaxStoreNameLength} characters");
                _logger.LogDebug("Validation failed: Store name too long ({Length} chars)", storeName.Length);
            }

            // Validate characters (alphanumeric, spaces, hyphens, underscores only)
            if (!StoreNameRegex.IsMatch(storeName))
            {
                result.ValidationErrors.Add("Store name can only contain letters, numbers, spaces, hyphens, and underscores");
                _logger.LogDebug("Validation failed: Store name contains invalid characters");
            }

            // Check reserved names
            if (ReservedStoreNames.Contains(storeName.ToLowerInvariant()))
            {
                result.ValidationErrors.Add($"Store name '{storeName}' is reserved and cannot be used");
                _logger.LogDebug("Validation failed: Store name is reserved");
            }
        }

        public async Task<StoreValidationResult> ValidateStoreCreationAsync(Guid sellerId, string storeName, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Validating store creation for seller: {SellerId}, StoreName: {StoreName}", sellerId, storeName);

            try
            {
                var result = new StoreValidationResult();

                // STEP 1: Validate input parameters FIRST (before any DB calls)
                _logger.LogDebug("Validating input parameters");

                // Validate sellerId
                if (sellerId == Guid.Empty)
                {
                    result.ValidationErrors.Add("Seller ID cannot be empty");
                    _logger.LogWarning("Validation failed: Empty seller ID");
                    return result;
                }

                // Validate store name - null/empty check
                if (string.IsNullOrWhiteSpace(storeName))
                {
                    result.ValidationErrors.Add("Store name cannot be empty");
                    _logger.LogWarning("Validation failed: Empty store name for seller {SellerId}", sellerId);
                    return result;
                }

                // Sanitize input - trim whitespace
                storeName = storeName.Trim();
                _logger.LogDebug("Sanitized store name: '{StoreName}'", storeName);

                // Validate store name format using helper method
                ValidateStoreNameFormat(storeName, result);

                // If format validation failed, return early
                if (result.ValidationErrors.Count > 0)
                {
                    _logger.LogDebug("Store creation validation failed at input validation stage. Errors: {ErrorCount}", result.ValidationErrors.Count);
                    return result;
                }

                // STEP 2: Check seller eligibility and store count
                _logger.LogDebug("Checking seller eligibility and existing stores");

                // Verify seller exists
                var seller = await _sellerRepository.GetSellerByIdAsync(sellerId, cancellationToken);
                result.SellerEligible = seller != null;

                if (!result.SellerEligible)
                {
                    result.ValidationErrors.Add($"Seller with ID '{sellerId}' does not exist or is not eligible to create stores");
                    _logger.LogWarning("Validation failed: Seller {SellerId} not found", sellerId);
                    return result; // Early return - no need to check further
                }

                // Get seller's stores to check count
                var sellerStores = await _storeRepository.GetStoresBySellerIdAsync(sellerId, cancellationToken);
                result.SellerStoreCount = sellerStores.Count;
                _logger.LogDebug("Seller {SellerId} currently has {StoreCount} stores (max: {MaxStores})",
                    sellerId, result.SellerStoreCount, result.MaxAllowedStores);

                if (result.SellerStoreCount >= result.MaxAllowedStores)
                {
                    result.ValidationErrors.Add($"Seller has reached the maximum allowed number of stores ({result.MaxAllowedStores})");
                    _logger.LogWarning("Validation failed: Seller {SellerId} has reached store limit", sellerId);
                }

                // STEP 3: Check platform-wide store name uniqueness (this covers seller-specific check too)
                _logger.LogDebug("Checking platform-wide store name uniqueness");

                var nameExistsGlobally = await _storeRepository.IsStoreNameTakenAsync(storeName, excludeStoreId: null, cancellationToken);
                result.NameAvailable = !nameExistsGlobally;

                if (nameExistsGlobally)
                {
                    result.ValidationErrors.Add($"Store name '{storeName}' is already in use. Please choose a different name");
                    _logger.LogWarning("Validation failed: Store name '{StoreName}' already exists", storeName);
                }

                // Final validation result (IsValid is computed automatically)
                _logger.LogDebug("Store creation validation completed for seller: {SellerId}. IsValid: {IsValid}, Errors: {ErrorCount}",
                    sellerId, result.IsValid, result.ValidationErrors.Count);

                if (!result.IsValid)
                {
                    _logger.LogInformation("Store creation validation failed for seller {SellerId}: {Errors}",
                        sellerId, string.Join("; ", result.ValidationErrors));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during store creation validation for seller: {SellerId}", sellerId);
                throw new InvalidOperationException($"Failed to validate store creation: {ex.Message}", ex);
            }
        }

        public async Task<StoreValidationResult> ValidateStoreUpdateAsync(Guid sellerId, StoreUpdateRequest updateRequest, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Validating store update for storeId: {StoreId}, sellerId: {SellerId}",
                updateRequest?.StoreId, sellerId);

            try
            {
                var result = new StoreValidationResult();

                // STEP 1: Validate input parameters
                _logger.LogDebug("Validating input parameters for store update");

                if (updateRequest == null)
                {
                    result.ValidationErrors.Add("Update request cannot be null");
                    return result;
                }

                if (updateRequest.StoreId == Guid.Empty)
                {
                    result.ValidationErrors.Add("Store ID cannot be empty");
                    return result;
                }

                if (sellerId == Guid.Empty)
                {
                    result.ValidationErrors.Add("Seller ID cannot be empty");
                    return result;
                }

                // STEP 2: Check if store exists and belongs to seller
                _logger.LogDebug("Checking store ownership");

                var store = await _storeRepository.GetStoreByIdAsync(updateRequest.StoreId, cancellationToken);

                if (store == null)
                {
                    result.ValidationErrors.Add($"Store with ID '{updateRequest.StoreId}' does not exist");
                    _logger.LogWarning("Validation failed: Store {StoreId} not found", updateRequest.StoreId);
                    return result;
                }

                if (store.SellerId != sellerId)
                {
                    result.ValidationErrors.Add("You do not have permission to update this store");
                    _logger.LogWarning("Validation failed: Seller {SellerId} does not own store {StoreId}", sellerId, updateRequest.StoreId);
                    return result;
                }

                // STEP 3: Check store status
                if (store.IsDeleted)
                {
                    result.ValidationErrors.Add("Cannot update a deleted store. Please restore it first");
                    _logger.LogWarning("Validation failed: Store {StoreId} is deleted", updateRequest.StoreId);
                    return result;
                }

                if (!store.IsActive)
                {
                    _logger.LogInformation("Updating inactive store {StoreId}", updateRequest.StoreId);
                    // Allow but log - business decision: inactive stores can be updated
                }

                // STEP 4: Validate updatable fields
                _logger.LogDebug("Validating store update fields");

                // Validate Name (if provided)
                if (!string.IsNullOrWhiteSpace(updateRequest.Name))
                {
                    var trimmedName = updateRequest.Name.Trim();

                    // Validate format using helper method
                    ValidateStoreNameFormat(trimmedName, result);

                    // Only check uniqueness if format validation passed
                    if (result.ValidationErrors.Count == 0)
                    {
                        // Check if name is actually changing
                        bool nameIsChanging = string.IsNullOrEmpty(store.Name) ||
                                              !string.Equals(store.Name, trimmedName, StringComparison.OrdinalIgnoreCase);

                        if (nameIsChanging)
                        {
                            // Check platform-wide uniqueness (excluding current store)
                            var nameExists = await _storeRepository.IsStoreNameTakenAsync(trimmedName, updateRequest.StoreId, cancellationToken);

                            if (nameExists)
                            {
                                result.ValidationErrors.Add($"Store name '{trimmedName}' is already in use. Please choose a different name");
                                _logger.LogWarning("Validation failed: Store name '{StoreName}' already exists", trimmedName);
                            }
                        }
                    }
                }

                // Validate Description (if provided)
                if (!string.IsNullOrWhiteSpace(updateRequest.Description))
                {
                    if (updateRequest.Description.Length > 100)
                    {
                        result.ValidationErrors.Add("Description cannot exceed 100 characters");
                        _logger.LogDebug("Validation failed: Description too long ({Length} chars)", updateRequest.Description.Length);
                    }
                }

                // Validate Category (if provided)
                if (!string.IsNullOrWhiteSpace(updateRequest.Category))
                {
                    var trimmedCategory = updateRequest.Category.Trim();

                    // Validate against Category enum
                    if (!Enum.TryParse<Category>(trimmedCategory, ignoreCase: true, out _))
                    {
                        var validCategories = string.Join(", ", Enum.GetNames(typeof(Category)));
                        result.ValidationErrors.Add($"Invalid category. Valid categories are: {validCategories}");
                        _logger.LogDebug("Validation failed: Invalid category '{Category}'", trimmedCategory);
                    }
                }

                // Validate Logo (if provided)
                if (!string.IsNullOrWhiteSpace(updateRequest.Logo))
                {
                    var trimmedLogo = updateRequest.Logo.Trim();

                    // Validate URL format
                    if (!Uri.TryCreate(trimmedLogo, UriKind.Absolute, out var logoUri) ||
                        (logoUri.Scheme != Uri.UriSchemeHttp && logoUri.Scheme != Uri.UriSchemeHttps))
                    {
                        result.ValidationErrors.Add("Logo must be a valid HTTP or HTTPS URL");
                        _logger.LogDebug("Validation failed: Invalid logo URL format");
                    }
                    else if (trimmedLogo.Length > 500)
                    {
                        result.ValidationErrors.Add("Logo URL cannot exceed 500 characters");
                        _logger.LogDebug("Validation failed: Logo URL too long ({Length} chars)", trimmedLogo.Length);
                    }
                }

                // IsValid is computed automatically based on ValidationErrors.Count
                _logger.LogDebug("Store update validation completed for store: {StoreId}. IsValid: {IsValid}, Errors: {ErrorCount}",
                    updateRequest.StoreId, result.IsValid, result.ValidationErrors.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during store update validation for store: {StoreId}", updateRequest?.StoreId);
                throw new InvalidOperationException($"Failed to validate store update: {ex.Message}", ex);
            }
        }

        public async Task<StoreValidationResult> ValidateStoreSoftDeletionAsync(Guid storeId, Guid sellerId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Validating store soft deletion for storeId: {StoreId}, sellerId: {SellerId}", storeId, sellerId);

            try
            {
                var result = new StoreValidationResult();

                // STEP 1: Validate input parameters
                if (storeId == Guid.Empty)
                {
                    result.ValidationErrors.Add("Store ID cannot be empty");
                    return result;
                }

                if (sellerId == Guid.Empty)
                {
                    result.ValidationErrors.Add("Seller ID cannot be empty");
                    return result;
                }

                // STEP 2: Check if store exists and belongs to seller
                _logger.LogDebug("Checking store ownership for soft deletion");

                var store = await _storeRepository.GetStoreByIdAsync(storeId, cancellationToken);

                if (store == null)
                {
                    result.ValidationErrors.Add($"Store with ID '{storeId}' does not exist");
                    _logger.LogWarning("Soft deletion validation failed: Store {StoreId} not found", storeId);
                    return result;
                }

                if (store.SellerId != sellerId)
                {
                    result.ValidationErrors.Add("You do not have permission to delete this store");
                    _logger.LogWarning("Soft deletion validation failed: Seller {SellerId} does not own store {StoreId}", sellerId, storeId);
                    return result;
                }

                // STEP 3: Check if store is already soft deleted
                if (store.IsDeleted)
                {
                    result.ValidationErrors.Add("Store is already soft deleted");
                    _logger.LogWarning("Soft deletion validation failed: Store {StoreId} is already deleted", storeId);
                    return result;
                }

                // STEP 4: Check if store has active products
                _logger.LogDebug("Checking if store has active products for soft deletion");

                var productCount = await _storeRepository.GetProductCountByStoreIdAsync(storeId, cancellationToken);

                if (productCount > 0)
                {
                    result.ValidationErrors.Add($"Cannot soft delete store with {productCount} active product(s). Please remove or deactivate all products first");
                    _logger.LogWarning("Soft deletion validation failed: Store {StoreId} has {ProductCount} active products", storeId, productCount);
                }

                // Log if soft deleting inactive store (allowed but noteworthy)
                if (!store.IsActive)
                {
                    _logger.LogInformation("Soft deleting inactive store {StoreId}", storeId);
                }

                // IsValid is computed automatically based on ValidationErrors.Count
                _logger.LogDebug("Store soft deletion validation completed for store: {StoreId}. IsValid: {IsValid}, Errors: {ErrorCount}",
                    storeId, result.IsValid, result.ValidationErrors.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during store soft deletion validation for store: {StoreId}", storeId);
                throw new InvalidOperationException($"Failed to validate store soft deletion: {ex.Message}", ex);
            }
        }
    }
}
