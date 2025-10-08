using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Bazario.Core.Domain.IdentityEntities;

namespace Bazario.Core.Domain.RepositoryContracts.UserManagement
{
    public interface ICustomerRepository
    {
        Task<ApplicationUser> AddCustomerAsync(ApplicationUser customer, string password, CancellationToken cancellationToken = default);

        Task<ApplicationUser> UpdateCustomerAsync(ApplicationUser customer, CancellationToken cancellationToken = default);

        Task<bool> DeleteCustomerByIdAsync(Guid customerId, CancellationToken cancellationToken = default);

        Task<ApplicationUser?> GetCustomerByIdAsync(Guid customerId, CancellationToken cancellationToken = default);

        Task<List<ApplicationUser>> GetAllCustomersAsync(CancellationToken cancellationToken = default);

        Task<List<ApplicationUser>> GetFilteredCustomersAsync(Expression<Func<ApplicationUser, bool>> predicate, CancellationToken cancellationToken = default);

        Task<bool> ChangeCustomerPasswordAsync(Guid customerId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

        Task<bool> ResetCustomerPasswordAsync(Guid customerId, string newPassword, CancellationToken cancellationToken = default);
    }
}
