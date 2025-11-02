using Bazario.Core.Domain.RepositoryContracts;
using Bazario.Core.Domain.RepositoryContracts.Authentication;
using Bazario.Core.Domain.RepositoryContracts.Catalog;
using Bazario.Core.Domain.RepositoryContracts.Inventory;
using Bazario.Core.Domain.RepositoryContracts.Location;
using Bazario.Core.Domain.RepositoryContracts.Order;
using Bazario.Core.Domain.RepositoryContracts.Review;
using Bazario.Core.Domain.RepositoryContracts.Store;
using Bazario.Core.Domain.RepositoryContracts.UserManagement;
using Bazario.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Bazario.Infrastructure.Repositories
{
    /// <summary>
    /// Implementation of the Unit of Work pattern for managing database transactions
    /// Coordinates work across multiple repositories and ensures they share the same transaction context
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UnitOfWork> _logger;
        private IDbContextTransaction? _transaction;
        private bool _disposed = false;

        // Repositories injected through constructor
        private readonly IRefreshTokenRepository _refreshTokens;
        private readonly IAdminRepository _admins;
        private readonly ICustomerRepository _customers;
        private readonly ISellerRepository _sellers;
        private readonly IProductRepository _products;
        private readonly IDiscountRepository _discounts;
        private readonly ICountryRepository _countries;
        private readonly IGovernorateRepository _governorates;
        private readonly ICityRepository _cities;
        private readonly IStoreGovernorateSupportRepository _storeGovernorateSupports;
        private readonly IStoreRepository _stores;
        private readonly IStoreShippingConfigurationRepository _storeShippingConfigurations;
        private readonly IOrderRepository _orders;
        private readonly IOrderItemRepository _orderItems;
        private readonly IReviewRepository _reviews;
        private readonly IStockReservationRepository _stockReservations;
        private readonly IInventoryAlertPreferencesRepository _inventoryAlertPreferences;

        public UnitOfWork(
            ApplicationDbContext context,
            ILogger<UnitOfWork> logger,
            IRefreshTokenRepository refreshTokens,
            IAdminRepository admins,
            ICustomerRepository customers,
            ISellerRepository sellers,
            IProductRepository products,
            IDiscountRepository discounts,
            ICountryRepository countries,
            IGovernorateRepository governorates,
            ICityRepository cities,
            IStoreGovernorateSupportRepository storeGovernorateSupports,
            IStoreRepository stores,
            IStoreShippingConfigurationRepository storeShippingConfigurations,
            IOrderRepository orders,
            IOrderItemRepository orderItems,
            IReviewRepository reviews,
            IStockReservationRepository stockReservations,
            IInventoryAlertPreferencesRepository inventoryAlertPreferences)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _refreshTokens = refreshTokens ?? throw new ArgumentNullException(nameof(refreshTokens));
            _admins = admins ?? throw new ArgumentNullException(nameof(admins));
            _customers = customers ?? throw new ArgumentNullException(nameof(customers));
            _sellers = sellers ?? throw new ArgumentNullException(nameof(sellers));
            _products = products ?? throw new ArgumentNullException(nameof(products));
            _discounts = discounts ?? throw new ArgumentNullException(nameof(discounts));
            _countries = countries ?? throw new ArgumentNullException(nameof(countries));
            _governorates = governorates ?? throw new ArgumentNullException(nameof(governorates));
            _cities = cities ?? throw new ArgumentNullException(nameof(cities));
            _storeGovernorateSupports = storeGovernorateSupports ?? throw new ArgumentNullException(nameof(storeGovernorateSupports));
            _stores = stores ?? throw new ArgumentNullException(nameof(stores));
            _storeShippingConfigurations = storeShippingConfigurations ?? throw new ArgumentNullException(nameof(storeShippingConfigurations));
            _orders = orders ?? throw new ArgumentNullException(nameof(orders));
            _orderItems = orderItems ?? throw new ArgumentNullException(nameof(orderItems));
            _reviews = reviews ?? throw new ArgumentNullException(nameof(reviews));
            _stockReservations = stockReservations ?? throw new ArgumentNullException(nameof(stockReservations));
            _inventoryAlertPreferences = inventoryAlertPreferences ?? throw new ArgumentNullException(nameof(inventoryAlertPreferences));
        }

        // Repository properties - expose injected repositories
        public IRefreshTokenRepository RefreshTokens => _refreshTokens;
        public IAdminRepository Admins => _admins;
        public ICustomerRepository Customers => _customers;
        public ISellerRepository Sellers => _sellers;
        public IProductRepository Products => _products;
        public IDiscountRepository Discounts => _discounts;
        public ICountryRepository Countries => _countries;
        public IGovernorateRepository Governorates => _governorates;
        public ICityRepository Cities => _cities;
        public IStoreGovernorateSupportRepository StoreGovernorateSupports => _storeGovernorateSupports;
        public IStoreRepository Stores => _stores;
        public IStoreShippingConfigurationRepository StoreShippingConfigurations => _storeShippingConfigurations;
        public IOrderRepository Orders => _orders;
        public IOrderItemRepository OrderItems => _orderItems;
        public IReviewRepository Reviews => _reviews;
        public IStockReservationRepository StockReservations => _stockReservations;
        public IInventoryAlertPreferencesRepository InventoryAlertPreferences => _inventoryAlertPreferences;

        /// <summary>
        /// Saves all changes made in this unit of work to the database
        /// </summary>
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _context.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("Successfully saved {Count} changes to the database", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes to the database");
                throw;
            }
        }

        /// <summary>
        /// Begins a new database transaction
        /// </summary>
        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                _logger.LogWarning("A transaction is already in progress");
                throw new InvalidOperationException("A transaction is already in progress");
            }

            try
            {
                _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                _logger.LogDebug("Transaction started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting database transaction");
                throw;
            }
        }

        /// <summary>
        /// Commits the current database transaction
        /// </summary>
        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
            {
                _logger.LogWarning("No transaction in progress to commit");
                throw new InvalidOperationException("No transaction in progress");
            }

            try
            {
                await _transaction.CommitAsync(cancellationToken);
                _logger.LogDebug("Transaction committed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error committing transaction");
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        /// <summary>
        /// Rolls back the current database transaction
        /// </summary>
        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
            {
                _logger.LogWarning("No transaction in progress to rollback");
                return;
            }

            try
            {
                await _transaction.RollbackAsync(cancellationToken);
                _logger.LogDebug("Transaction rolled back");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rolling back transaction");
                throw;
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        /// <summary>
        /// Disposes the unit of work and releases resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transaction?.Dispose();
                    _logger.LogDebug("UnitOfWork disposed");
                }
                _disposed = true;
            }
        }
    }
}
