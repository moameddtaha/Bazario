using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.DTO;
using Bazario.Core.DTO.UserManagement.Seller;
using Bazario.Core.Enums.Authentication;

namespace Bazario.Core.Extensions.UserManagement
{
    public static class SellerExtensions
    {
        public static SellerResponse ToSellerResponse(this ApplicationUser seller)
        {
            return new SellerResponse
            {
                SellerId = seller.Id,
                FirstName = seller.FirstName,
                LastName = seller.LastName,
                UserName = seller.UserName,
                Gender = Enum.TryParse<Gender>(seller.Gender, out var gender) ? gender : Gender.Undefined,
                Age = seller.Age,
                Email = seller.Email,
                PhoneNumber = seller.PhoneNumber,
                DateOfBirth = seller.DateOfBirth
            };
        }
    }
}
