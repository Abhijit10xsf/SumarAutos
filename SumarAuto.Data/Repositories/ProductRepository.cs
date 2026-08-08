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
                    return hanaProducts.Take(100);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("HANA Query Exception: " + ex.Message);
            }

            return new List<Product>();
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
LEFT JOIN (
    SELECT ""ItemCode"", MIN(""Price"") AS ""Price"" 
    FROM ""ITM1"" 
    WHERE ""PriceList"" = 1 
    GROUP BY ""ItemCode""
) P ON T0.""ItemCode"" = P.""ItemCode""
LEFT JOIN (
    SELECT ""ItemCode"", SUM(""OnHand"") AS ""OnHand"" 
    FROM ""OITW"" 
    WHERE ""WhsCode"" IN ('01', 'SHJ') 
    GROUP BY ""ItemCode""
) W1 ON T0.""ItemCode"" = W1.""ItemCode""
LEFT JOIN (
    SELECT ""ItemCode"", SUM(""OnHand"") AS ""OnHand"" 
    FROM ""OITW"" 
    WHERE ""WhsCode"" IN ('02', 'JBL') 
    GROUP BY ""ItemCode""
) W2 ON T0.""ItemCode"" = W2.""ItemCode""
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetProductById Exception: " + ex.Message);
            }

            return null;
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
                    return categories;
                }
            }
            catch { }

            return new List<string>();
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
                    return brands;
                }
            }
            catch { }

            return new List<string>();
        }

        private List<Product> GetProductsFromHana(ProductFilter filter)
        {
            string query = @"
SELECT TOP 100
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
LEFT JOIN (
    SELECT ""ItemCode"", MIN(""Price"") AS ""Price"" 
    FROM ""ITM1"" 
    WHERE ""PriceList"" = 1 
    GROUP BY ""ItemCode""
) P ON T0.""ItemCode"" = P.""ItemCode""
LEFT JOIN (
    SELECT ""ItemCode"", SUM(""OnHand"") AS ""OnHand"" 
    FROM ""OITW"" 
    WHERE ""WhsCode"" IN ('01', 'SHJ') 
    GROUP BY ""ItemCode""
) W1 ON T0.""ItemCode"" = W1.""ItemCode""
LEFT JOIN (
    SELECT ""ItemCode"", SUM(""OnHand"") AS ""OnHand"" 
    FROM ""OITW"" 
    WHERE ""WhsCode"" IN ('02', 'JBL') 
    GROUP BY ""ItemCode""
) W2 ON T0.""ItemCode"" = W2.""ItemCode""
LEFT JOIN (
    SELECT ""ItemCode"", SUM(""OnOrder"") AS ""Transit"" 
    FROM ""OITW"" 
    GROUP BY ""ItemCode""
) W_ALL ON T0.""ItemCode"" = W_ALL.""ItemCode""
WHERE T0.""SellItem"" = 'Y' AND T0.""validFor"" = 'Y'
ORDER BY T0.""DocEntry"" ASC";

            DataTable dt = _hanaHelper.ExecuteDataTable(query);
            if (dt == null || dt.Rows.Count == 0) return null;

            HashSet<string> seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<Product> products = new List<Product>();
            foreach (DataRow row in dt.Rows)
            {
                var p = MapRowToProduct(row);
                if (!string.IsNullOrWhiteSpace(p.Code) && seenCodes.Add(p.Code))
                {
                    products.Add(p);
                    if (products.Count >= 100) break;
                }
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
    }
}
