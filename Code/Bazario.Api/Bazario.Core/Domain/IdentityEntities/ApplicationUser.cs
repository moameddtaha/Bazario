using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities.Order;
using Bazario.Core.Domain.Entities.Review;
using Bazario.Core.Domain.Entities.Store;
using Microsoft.AspNetCore.Identity;

namespace Bazario.Core.Domain.IdentityEntities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        [StringLength(20)]
        public string? FirstName { get; set; }

        [StringLength(20)]
        public string? LastName { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        public int? Age { get; set; }

        public DateTime? DateOfBirth { get; set; }

        // ---------- Audit Properties ----------
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastLoginAt { get; set; }

        // ---------- Navigation Properties ----------

        public ICollection<Store>? Stores { get; set; }

        public ICollection<Review>? Reviews { get; set; }

        public ICollection<Order>? Orders { get; set; }
    }
}
