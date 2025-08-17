using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Enums;

namespace Bazario.Core.DTO
{
    public class AdminResponse
    {
        public Guid AdminId { get; set; }

        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [Display(Name = "User Name")]
        public string? UserName { get; set; }

        [Display(Name = "Gender")]
        public Gender? Gender { get; set; }

        [Display(Name = "Age")]
        public int? Age { get; set; }

        [Display(Name = "Email address")]
        public string? Email { get; set; }

        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Date of birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is AdminResponse response &&
                   AdminId.Equals(response.AdminId) &&
                   FirstName == response.FirstName &&
                   LastName == response.LastName &&
                   UserName == response.UserName &&
                   Gender == response.Gender &&
                   Age == response.Age &&
                   Email == response.Email &&
                   PhoneNumber == response.PhoneNumber &&
                   DateOfBirth == response.DateOfBirth;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(AdminId);
            hash.Add(FirstName);
            hash.Add(LastName);
            hash.Add(UserName);
            hash.Add(Gender);
            hash.Add(Age);
            hash.Add(Email);
            hash.Add(PhoneNumber);
            hash.Add(DateOfBirth);
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return $"Admin: ID: {AdminId}, First Name: {FirstName}, Last Name: {LastName}, Username: {UserName}, Gender: {Gender}, Age: {Age}, Email: {Email}, Phone: {PhoneNumber}, DOB: {DateOfBirth:yyyy-MM-dd}";
        }

        public AdminUpdateRequest ToAdminUpdateRequest()
        {
            return new AdminUpdateRequest()
            {
                AdminId = AdminId,
                FirstName = FirstName,
                LastName = LastName,
                UserName = UserName,
                Gender = Gender,
                Age = Age,
                Email = Email,
                PhoneNumber = PhoneNumber,
                DateOfBirth = DateOfBirth
            };
        }
    }
}
