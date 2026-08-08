using System;
using System.Collections.Generic;
using System.Linq;
using SumarAuto.Data.Interfaces;
using SumarAuto.Data.Entities;

namespace SumarAuto.Data.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly SumarDbContext _db;

        public OrderRepository()
        {
            _db = new SumarDbContext();
        }

        public OrderRepository(SumarDbContext db)
        {
            _db = db ?? new SumarDbContext();
        }

        public int CreateOrder(Order order, CartSummary cart)
        {
            if (order == null || cart == null || cart.Items == null || cart.Items.Count == 0)
                return 0;

            using (var tx = _db.Database.BeginTransaction())
            {
                try
                {
                    order.Subtotal = cart.Subtotal;
                    order.VatAmount = cart.Vat;
                    order.GrandTotal = cart.GrandTotal;
                    order.OrderStatus = "Processing";
                    order.CreatedAt = DateTime.Now;

                    _db.Orders.Add(order);
                    _db.SaveChanges();

                    foreach (var item in cart.Items)
                    {
                        var detail = new OrderDetail
                        {
                            OrderId = order.Id,
                            ProductId = item.ProductId,
                            ProductCode = item.Product.Code ?? "",
                            ProductTitle = item.Product.Title ?? "",
                            UnitPrice = item.Product.Price,
                            Quantity = item.Quantity
                        };
                        _db.OrderDetails.Add(detail);

                        // Decrement stock in DB
                        var product = _db.Products.FirstOrDefault(p => p.Id == item.ProductId);
                        if (product != null)
                        {
                            if (product.SharjahStock >= item.Quantity)
                            {
                                product.SharjahStock -= item.Quantity;
                            }
                            else
                            {
                                product.SharjahStock = 0;
                            }
                        }
                    }

                    _db.SaveChanges();
                    tx.Commit();
                    return order.Id;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public Order GetOrderById(int id)
        {
            var order = _db.Orders.FirstOrDefault(o => o.Id == id);
            if (order != null)
            {
                order.Details = _db.OrderDetails.Where(d => d.OrderId == order.Id).ToList();
            }
            return order;
        }

        public Order GetOrderByNumber(string orderNumber)
        {
            var order = _db.Orders.FirstOrDefault(o => o.OrderNumber == orderNumber);
            if (order != null)
            {
                order.Details = _db.OrderDetails.Where(d => d.OrderId == order.Id).ToList();
            }
            return order;
        }

        public IEnumerable<Order> GetOrdersByUserId(int userId)
        {
            var list = _db.Orders.Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt).ToList();
            foreach (var order in list)
            {
                order.Details = _db.OrderDetails.Where(d => d.OrderId == order.Id).ToList();
            }
            return list;
        }
    }
}
