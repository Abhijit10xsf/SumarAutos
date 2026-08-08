using System.Collections.Generic;
using SumarAuto.Data.Entities;

namespace SumarAuto.Data.Interfaces
{
    public interface IProductRepository
    {
        IEnumerable<Product> GetAllProducts(ProductFilter filter = null);
        Product GetProductById(int id);
        SummaryStats GetSummaryStats();
        List<string> GetCategories();
        List<string> GetBrands();
    }
}
