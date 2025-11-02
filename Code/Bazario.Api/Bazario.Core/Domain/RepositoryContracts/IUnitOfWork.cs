using Bazario.Core.Domain.RepositoryContracts.Authentication;
using Bazario.Core.Domain.RepositoryContracts.Catalog;
using Bazario.Core.Domain.RepositoryContracts.Inventory;
using Bazario.Core.Domain.RepositoryContracts.Location;
using Bazario.Core.Domain.RepositoryContracts.Order;
using Bazario.Core.Domain.RepositoryContracts.Review;
using Bazario.Core.Domain.RepositoryContracts.Store;
using Bazario.Core.Domain.RepositoryContracts.UserManagement;

namespace Bazario.Core.Domain.RepositoryContracts
{
    /// <summary>
    /// Defines the Unit of Work pattern for managing database transactions across multiple repositories
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        // Authentication
        IRefreshTokenRepository RefreshTokens { get; }

        // User Management
        IAdminRepository Admins { get; }
        ICustomerRepository Customers { get; }
        ISellerRepository Sellers { get; }

        // Catalog
        IProductRepository Products { get; }
        IDiscountRepository Discounts { get; }

        // Location
        ICountryRepository Countries { get; }
        IGovernorateRepository Governorates { get; }
        ICityRepository Cities { get; }
        IStoreGovernorateSupportRepository StoreGovernorateSupports { get; }

        // Store
        IStoreRepository Stores { get; }
        IStoreShippingConfigurationRepository StoreShippingConfigurations { get; }

        // Order
        IOrderRepository Orders { get; }
        IOrderItemRepository OrderItems { get; }

        // Review
        IReviewRepository Reviews { get; }

        // Inventory
        IStockReservationRepository StockReservations { get; }
        IInventoryAlertPreferencesRepository InventoryAlertPreferences { get; }

        /// <summary>
        /// Saves all changes made in this unit of work to the database
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The number of state entries written to the database</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Begins a new database transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits the current database transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the current database transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}
