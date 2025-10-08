using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Bazario.Core.Domain.IdentityEntities;
using Bazario.Core.Enums.Authentication;

namespace Bazario.Core.DTO.UserManagement.Customer
{
    public class CustomerAddRequest
    {
        [StringLength(30)]
        [Display(Name = "First Name")]
        [Required(ErrorMessage = "First Name cannot be blank")]
        public string? FirstName { get; set; }

        [StringLength(30)]
        [Display(Name = "Last Name")]
        [Required(ErrorMessage = "Last Name cannot be blank")]
        public string? LastName { get; set; }

        [StringLength(30)]
        [Display(Name = "User Name")]
        [Required(ErrorMessage = "User Name cannot be blank")]
        public string? UserName { get; set; }

        [StringLength(10)]
        [Display(Name = "Gender")]
        [Required(ErrorMessage = "Gender cannot be blank")]
        public Gender? Gender { get; set; }

        [Display(Name = "Age")]
        [Required(ErrorMessage = "Age cannot be blank")]
        public int? Age { get; set; }

        [EmailAddress]
        [StringLength(30)]
        [Display(Name = "Email address")]
        [Required(ErrorMessage = "Email address cannot be blank")]
        public string? Email { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        [Required(ErrorMessage = "Phone Number cannot be blank")]
        [RegularExpression(@"^\+?\d{10,15}$", ErrorMessage = "Invalid phone number format.")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Date of birth")]
        [Required(ErrorMessage = "Date of birth cannot be blank")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; } = false;

        [Display(Name = "Phone Number Confirmed")]
        public bool PhoneNumberConfirmed { get; set; } = false;

        [Display(Name = "User Roles")]
        public List<string> Roles { get; set; } = new() { "customer" };

        public ApplicationUser ToCustomer()
        {
            return new ApplicationUser
            {
                FirstName = FirstName,
                LastName = LastName,
                UserName = UserName,
                Gender = Gender?.ToString(),
                Age = Age,
                Email = Email,
                PhoneNumber = PhoneNumber,
                DateOfBirth = DateOfBirth,
                EmailConfirmed = EmailConfirmed,
                PhoneNumberConfirmed = PhoneNumberConfirmed,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
