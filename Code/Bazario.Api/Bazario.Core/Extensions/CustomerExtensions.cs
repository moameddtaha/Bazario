using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.DTO;
using Bazario.Core.DTO.Customer;
using Bazario.Core.Enums;

namespace Bazario.Core.Extensions
{
    public static class CustomerExtensions
    {
        public static CustomerResponse ToCustomerResponse(this ApplicationUser customer)
        {
            return new CustomerResponse
            {
                CustomerId = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                UserName = customer.UserName,
                Gender = Enum.TryParse<Gender>(customer.Gender, out var gender) ? gender : Gender.Undefined,
                Age = customer.Age,
                Email = customer.Email,
                PhoneNumber = customer.PhoneNumber,
                DateOfBirth = customer.DateOfBirth
            };
        }
    }
}
