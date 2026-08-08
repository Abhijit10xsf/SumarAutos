using System.Collections.Generic;
using SumarAuto.Data.Entities;

namespace SumarAuto.Client.Models
{
    public class CatalogViewModel
    {
        public SummaryStats Stats { get; set; }
        public IEnumerable<Product> Products { get; set; }
        public List<string> Categories { get; set; }
        public List<string> Brands { get; set; }
        public ProductFilter Filter { get; set; } = new ProductFilter();
        public CartSummary Cart { get; set; }
    }
}
