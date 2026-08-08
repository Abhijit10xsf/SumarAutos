using System;
using System.Collections.Generic;

namespace SumarAuto.Data.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }
        public string Brand { get; set; }
        public string Category { get; set; }
        public string Image { get; set; }
        public string Ean { get; set; }
        public string Oe { get; set; }
        public string Compatibility { get; set; }
        public string SpecsJson { get; set; }
        public Dictionary<string, string> Specs { get; set; } = new Dictionary<string, string>();
        public int SharjahStock { get; set; }
        public int JebelStock { get; set; }
        public int TransitStock { get; set; }
        public decimal Price { get; set; }
        public int Moq { get; set; }
        public bool IsOffer { get; set; }

        public int TotalStock => SharjahStock + JebelStock;
    }
}
