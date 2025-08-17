using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.IdentityEntities;

namespace Bazario.Core.Domain.RepositoryContracts
{
    public interface ISellerRepository
    {
        Task<ApplicationUser> AddSellerAsync(ApplicationUser seller, string password, CancellationToken cancellationToken = default);

        Task<ApplicationUser> UpdateSellerAsync(ApplicationUser seller, CancellationToken cancellationToken = default);

        Task<bool> DeleteSellerByIdAsync(Guid sellerId, CancellationToken cancellationToken = default);

        Task<ApplicationUser?> GetSellerByIdAsync(Guid sellerId, CancellationToken cancellationToken = default);

        Task<List<ApplicationUser>> GetAllSellersAsync(CancellationToken cancellationToken = default);

        Task<ApplicationUser> GetFilteredSellerAsync(Expression<Func<ApplicationUser, bool>> predicate, CancellationToken cancellationToken = default);

        Task<bool> ChangeSellerPasswordAsync(Guid sellerId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

        Task<bool> ResetSellerPasswordAsync(Guid sellerId, string newPassword, CancellationToken cancellationToken = default);
    }
}
