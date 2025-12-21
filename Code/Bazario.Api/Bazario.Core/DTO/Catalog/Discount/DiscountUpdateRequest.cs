using System;
using System.ComponentModel.DataAnnotations;
using Bazario.Core.Enums.Catalog;
using DiscountEntity = Bazario.Core.Domain.Entities.Catalog.Discount;

namespace Bazario.Core.DTO.Catalog.Discount
{
    /// <summary>
    /// Request DTO for updating an existing discount code
    /// Uses nullable properties for partial updates
    /// </summary>
    public class DiscountUpdateRequest
    {
        [Required(ErrorMessage = "Discount Id cannot be blank")]
        public Guid DiscountId { get; set; }

        [StringLength(50, ErrorMessage = "Discount code cannot exceed 50 characters")]
        [Display(Name = "Discount Code")]
        public string? Code { get; set; }

        [Display(Name = "Discount Type")]
        public DiscountType? Type { get; set; }

        [Display(Name = "Discount Value")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Discount value must be greater than 0")]
        public decimal? Value { get; set; }

        [Display(Name = "Valid From")]
        [DataType(DataType.DateTime)]
        public DateTime? ValidFrom { get; set; }

        [Display(Name = "Valid To")]
        [DataType(DataType.DateTime)]
        public DateTime? ValidTo { get; set; }

        [Display(Name = "Minimum Order Amount")]
        [Range(0, double.MaxValue, ErrorMessage = "Minimum order amount cannot be negative")]
        public decimal? MinimumOrderAmount { get; set; }

        [Display(Name = "Applicable Store Id")]
        public Guid? ApplicableStoreId { get; set; }

        [Display(Name = "Is Active")]
        public bool? IsActive { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Updated by user cannot be blank")]
        [Display(Name = "Updated By")]
        public Guid UpdatedBy { get; set; }

        /// <summary>
        /// Row version for optimistic concurrency control
        /// Must be provided for updates to prevent lost updates
        /// </summary>
        [Display(Name = "Row Version")]
        public byte[]? RowVersion { get; set; }

        /// <summary>
        /// Converts this DTO to a Discount entity for partial updates
        /// NOTE: Uses sentinel values where needed for "not provided" fields
        /// Null values indicate "don't update this field"
        /// </summary>
        public DiscountEntity ToDiscount()
        {
            return new DiscountEntity
            {
                DiscountId = DiscountId,
                Code = Code ?? string.Empty,
                Type = Type ?? DiscountType.Percentage, // Sentinel: repository should check for null
                Value = Value ?? -1, // Sentinel: -1 indicates not provided
                ValidFrom = ValidFrom ?? DateTime.MinValue, // Sentinel: MinValue indicates not provided
                ValidTo = ValidTo ?? DateTime.MinValue, // Sentinel: MinValue indicates not provided
                MinimumOrderAmount = MinimumOrderAmount ?? -1, // Sentinel: -1 indicates not provided
                ApplicableStoreId = ApplicableStoreId,
                IsActive = IsActive ?? true,
                Description = Description,
                UpdatedBy = UpdatedBy,
                UpdatedAt = DateTime.UtcNow,
                RowVersion = RowVersion
            };
        }
    }
}
