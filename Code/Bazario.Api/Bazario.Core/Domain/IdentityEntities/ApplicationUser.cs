using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bazario.Core.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Bazario.Core.Domain.IdentityEntities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        [StringLength(20)]
        public string? FirstName { get; set; }

        [StringLength(20)]
        public string? LastName { get; set; }

        // ---------- Navigation Properties ----------

        public ICollection<Store>? Stores { get; set; }

        public ICollection<Review>? Reviews { get; set; }

        public ICollection<Order>? Orders { get; set; }
    }
}
