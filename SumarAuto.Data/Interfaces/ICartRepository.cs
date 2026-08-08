using SumarAuto.Data.Entities;

namespace SumarAuto.Data.Interfaces
{
    public interface ICartRepository
    {
        CartSummary GetCart(int userId);
        void AddToCart(int productId, int quantity, int userId);
        void UpdateQuantity(int productId, int quantity, int userId);
        void RemoveFromCart(int productId, int userId);
        void ClearCart(int userId);
    }
}
