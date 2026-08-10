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

        private static readonly object CacheLock = new object();
        private static List<Product> _cachedProducts = null;
        private static Dictionary<int, Product> _cachedProductsById = null;
        private static DateTime _lastProductCacheTime = DateTime.MinValue;
        private static readonly TimeSpan ProductCacheTtl = TimeSpan.FromMinutes(5);

        private static List<string> _cachedCategories = null;
        private static DateTime _lastCategoryCacheTime = DateTime.MinValue;

        private static List<string> _cachedBrands = null;
        private static DateTime _lastBrandCacheTime = DateTime.MinValue;
        private static readonly TimeSpan MetaCacheTtl = TimeSpan.FromMinutes(15);

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

        public static void InvalidateCache()
        {
            lock (CacheLock)
            {
                _cachedProducts = null;
                _cachedProductsById = null;
                _cachedCategories = null;
                _cachedBrands = null;
                _lastProductCacheTime = DateTime.MinValue;
                _lastCategoryCacheTime = DateTime.MinValue;
                _lastBrandCacheTime = DateTime.MinValue;
            }
        }

        private List<Product> GetOrLoadProducts()
        {
            if (_cachedProducts != null && (DateTime.Now - _lastProductCacheTime) < ProductCacheTtl)
            {
                return _cachedProducts;
            }

            lock (CacheLock)
            {
                if (_cachedProducts != null && (DateTime.Now - _lastProductCacheTime) < ProductCacheTtl)
                {
                    return _cachedProducts;
                }

                List<Product> products = null;
                try
                {
                    products = GetProductsFromHanaRaw();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("HANA Query Exception: " + ex.Message);
                }

                if (products == null || products.Count == 0)
                {
                    products = GetProductsFromLocalDb(null);
                }

                _cachedProducts = products ?? new List<Product>();

                var dict = new Dictionary<int, Product>();
                foreach (var p in _cachedProducts)
                {
                    dict[p.Id] = p;
                }
                _cachedProductsById = dict;

                _lastProductCacheTime = DateTime.Now;
                return _cachedProducts;
            }
        }

        public IEnumerable<Product> GetAllProducts(ProductFilter filter = null)
        {
            var baseProducts = GetOrLoadProducts();
            return ApplyFilter(baseProducts, filter).Take(100);
        }

        public Product GetProductById(int id)
        {
            var baseProducts = GetOrLoadProducts();
            if (_cachedProductsById != null && _cachedProductsById.TryGetValue(id, out var product))
            {
                return product;
            }
            return baseProducts?.FirstOrDefault(p => p.Id == id);
        }

        public SummaryStats GetSummaryStats()
        {
            var stats = new SummaryStats();
            var products = GetOrLoadProducts();
            stats.AvailableProducts = products.Count;
            stats.ReadyStock = products.Sum(p => p.SharjahStock + p.JebelStock);
            stats.InTransit = products.Sum(p => p.TransitStock);
            stats.SpecialOffers = products.Count(p => p.IsOffer);
            return stats;
        }

        public List<string> GetCategories()
        {
            if (_cachedCategories != null && (DateTime.Now - _lastCategoryCacheTime) < MetaCacheTtl)
            {
                return _cachedCategories;
            }

            lock (CacheLock)
            {
                if (_cachedCategories != null && (DateTime.Now - _lastCategoryCacheTime) < MetaCacheTtl)
                {
                    return _cachedCategories;
                }

                List<string> categories = new List<string>();
                try
                {
                    string query = @"SELECT DISTINCT ""ItmsGrpNam"" FROM ""OITB"" WHERE ""ItmsGrpNam"" IS NOT NULL ORDER BY ""ItmsGrpNam""";
                    DataTable dt = _hanaHelper.ExecuteDataTable(query);
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            var cat = row[0]?.ToString();
                            if (!string.IsNullOrWhiteSpace(cat)) categories.Add(cat);
                        }
                    }
                }
                catch { }

                _cachedCategories = categories;
                _lastCategoryCacheTime = DateTime.Now;
                return _cachedCategories;
            }
        }

        public List<string> GetBrands()
        {
            if (_cachedBrands != null && (DateTime.Now - _lastBrandCacheTime) < MetaCacheTtl)
            {
                return _cachedBrands;
            }

            lock (CacheLock)
            {
                if (_cachedBrands != null && (DateTime.Now - _lastBrandCacheTime) < MetaCacheTtl)
                {
                    return _cachedBrands;
                }

                List<string> brands = new List<string>();
                try
                {
                    string query = @"SELECT DISTINCT ""FirmName"" FROM ""OMRC"" WHERE ""FirmName"" IS NOT NULL ORDER BY ""FirmName""";
                    DataTable dt = _hanaHelper.ExecuteDataTable(query);
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            var brand = row[0]?.ToString();
                            if (!string.IsNullOrWhiteSpace(brand)) brands.Add(brand);
                        }
                    }
                }
                catch { }

                _cachedBrands = brands;
                _lastBrandCacheTime = DateTime.Now;
                return _cachedBrands;
            }
        }

        private List<Product> GetProductsFromHanaRaw()
        {
            string query = @"
SELECT 
    T0.""ItemCode"" AS ""Code"", 
    T0.""ItemName"" AS ""Title"", 
    COALESCE(T2.""FirmName"", 'Generic') AS ""Brand"", 
    COALESCE(T1.""ItmsGrpNam"", 'General') AS ""Category"", 
    COALESCE(T0.""CodeBars"", '') AS ""Ean"", 
    COALESCE(T0.""SWW"", '') AS ""Oe"", 
    COALESCE(T0.""U_Image_1"", 'default-part.jpg') AS ""Image"", 
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
ORDER BY T0.""ItemCode"" ASC
LIMIT 100";

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

            return products;
        }

        private static object GetRowValue(DataRow row, params string[] names)
        {
            if (row == null || row.Table == null) return null;

            foreach (var name in names)
            {
                if (row.Table.Columns.Contains(name) && row[name] != DBNull.Value)
                {
                    return row[name];
                }
            }

            foreach (DataColumn col in row.Table.Columns)
            {
                foreach (var name in names)
                {
                    if (col.ColumnName.Equals(name, StringComparison.OrdinalIgnoreCase) && row[col] != DBNull.Value)
                    {
                        return row[col];
                    }
                }
            }

            return null;
        }

        private Product MapRowToProduct(DataRow row)
        {
            string code = GetRowValue(row, "Code", "ItemCode")?.ToString() ?? "";
            string title = GetRowValue(row, "Title", "ItemName")?.ToString() ?? "";
            string brand = GetRowValue(row, "Brand", "FirmName")?.ToString() ?? "Generic";
            string category = GetRowValue(row, "Category", "ItmsGrpNam")?.ToString() ?? "General";
            string rawImage = GetRowValue(row, "Image", "PictName")?.ToString() ?? "";
            string ean = GetRowValue(row, "Ean", "CodeBars")?.ToString() ?? "";
            string oe = GetRowValue(row, "Oe", "SWW")?.ToString() ?? "";
            string comp = GetRowValue(row, "Compatibility", "UserText")?.ToString() ?? "";

            object idVal = GetRowValue(row, "Id", "DocEntry");
            int id = 0;
            if (idVal != null)
            {
                int.TryParse(idVal.ToString(), out id);
            }
            if (id == 0 && !string.IsNullOrEmpty(code))
            {
                id = Math.Abs(code.GetHashCode());
            }

            object moqVal = GetRowValue(row, "Moq", "MinOrdrQty");
            int moq = 1;
            if (moqVal != null) int.TryParse(moqVal.ToString(), out moq);
            if (moq <= 0) moq = 1;

            object priceVal = GetRowValue(row, "Price");
            decimal price = 0m;
            if (priceVal != null) decimal.TryParse(priceVal.ToString(), out price);

            object sharjahVal = GetRowValue(row, "SharjahStock", "OnHand");
            int sharjah = 0;
            if (sharjahVal != null) int.TryParse(sharjahVal.ToString(), out sharjah);

            object jebelVal = GetRowValue(row, "JebelStock");
            int jebel = 0;
            if (jebelVal != null) int.TryParse(jebelVal.ToString(), out jebel);

            object transitVal = GetRowValue(row, "TransitStock", "Transit");
            int transit = 0;
            if (transitVal != null) int.TryParse(transitVal.ToString(), out transit);

            string assetImage = GetAssetImageForProduct(rawImage, title, category, code, id);

            var p = new Product
            {
                Id = id,
                Code = code,
                Title = title,
                Brand = brand,
                Category = category,
                Ean = ean,
                Oe = oe,
                Image = assetImage,
                Compatibility = comp,
                Moq = moq,
                Price = price,
                SharjahStock = sharjah,
                JebelStock = jebel,
                TransitStock = transit,
                IsOffer = false
            };

            return p;
        }

        private string GetAssetImageForProduct(string rawImage, string title, string category, string code, int id)
        {
            if (!string.IsNullOrWhiteSpace(rawImage) &&
                (rawImage.StartsWith("/Content/assets/", StringComparison.OrdinalIgnoreCase) ||
                 rawImage.StartsWith("~/Content/assets/", StringComparison.OrdinalIgnoreCase) ||
                 rawImage.StartsWith("http", StringComparison.OrdinalIgnoreCase)))
            {
                if (rawImage.StartsWith("/Content/assets/", StringComparison.OrdinalIgnoreCase))
                {
                    return "~" + rawImage;
                }
                return rawImage;
            }

            string search = $"{title} {category} {code}".ToLowerInvariant();

            if (search.Contains("filter") || search.Contains("air") || search.Contains("oil") || search.Contains("fuel"))
                return "~/Content/assets/img/air-filter.svg";
            if (search.Contains("brake") || search.Contains("pad") || search.Contains("disc") || search.Contains("shoe") || search.Contains("caliper"))
                return "~/Content/assets/img/brake-pad.svg";
            if (search.Contains("plug") || search.Contains("spark") || search.Contains("ignition") || search.Contains("sensor") || search.Contains("coil") || search.Contains("elec"))
                return "~/Content/assets/img/spark-plug.svg";
            if (search.Contains("bearing") || search.Contains("wheel") || search.Contains("suspension") || search.Contains("arm") || search.Contains("joint") || search.Contains("strut") || search.Contains("shock"))
                return "~/Content/assets/img/bearing.svg";
            if (search.Contains("belt") || search.Contains("rib") || search.Contains("drive") || search.Contains("timing") || search.Contains("chain"))
                return "~/Content/assets/img/drive-belt.svg";
            if (search.Contains("seal") || search.Contains("ring") || search.Contains("gasket") || search.Contains("o-ring") || search.Contains("washer"))
                return "~/Content/assets/img/seal-ring.svg";

            string[] assetImages = new string[]
            {
                "~/Content/assets/img/air-filter.svg",
                "~/Content/assets/img/brake-pad.svg",
                "~/Content/assets/img/spark-plug.svg",
                "~/Content/assets/img/bearing.svg",
                "~/Content/assets/img/drive-belt.svg",
                "~/Content/assets/img/seal-ring.svg"
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
