using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Enums.Authentication;

namespace Bazario.Core.DTO.UserManagement.Seller
{
    public class SellerUpdateRequest
    {
        [Required(ErrorMessage = "Seller Id cannot be blank")]
        public Guid SellerId { get; set; }

        [StringLength(30)]
        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [StringLength(30)]
        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [StringLength(30)]
        [Display(Name = "User Name")]
        public string? UserName { get; set; }

        [StringLength(10)]
        [Display(Name = "Gender")]
        public Gender? Gender { get; set; }

        [Display(Name = "Age")]
        public int? Age { get; set; }

        [EmailAddress]
        [StringLength(30)]
        [Display(Name = "Email address")]
        public string? Email { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        [RegularExpression(@"^\+?\d{10,15}$", ErrorMessage = "Invalid phone number format.")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Date of birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Email Confirmed")]
        public bool? EmailConfirmed { get; set; }

        [Display(Name = "Phone Number Confirmed")]
        public bool? PhoneNumberConfirmed { get; set; }

        [Display(Name = "Last Login At")]
        [DataType(DataType.DateTime)]
        public DateTime? LastLoginAt { get; set; }

        public ApplicationUser ToSeller()
        {
            return new ApplicationUser
            {
                Id = SellerId,
                FirstName = FirstName,
                LastName = LastName,
                UserName = UserName,
                Gender = Gender?.ToString(),
                Age = Age,
                Email = Email,
                PhoneNumber = PhoneNumber,
                DateOfBirth = DateOfBirth,
                EmailConfirmed = EmailConfirmed ?? false,
                PhoneNumberConfirmed = PhoneNumberConfirmed ?? false,
                LastLoginAt = LastLoginAt
                // Note: Role is not set here as it's managed by UserManager
            };
        }
    }
}
