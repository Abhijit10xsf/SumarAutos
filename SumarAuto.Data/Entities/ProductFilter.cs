using System.Collections.Generic;

namespace SumarAuto.Data.Entities
{
    public class ProductFilter
    {
        public string Query { get; set; }
        public string Category { get; set; }
        public List<string> Brands { get; set; } = new List<string>();
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool SharjahOnly { get; set; }
        public bool JebelOnly { get; set; }
        public bool OffersOnly { get; set; }
        public bool InStockOnly { get; set; }
        public string SortBy { get; set; } = "featured"; // featured, price-low, price-high, stock
    }
}
