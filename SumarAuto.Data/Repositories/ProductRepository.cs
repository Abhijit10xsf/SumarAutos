using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json;
using SumarAuto.Data.Interfaces;
using SumarAuto.Data.Entities;

namespace SumarAuto.Data.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly SumarDbContext _db;
        private readonly HanaDataHelper _hanaHelper;

        public ProductRepository()
        {
            _db = new SumarDbContext();
            _hanaHelper = new HanaDataHelper();
        }

        public ProductRepository(SumarDbContext db)
        {
            _db = db ?? new SumarDbContext();
            _hanaHelper = new HanaDataHelper();
        }

        public IEnumerable<Product> GetAllProducts(ProductFilter filter = null)
        {
            try
            {
                var hanaProducts = GetProductsFromHana(filter);
                if (hanaProducts != null && hanaProducts.Count > 0)
                {
                    return hanaProducts;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("HANA Query Exception: " + ex.Message);
            }

            var staticProducts = GetStaticProducts();
            return ApplyFilter(staticProducts, filter);
        }

        public Product GetProductById(int id)
        {
            try
            {
                string query = @"
SELECT 
    T0.""DocEntry"" AS ""Id"",
    T0.""ItemCode"" AS ""Code"", 
    T0.""ItemName"" AS ""Title"", 
    COALESCE(T2.""FirmName"", 'Generic') AS ""Brand"", 
    COALESCE(T1.""ItmsGrpNam"", 'General') AS ""Category"", 
    COALESCE(T0.""CodeBars"", '') AS ""Ean"", 
    COALESCE(T0.""SWW"", '') AS ""Oe"", 
    COALESCE(T0.""PictName"", 'default-part.jpg') AS ""Image"", 
    COALESCE(T0.""UserText"", '') AS ""Compatibility"",
    COALESCE(T0.""MinOrdrQty"", 1) AS ""Moq"",
    COALESCE(P.""Price"", 0) AS ""Price"",
    COALESCE(W1.""OnHand"", 0) AS ""SharjahStock"",
    COALESCE(W2.""OnHand"", 0) AS ""JebelStock"",
    COALESCE(W_ALL.""Transit"", 0) AS ""TransitStock""
FROM ""OITM"" T0
LEFT JOIN ""OITB"" T1 ON T0.""ItmsGrpCod"" = T1.""ItmsGrpCod""
LEFT JOIN ""OMRC"" T2 ON T0.""FirmCode"" = T2.""FirmCode""
LEFT JOIN ""ITM1"" P ON T0.""ItemCode"" = P.""ItemCode"" AND P.""PriceList"" = 1
LEFT JOIN ""OITW"" W1 ON T0.""ItemCode"" = W1.""ItemCode"" AND (W1.""WhsCode"" = '01' OR W1.""WhsCode"" = 'SHJ')
LEFT JOIN ""OITW"" W2 ON T0.""ItemCode"" = W2.""ItemCode"" AND (W2.""WhsCode"" = '02' OR W2.""WhsCode"" = 'JBL')
LEFT JOIN (
    SELECT ""ItemCode"", SUM(""OnOrder"") AS ""Transit"" 
    FROM ""OITW"" 
    GROUP BY ""ItemCode""
) W_ALL ON T0.""ItemCode"" = W_ALL.""ItemCode""
WHERE T0.""DocEntry"" = " + id;

                DataTable dt = _hanaHelper.ExecuteDataTable(query);
                if (dt != null && dt.Rows.Count > 0)
                {
                    return MapRowToProduct(dt.Rows[0]);
                }
            }
            catch { }

            var p = _db.Products.FirstOrDefault(x => x.Id == id);
            if (p != null) ParseSpecsJson(p);
            return p;
        }

        public SummaryStats GetSummaryStats()
        {
            var stats = new SummaryStats();
            var products = GetAllProducts().ToList();
            stats.AvailableProducts = products.Count;
            stats.ReadyStock = products.Sum(p => p.SharjahStock + p.JebelStock);
            stats.InTransit = products.Sum(p => p.TransitStock);
            stats.SpecialOffers = products.Count(p => p.IsOffer);
            return stats;
        }

        public List<string> GetCategories()
        {
            try
            {
                string query = @"SELECT DISTINCT ""ItmsGrpNam"" FROM ""OITB"" WHERE ""ItmsGrpNam"" IS NOT NULL ORDER BY ""ItmsGrpNam""";
                DataTable dt = _hanaHelper.ExecuteDataTable(query);
                if (dt != null && dt.Rows.Count > 0)
                {
                    List<string> categories = new List<string>();
                    foreach (DataRow row in dt.Rows)
                    {
                        var cat = row[0]?.ToString();
                        if (!string.IsNullOrWhiteSpace(cat)) categories.Add(cat);
                    }
                    if (categories.Count > 0) return categories;
                }
            }
            catch { }

            return _db.Products.Select(p => p.Category).Where(c => c != null && c != "").Distinct().OrderBy(c => c).ToList();
        }

        public List<string> GetBrands()
        {
            try
            {
                string query = @"SELECT DISTINCT ""FirmName"" FROM ""OMRC"" WHERE ""FirmName"" IS NOT NULL ORDER BY ""FirmName""";
                DataTable dt = _hanaHelper.ExecuteDataTable(query);
                if (dt != null && dt.Rows.Count > 0)
                {
                    List<string> brands = new List<string>();
                    foreach (DataRow row in dt.Rows)
                    {
                        var brand = row[0]?.ToString();
                        if (!string.IsNullOrWhiteSpace(brand)) brands.Add(brand);
                    }
                    if (brands.Count > 0) return brands;
                }
            }
            catch { }

            return _db.Products.Select(p => p.Brand).Where(b => b != null && b != "").Distinct().OrderBy(b => b).ToList();
        }

        private List<Product> GetProductsFromHana(ProductFilter filter)
        {
            string query = @"
SELECT 
    T0.""DocEntry"" AS ""Id"",
    T0.""ItemCode"" AS ""Code"", 
    T0.""ItemName"" AS ""Title"", 
    COALESCE(T2.""FirmName"", 'Generic') AS ""Brand"", 
    COALESCE(T1.""ItmsGrpNam"", 'General') AS ""Category"", 
    COALESCE(T0.""CodeBars"", '') AS ""Ean"", 
    COALESCE(T0.""SWW"", '') AS ""Oe"", 
    COALESCE(T0.""PictName"", 'default-part.jpg') AS ""Image"", 
    COALESCE(T0.""UserText"", '') AS ""Compatibility"",
    COALESCE(T0.""MinOrdrQty"", 1) AS ""Moq"",
    COALESCE(P.""Price"", 0) AS ""Price"",
    COALESCE(W1.""OnHand"", 0) AS ""SharjahStock"",
    COALESCE(W2.""OnHand"", 0) AS ""JebelStock"",
    COALESCE(W_ALL.""Transit"", 0) AS ""TransitStock""
FROM ""OITM"" T0
LEFT JOIN ""OITB"" T1 ON T0.""ItmsGrpCod"" = T1.""ItmsGrpCod""
LEFT JOIN ""OMRC"" T2 ON T0.""FirmCode"" = T2.""FirmCode""
LEFT JOIN ""ITM1"" P ON T0.""ItemCode"" = P.""ItemCode"" AND P.""PriceList"" = 1
LEFT JOIN ""OITW"" W1 ON T0.""ItemCode"" = W1.""ItemCode"" AND (W1.""WhsCode"" = '01' OR W1.""WhsCode"" = 'SHJ')
LEFT JOIN ""OITW"" W2 ON T0.""ItemCode"" = W2.""ItemCode"" AND (W2.""WhsCode"" = '02' OR W2.""WhsCode"" = 'JBL')
LEFT JOIN (
    SELECT ""ItemCode"", SUM(""OnOrder"") AS ""Transit"" 
    FROM ""OITW"" 
    GROUP BY ""ItemCode""
) W_ALL ON T0.""ItemCode"" = W_ALL.""ItemCode""
WHERE T0.""SellItem"" = 'Y' AND T0.""validFor"" = 'Y'";

            DataTable dt = _hanaHelper.ExecuteDataTable(query);
            if (dt == null || dt.Rows.Count == 0) return null;

            List<Product> products = new List<Product>();
            foreach (DataRow row in dt.Rows)
            {
                products.Add(MapRowToProduct(row));
            }

            // Apply filter logic in memory
            return ApplyFilter(products, filter);
        }

        private Product MapRowToProduct(DataRow row)
        {
            int id = Convert.ToInt32(row["Id"] != DBNull.Value ? row["Id"] : 0);
            string code = row["Code"]?.ToString() ?? "";
            string title = row["Title"]?.ToString() ?? "";
            string brand = row["Brand"]?.ToString() ?? "Generic";
            string category = row["Category"]?.ToString() ?? "General";
            string rawImage = row["Image"]?.ToString() ?? "";

            if (id == 0)
            {
                id = Math.Abs(code.GetHashCode());
            }

            string assetImage = GetAssetImageForProduct(rawImage, title, category, code, id);

            var p = new Product
            {
                Id = id,
                Code = code,
                Title = title,
                Brand = brand,
                Category = category,
                Ean = row["Ean"]?.ToString() ?? "",
                Oe = row["Oe"]?.ToString() ?? "",
                Image = assetImage,
                Compatibility = row["Compatibility"]?.ToString() ?? "",
                Moq = Convert.ToInt32(row["Moq"] != DBNull.Value ? row["Moq"] : 1),
                Price = Convert.ToDecimal(row["Price"] != DBNull.Value ? row["Price"] : 0m),
                SharjahStock = Convert.ToInt32(row["SharjahStock"] != DBNull.Value ? row["SharjahStock"] : 0),
                JebelStock = Convert.ToInt32(row["JebelStock"] != DBNull.Value ? row["JebelStock"] : 0),
                TransitStock = Convert.ToInt32(row["TransitStock"] != DBNull.Value ? row["TransitStock"] : 0),
                IsOffer = false
            };

            return p;
        }

        private string GetAssetImageForProduct(string rawImage, string title, string category, string code, int id)
        {
            if (!string.IsNullOrWhiteSpace(rawImage) &&
                (rawImage.StartsWith("/Content/assets/", StringComparison.OrdinalIgnoreCase) ||
                 rawImage.StartsWith("http", StringComparison.OrdinalIgnoreCase)))
            {
                return rawImage;
            }

            string search = $"{title} {category} {code}".ToLowerInvariant();

            if (search.Contains("filter") || search.Contains("air") || search.Contains("oil") || search.Contains("fuel"))
                return "/Content/assets/img/air-filter.svg";
            if (search.Contains("brake") || search.Contains("pad") || search.Contains("disc") || search.Contains("shoe") || search.Contains("caliper"))
                return "/Content/assets/img/brake-pad.svg";
            if (search.Contains("plug") || search.Contains("spark") || search.Contains("ignition") || search.Contains("sensor") || search.Contains("coil") || search.Contains("elec"))
                return "/Content/assets/img/spark-plug.svg";
            if (search.Contains("bearing") || search.Contains("wheel") || search.Contains("suspension") || search.Contains("arm") || search.Contains("joint") || search.Contains("strut") || search.Contains("shock"))
                return "/Content/assets/img/bearing.svg";
            if (search.Contains("belt") || search.Contains("rib") || search.Contains("drive") || search.Contains("timing") || search.Contains("chain"))
                return "/Content/assets/img/drive-belt.svg";
            if (search.Contains("seal") || search.Contains("ring") || search.Contains("gasket") || search.Contains("o-ring") || search.Contains("washer"))
                return "/Content/assets/img/seal-ring.svg";

            string[] assetImages = new string[]
            {
                "/Content/assets/img/air-filter.svg",
                "/Content/assets/img/brake-pad.svg",
                "/Content/assets/img/spark-plug.svg",
                "/Content/assets/img/bearing.svg",
                "/Content/assets/img/drive-belt.svg",
                "/Content/assets/img/seal-ring.svg"
            };

            int index = Math.Abs(id) % assetImages.Length;
            return assetImages[index];
        }

        private List<Product> ApplyFilter(List<Product> list, ProductFilter filter)
        {
            if (filter == null) return list;

            IEnumerable<Product> queryable = list;

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var q = filter.Query.Trim().ToLower();
                queryable = queryable.Where(p =>
                    p.Code.ToLower().Contains(q) ||
                    p.Title.ToLower().Contains(q) ||
                    p.Brand.ToLower().Contains(q) ||
                    p.Ean.ToLower().Contains(q) ||
                    p.Oe.ToLower().Contains(q) ||
                    p.Compatibility.ToLower().Contains(q)
                );
            }

            if (!string.IsNullOrWhiteSpace(filter.Category) && !filter.Category.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                queryable = queryable.Where(p => p.Category.Equals(filter.Category, StringComparison.OrdinalIgnoreCase));
            }

            if (filter.Brands != null && filter.Brands.Count > 0)
            {
                queryable = queryable.Where(p => filter.Brands.Contains(p.Brand));
            }

            if (filter.MinPrice.HasValue)
            {
                queryable = queryable.Where(p => p.Price >= filter.MinPrice.Value);
            }

            if (filter.MaxPrice.HasValue)
            {
                queryable = queryable.Where(p => p.Price <= filter.MaxPrice.Value);
            }

            if (filter.SharjahOnly)
            {
                queryable = queryable.Where(p => p.SharjahStock > 0);
            }

            if (filter.JebelOnly)
            {
                queryable = queryable.Where(p => p.JebelStock > 0);
            }

            if (filter.OffersOnly)
            {
                queryable = queryable.Where(p => p.IsOffer);
            }

            if (filter.InStockOnly)
            {
                queryable = queryable.Where(p => (p.SharjahStock + p.JebelStock) > 0);
            }

            if (!string.IsNullOrWhiteSpace(filter.SortBy))
            {
                switch (filter.SortBy.ToLower())
                {
                    case "price-low":
                        queryable = queryable.OrderBy(p => p.Price);
                        break;
                    case "price-high":
                        queryable = queryable.OrderByDescending(p => p.Price);
                        break;
                    case "stock":
                        queryable = queryable.OrderByDescending(p => (p.SharjahStock + p.JebelStock));
                        break;
                    default:
                        queryable = queryable.OrderBy(p => p.Id);
                        break;
                }
            }

            return queryable.ToList();
        }

        private List<Product> GetProductsFromLocalDb(ProductFilter filter)
        {
            IQueryable<Product> queryable = _db.Products;

            if (filter != null)
            {
                if (!string.IsNullOrWhiteSpace(filter.Query))
                {
                    var q = filter.Query.Trim().ToLower();
                    queryable = queryable.Where(p =>
                        p.Code.ToLower().Contains(q) ||
                        p.Title.ToLower().Contains(q) ||
                        p.Brand.ToLower().Contains(q) ||
                        p.Ean.ToLower().Contains(q) ||
                        p.Oe.ToLower().Contains(q) ||
                        p.Compatibility.ToLower().Contains(q)
                    );
                }

                if (!string.IsNullOrWhiteSpace(filter.Category) && !filter.Category.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    queryable = queryable.Where(p => p.Category == filter.Category);
                }

                if (filter.Brands != null && filter.Brands.Count > 0)
                {
                    queryable = queryable.Where(p => filter.Brands.Contains(p.Brand));
                }

                if (filter.MinPrice.HasValue)
                {
                    queryable = queryable.Where(p => p.Price >= filter.MinPrice.Value);
                }

                if (filter.MaxPrice.HasValue)
                {
                    queryable = queryable.Where(p => p.Price <= filter.MaxPrice.Value);
                }

                if (filter.SharjahOnly)
                {
                    queryable = queryable.Where(p => p.SharjahStock > 0);
                }

                if (filter.JebelOnly)
                {
                    queryable = queryable.Where(p => p.JebelStock > 0);
                }

                if (filter.OffersOnly)
                {
                    queryable = queryable.Where(p => p.IsOffer);
                }

                if (filter.InStockOnly)
                {
                    queryable = queryable.Where(p => (p.SharjahStock + p.JebelStock) > 0);
                }

                if (!string.IsNullOrWhiteSpace(filter.SortBy))
                {
                    switch (filter.SortBy.ToLower())
                    {
                        case "price-low":
                            queryable = queryable.OrderBy(p => p.Price);
                            break;
                        case "price-high":
                            queryable = queryable.OrderByDescending(p => p.Price);
                            break;
                        case "stock":
                            queryable = queryable.OrderByDescending(p => (p.SharjahStock + p.JebelStock));
                            break;
                        default:
                            queryable = queryable.OrderBy(p => p.Id);
                            break;
                    }
                }
                else
                {
                    queryable = queryable.OrderBy(p => p.Id);
                }
            }
            else
            {
                queryable = queryable.OrderBy(p => p.Id);
            }

            var list = queryable.ToList();
            foreach (var p in list)
            {
                ParseSpecsJson(p);
            }
            return list;
        }

        private void ParseSpecsJson(Product p)
        {
            if (p != null && p.Specs == null && !string.IsNullOrWhiteSpace(p.SpecsJson))
            {
                try
                {
                    p.Specs = JsonConvert.DeserializeObject<Dictionary<string, string>>(p.SpecsJson) ?? new Dictionary<string, string>();
                }
                catch { }
            }
        }

        public static List<Product> GetStaticProducts()
        {
            return new List<Product>
            {
                new Product
                {
                    Id = 1,
                    Code = "MCP-001",
                    Title = "Seal ring, spark plug tube",
                    Brand = "FEBEST",
                    Category = "Engine",
                    Image = "/Content/assets/img/seal-ring.svg",
                    Ean = "4056111015293",
                    Oe = "10966-AA000",
                    Compatibility = "Subaru / Chery / Mitsubishi",
                    Specs = new Dictionary<string, string> { { "Inner diameter", "24 mm" }, { "Outer diameter", "35 mm" } },
                    SharjahStock = 119,
                    JebelStock = 2368,
                    TransitStock = 180,
                    Price = 6.06m,
                    Moq = 1,
                    IsOffer = true
                },
                new Product
                {
                    Id = 2,
                    Code = "AF-4198",
                    Title = "Premium engine air filter",
                    Brand = "BOSCH",
                    Category = "Filters",
                    Image = "/Content/assets/img/air-filter.svg",
                    Ean = "4047026500198",
                    Oe = "17801-0M030",
                    Compatibility = "Toyota Corolla / Yaris",
                    Specs = new Dictionary<string, string> { { "Length", "278 mm" }, { "Width", "167 mm" } },
                    SharjahStock = 320,
                    JebelStock = 985,
                    TransitStock = 80,
                    Price = 21.75m,
                    Moq = 2,
                    IsOffer = false
                },
                new Product
                {
                    Id = 3,
                    Code = "BP-7740",
                    Title = "Ceramic front brake pad set",
                    Brand = "FEBEST",
                    Category = "Brake",
                    Image = "/Content/assets/img/brake-pad.svg",
                    Ean = "4056111097749",
                    Oe = "D1060-JA00A",
                    Compatibility = "Nissan Altima / Teana",
                    Specs = new Dictionary<string, string> { { "Axle", "Front" }, { "Wear sensor", "Included" } },
                    SharjahStock = 86,
                    JebelStock = 412,
                    TransitStock = 120,
                    Price = 68.50m,
                    Moq = 1,
                    IsOffer = true
                },
                new Product
                {
                    Id = 4,
                    Code = "SP-7164",
                    Title = "Iridium spark plug long life",
                    Brand = "NGK",
                    Category = "Electrical",
                    Image = "/Content/assets/img/spark-plug.svg",
                    Ean = "0087295171646",
                    Oe = "ILZKR7B11",
                    Compatibility = "Honda / Hyundai / Kia",
                    Specs = new Dictionary<string, string> { { "Thread", "M12 x 1.25" }, { "Gap", "1.1 mm" } },
                    SharjahStock = 560,
                    JebelStock = 1900,
                    TransitStock = 0,
                    Price = 31.20m,
                    Moq = 4,
                    IsOffer = false
                },
                new Product
                {
                    Id = 5,
                    Code = "WB-2305",
                    Title = "Front wheel bearing kit",
                    Brand = "SKF",
                    Category = "Suspension",
                    Image = "/Content/assets/img/bearing.svg",
                    Ean = "7316575623052",
                    Oe = "40210-2Y000",
                    Compatibility = "Nissan Maxima / Murano",
                    Specs = new Dictionary<string, string> { { "Inner diameter", "45 mm" }, { "Outer diameter", "84 mm" } },
                    SharjahStock = 45,
                    JebelStock = 98,
                    TransitStock = 60,
                    Price = 145.90m,
                    Moq = 1,
                    IsOffer = false
                },
                new Product
                {
                    Id = 6,
                    Code = "DB-6PK2135",
                    Title = "Multi-rib auxiliary drive belt",
                    Brand = "BOSCH",
                    Category = "Engine",
                    Image = "/Content/assets/img/drive-belt.svg",
                    Ean = "4047025272133",
                    Oe = "6PK2135",
                    Compatibility = "Toyota / Lexus / Mitsubishi",
                    Specs = new Dictionary<string, string> { { "Ribs", "6" }, { "Length", "2135 mm" } },
                    SharjahStock = 210,
                    JebelStock = 640,
                    TransitStock = 150,
                    Price = 47.80m,
                    Moq = 2,
                    IsOffer = true
                }
            };
        }
    }
}
