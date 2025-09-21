using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.DTO;
using Bazario.Core.DTO.Admin;
using Bazario.Core.Enums;

namespace Bazario.Core.Extensions
{
    public static class AdminExtensions
    {
        public static AdminResponse ToAdminResponse(this ApplicationUser admin)
        {
            return new AdminResponse
            {
                AdminId = admin.Id,
                FirstName = admin.FirstName,
                LastName = admin.LastName,
                UserName = admin.UserName,
                Gender = Enum.TryParse<Gender>(admin.Gender, out var gender) ? gender : Gender.Undefined,
                Age = admin.Age,
                Email = admin.Email,
                PhoneNumber = admin.PhoneNumber,
                DateOfBirth = admin.DateOfBirth
            };
        }
    }
}
