using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Bazario.Core.Domain.IdentityEntities;

namespace Bazario.Core.Domain.RepositoryContracts.UserManagement
{
    public interface IAdminRepository
    {
        Task<ApplicationUser> AddAdminAsync(ApplicationUser admin, string password, CancellationToken cancellationToken = default);

        Task<ApplicationUser> UpdateAdminAsync(ApplicationUser admin, CancellationToken cancellationToken = default);

        Task<bool> DeleteAdminByIdAsync(Guid adminId, CancellationToken cancellationToken = default);

        Task<ApplicationUser?> GetAdminByIdAsync(Guid adminId, CancellationToken cancellationToken = default);

        Task<List<ApplicationUser>> GetAllAdminsAsync(CancellationToken cancellationToken = default);

        Task<List<ApplicationUser>> GetFilteredAdminsAsync(Expression<Func<ApplicationUser, bool>> predicate, CancellationToken cancellationToken = default);

        Task<bool> ChangeAdminPasswordAsync(Guid adminId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

        Task<bool> ResetAdminPasswordAsync(Guid adminId, string newPassword, CancellationToken cancellationToken = default);
    }
}
