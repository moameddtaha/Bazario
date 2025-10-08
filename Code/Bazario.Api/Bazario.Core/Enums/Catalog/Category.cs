using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bazario.Core.Enums.Catalog
{
    [Flags]
    public enum Category
    {
        uncategorized = 1,
        Clothes = 2,
        Electronics = 4,
        Home = 8,
        Sports = 16,
        Books = 32,
        Beauty = 64,
        Toys = 128,
    }
}
