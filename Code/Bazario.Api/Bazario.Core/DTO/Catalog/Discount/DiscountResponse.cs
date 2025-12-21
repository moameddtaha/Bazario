using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Bazario.Core.Enums.Catalog;
using DiscountEntity = Bazario.Core.Domain.Entities.Catalog.Discount;

namespace Bazario.Core.DTO.Catalog.Discount
{
    /// <summary>
    /// Response DTO for discount data
    /// </summary>
    public class DiscountResponse
    {
        public Guid DiscountId { get; set; }

        [Display(Name = "Discount Code")]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "Discount Type")]
        public DiscountType Type { get; set; }

        [Display(Name = "Discount Value")]
        public decimal Value { get; set; }

        [Display(Name = "Valid From")]
        [DataType(DataType.DateTime)]
        public DateTime ValidFrom { get; set; }

        [Display(Name = "Valid To")]
        [DataType(DataType.DateTime)]
        public DateTime ValidTo { get; set; }

        [Display(Name = "Minimum Order Amount")]
        public decimal MinimumOrderAmount { get; set; }

        [Display(Name = "Is Used")]
        public bool IsUsed { get; set; }

        [Display(Name = "Applicable Store Id")]
        public Guid? ApplicableStoreId { get; set; }

        [Display(Name = "Store Name")]
        public string? StoreName { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Created At")]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Updated At")]
        [DataType(DataType.DateTime)]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Created By")]
        public Guid CreatedBy { get; set; }

        [Display(Name = "Updated By")]
        public Guid? UpdatedBy { get; set; }

        // ---------- Concurrency Control ----------
        [Display(Name = "Row Version")]
        public byte[]? RowVersion { get; set; }

        // ---------- Computed Properties ----------
        [Display(Name = "Is Valid")]
        public bool IsCurrentlyValid => IsActive && !IsUsed && DateTime.UtcNow >= ValidFrom && DateTime.UtcNow <= ValidTo;

        [Display(Name = "Is Global")]
        public bool IsGlobal => ApplicableStoreId == null;

        /// <summary>
        /// Creates a DiscountResponse from a Discount entity
        /// </summary>
        public static DiscountResponse FromDiscount(DiscountEntity discount)
        {
            return new DiscountResponse
            {
                DiscountId = discount.DiscountId,
                Code = discount.Code,
                Type = discount.Type,
                Value = discount.Value,
                ValidFrom = discount.ValidFrom,
                ValidTo = discount.ValidTo,
                MinimumOrderAmount = discount.MinimumOrderAmount,
                IsUsed = discount.IsUsed,
                ApplicableStoreId = discount.ApplicableStoreId,
                StoreName = discount.Store?.Name,
                IsActive = discount.IsActive,
                Description = discount.Description,
                CreatedAt = discount.CreatedAt,
                UpdatedAt = discount.UpdatedAt,
                CreatedBy = discount.CreatedBy,
                UpdatedBy = discount.UpdatedBy,
                RowVersion = discount.RowVersion
            };
        }

        public override bool Equals(object? obj)
        {
            if (obj is not DiscountResponse response)
                return false;

            // Compare RowVersion byte arrays
            bool rowVersionEquals = (RowVersion == null && response.RowVersion == null) ||
                                   (RowVersion != null && response.RowVersion != null &&
                                    RowVersion.SequenceEqual(response.RowVersion));

            return DiscountId.Equals(response.DiscountId) &&
                   Code == response.Code &&
                   Type == response.Type &&
                   Value == response.Value &&
                   ValidFrom == response.ValidFrom &&
                   ValidTo == response.ValidTo &&
                   MinimumOrderAmount == response.MinimumOrderAmount &&
                   IsUsed == response.IsUsed &&
                   ApplicableStoreId == response.ApplicableStoreId &&
                   StoreName == response.StoreName &&
                   IsActive == response.IsActive &&
                   Description == response.Description &&
                   CreatedAt == response.CreatedAt &&
                   UpdatedAt == response.UpdatedAt &&
                   CreatedBy == response.CreatedBy &&
                   UpdatedBy == response.UpdatedBy &&
                   rowVersionEquals;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(DiscountId);
            hash.Add(Code);
            hash.Add(Type);
            hash.Add(Value);
            hash.Add(ValidFrom);
            hash.Add(ValidTo);
            hash.Add(MinimumOrderAmount);
            hash.Add(IsUsed);
            hash.Add(ApplicableStoreId);
            hash.Add(StoreName);
            hash.Add(IsActive);
            hash.Add(Description);
            hash.Add(CreatedAt);
            hash.Add(UpdatedAt);
            hash.Add(CreatedBy);
            hash.Add(UpdatedBy);
            // RowVersion not included in hash - concurrency tokens typically excluded
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return $"Discount: ID: {DiscountId}, Code: {Code}, Type: {Type}, Value: {Value}, Active: {IsActive}, Used: {IsUsed}, Valid: {IsCurrentlyValid}";
        }

        public DiscountUpdateRequest ToDiscountUpdateRequest()
        {
            return new DiscountUpdateRequest
            {
                DiscountId = DiscountId,
                Code = Code,
                Type = Type,
                Value = Value,
                ValidFrom = ValidFrom,
                ValidTo = ValidTo,
                MinimumOrderAmount = MinimumOrderAmount,
                ApplicableStoreId = ApplicableStoreId,
                IsActive = IsActive,
                Description = Description,
                UpdatedBy = UpdatedBy ?? Guid.Empty,
                RowVersion = RowVersion
            };
        }
    }
}
