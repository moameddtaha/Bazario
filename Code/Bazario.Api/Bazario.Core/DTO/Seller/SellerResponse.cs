using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Enums;

namespace Bazario.Core.DTO.Seller
{
    public class SellerResponse
    {
        public Guid SellerId { get; set; }

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

        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; }

        [Display(Name = "Phone Number Confirmed")]
        public bool PhoneNumberConfirmed { get; set; }

        [Display(Name = "User Role")]
        public Role Role { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Last Login At")]
        public DateTime? LastLoginAt { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is SellerResponse response &&
                   SellerId.Equals(response.SellerId) &&
                   FirstName == response.FirstName &&
                   LastName == response.LastName &&
                   UserName == response.UserName &&
                   Gender == response.Gender &&
                   Age == response.Age &&
                   Email == response.Email &&
                   PhoneNumber == response.PhoneNumber &&
                   DateOfBirth == response.DateOfBirth &&
                   EmailConfirmed == response.EmailConfirmed &&
                   PhoneNumberConfirmed == response.PhoneNumberConfirmed &&
                   CreatedAt == response.CreatedAt &&
                   LastLoginAt == response.LastLoginAt;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(SellerId);
            hash.Add(FirstName);
            hash.Add(LastName);
            hash.Add(UserName);
            hash.Add(Gender);
            hash.Add(Age);
            hash.Add(Email);
            hash.Add(PhoneNumber);
            hash.Add(DateOfBirth);
            hash.Add(EmailConfirmed);
            hash.Add(PhoneNumberConfirmed);
            hash.Add(CreatedAt);
            hash.Add(LastLoginAt);
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return $"Seller: ID: {SellerId}, First Name: {FirstName}, Last Name: {LastName}, Username: {UserName}, Gender: {Gender}, Age: {Age}, Email: {Email}, Phone: {PhoneNumber}, DOB: {DateOfBirth:yyyy-MM-dd}, Email Confirmed: {EmailConfirmed}, Phone Confirmed: {PhoneNumberConfirmed}, Created: {CreatedAt:yyyy-MM-dd}, Last Login: {LastLoginAt:yyyy-MM-dd}";
        }

        public SellerUpdateRequest ToSellerUpdateRequest()
        {
            return new SellerUpdateRequest()
            {
                SellerId = SellerId,
                FirstName = FirstName,
                LastName = LastName,
                UserName = UserName,
                Gender = Gender,
                Age = Age,
                Email = Email,
                PhoneNumber = PhoneNumber,
                DateOfBirth = DateOfBirth,
                EmailConfirmed = EmailConfirmed,
                PhoneNumberConfirmed = PhoneNumberConfirmed,
                Role = Role,
                LastLoginAt = LastLoginAt
            };
        }
    }
}
