using System;
using System.ComponentModel.DataAnnotations;
using Bazario.Core.Enums.Catalog;
using DiscountEntity = Bazario.Core.Domain.Entities.Catalog.Discount;

namespace Bazario.Core.DTO.Catalog.Discount
{
    /// <summary>
    /// Request DTO for creating a new discount code
    /// </summary>
    public class DiscountAddRequest
    {
        [Required(ErrorMessage = "Discount code cannot be blank")]
        [StringLength(50, ErrorMessage = "Discount code cannot exceed 50 characters")]
        [Display(Name = "Discount Code")]
        public string? Code { get; set; }

        [Required(ErrorMessage = "Discount type cannot be blank")]
        [Display(Name = "Discount Type")]
        public DiscountType Type { get; set; }

        [Required(ErrorMessage = "Discount value cannot be blank")]
        [Display(Name = "Discount Value")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Discount value must be greater than 0")]
        public decimal Value { get; set; }

        [Required(ErrorMessage = "Valid from date cannot be blank")]
        [Display(Name = "Valid From")]
        [DataType(DataType.DateTime)]
        public DateTime ValidFrom { get; set; }

        [Required(ErrorMessage = "Valid to date cannot be blank")]
        [Display(Name = "Valid To")]
        [DataType(DataType.DateTime)]
        public DateTime ValidTo { get; set; }

        [Display(Name = "Minimum Order Amount")]
        [Range(0, double.MaxValue, ErrorMessage = "Minimum order amount cannot be negative")]
        public decimal MinimumOrderAmount { get; set; } = 0;

        [Display(Name = "Applicable Store Id")]
        public Guid? ApplicableStoreId { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Created by user cannot be blank")]
        [Display(Name = "Created By")]
        public Guid CreatedBy { get; set; }

        /// <summary>
        /// Converts this DTO to a Discount entity
        /// </summary>
        public DiscountEntity ToDiscount()
        {
            return new DiscountEntity
            {
                Code = Code ?? string.Empty,
                Type = Type,
                Value = Value,
                ValidFrom = ValidFrom,
                ValidTo = ValidTo,
                MinimumOrderAmount = MinimumOrderAmount,
                ApplicableStoreId = ApplicableStoreId,
                Description = Description,
                CreatedBy = CreatedBy,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsUsed = false
            };
        }
    }
}
