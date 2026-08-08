using System.Collections.Generic;
using SumarAuto.Data.Entities;

namespace SumarAuto.Data.Interfaces
{
    public interface IOrderRepository
    {
        int CreateOrder(Order order, CartSummary cart);
        Order GetOrderById(int id);
        Order GetOrderByNumber(string orderNumber);
        IEnumerable<Order> GetOrdersByUserId(int userId);
    }
}
