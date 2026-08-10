using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using SumarAuto.Data.Entities;
using SumarAuto.Data.Interfaces;
using SumarAuto.Data.Repositories;

namespace SumarAuto.Client.Controllers
{
    public class OrderController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICartRepository _cartRepository;

        public OrderController()
        {
            _productRepository = new ProductRepository();
            _cartRepository = new CartRepository(_productRepository);
            _orderRepository = new OrderRepository();
        }

        public OrderController(IOrderRepository orderRepository, IProductRepository productRepository, ICartRepository cartRepository)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _cartRepository = cartRepository;
        }

        private User GetCurrentUser()
        {
            return Session["CurrentUser"] as User;
        }

        // GET: Order/Index
        public ActionResult Index()
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Title = "Previous Orders | TradeParts Wholesale";
            ViewBag.CurrentNav = "Orders";
            ViewBag.CurrentUser = currentUser;

            List<Order> userOrders = new List<Order>();

            try
            {
                var dbOrders = _orderRepository.GetOrdersByUserId(currentUser.Id);
                if (dbOrders != null)
                {
                    userOrders = dbOrders.ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error retrieving user orders from DB: " + ex.Message);
            }

            // Ensure images and item totals on details
            foreach (var order in userOrders)
            {
                EnhanceOrderDetails(order);
            }

            // Requirement: "show 2 orders as default in that screen"
            // If the user has fewer than 2 orders in DB, generate default previous orders for the logged-in user
            if (userOrders.Count < 2)
            {
                var defaultOrders = GetDefaultOrdersForUser(currentUser.Id);

                // Add default orders that are not already present
                foreach (var def in defaultOrders)
                {
                    if (!userOrders.Any(o => o.OrderNumber == def.OrderNumber))
                    {
                        userOrders.Add(def);
                    }
                }
            }

            // Sort newest first
            userOrders = userOrders.OrderByDescending(o => o.CreatedAt).ToList();

            return View(userOrders);
        }

        [HttpPost]
        public ActionResult Reorder(int orderId)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            Order targetOrder = null;

            try
            {
                targetOrder = _orderRepository.GetOrderById(orderId);
            }
            catch
            {
            }

            if (targetOrder == null)
            {
                // Check default orders
                targetOrder = GetDefaultOrdersForUser(currentUser.Id).FirstOrDefault(o => o.Id == orderId);
            }

            if (targetOrder != null && targetOrder.Details != null && targetOrder.Details.Count > 0)
            {
                int itemsAdded = 0;
                foreach (var detail in targetOrder.Details)
                {
                    if (detail.ProductId > 0)
                    {
                        _cartRepository.AddToCart(detail.ProductId, detail.Quantity, currentUser.Id);
                        itemsAdded++;
                    }
                }

                TempData["SuccessMessage"] = $"Reordered {itemsAdded} product line(s) from Order #{targetOrder.OrderNumber} into your wholesale cart.";
            }

            return RedirectToAction("Index", "Cart");
        }

        private void EnhanceOrderDetails(Order order)
        {
            if (order == null || order.Details == null) return;

            var allProducts = _productRepository.GetAllProducts().ToDictionary(p => p.Id);

            foreach (var detail in order.Details)
            {
                if (allProducts.TryGetValue(detail.ProductId, out var product))
                {
                    if (string.IsNullOrEmpty(detail.ProductCode)) detail.ProductCode = product.Code;
                    if (string.IsNullOrEmpty(detail.ProductTitle)) detail.ProductTitle = product.Title;
                }
            }
        }

        private List<Order> GetDefaultOrdersForUser(int userId)
        {
            var defaultOrders = new List<Order>();

            // Order 1: Recent Processing Order
            var order1 = new Order
            {
                Id = 9901,
                OrderNumber = "PO-2026-84920",
                UserId = userId,
                OrderStatus = "Processing",
                CreatedAt = DateTime.Now.AddDays(-2).AddHours(-3),
                FulfillmentMethod = "Standard Road Freight (Sharjah Warehouse Hub)",
                PaymentMethod = "Wholesale Credit Account (Net 30 Days)",
                Notes = "Urgent delivery requested for Sharjah Main Workshop Branch.",
                Details = new List<OrderDetail>
                {
                    new OrderDetail
                    {
                        Id = 1,
                        OrderId = 9901,
                        ProductId = 1,
                        ProductCode = "SP-10023",
                        ProductTitle = "Brake Disc Rotor Front Vented",
                        UnitPrice = 145.00m,
                        Quantity = 10
                    },
                    new OrderDetail
                    {
                        Id = 2,
                        OrderId = 9901,
                        ProductId = 2,
                        ProductCode = "OF-88210",
                        ProductTitle = "Synthetic Engine Oil Filter Element",
                        UnitPrice = 32.00m,
                        Quantity = 25
                    },
                    new OrderDetail
                    {
                        Id = 3,
                        OrderId = 9901,
                        ProductId = 3,
                        ProductCode = "SPK-9901",
                        ProductTitle = "Iridium Platinum Spark Plug Pack (Set of 4)",
                        UnitPrice = 85.00m,
                        Quantity = 15
                    }
                }
            };
            order1.Subtotal = order1.Details.Sum(d => d.LineTotal);
            order1.VatAmount = Math.Round(order1.Subtotal * 0.05m, 2);
            order1.GrandTotal = order1.Subtotal + order1.VatAmount;
            defaultOrders.Add(order1);

            // Order 2: Delivered Order
            var order2 = new Order
            {
                Id = 9902,
                OrderNumber = "PO-2026-73105",
                UserId = userId,
                OrderStatus = "Delivered",
                CreatedAt = DateTime.Now.AddDays(-10).AddHours(-5),
                FulfillmentMethod = "Express Courier (Jebel Ali Free Zone)",
                PaymentMethod = "Bank Wire Transfer (Paid)",
                Notes = "Delivered to Central Logistics Gate 3.",
                Details = new List<OrderDetail>
                {
                    new OrderDetail
                    {
                        Id = 4,
                        OrderId = 9902,
                        ProductId = 4,
                        ProductCode = "AF-40012",
                        ProductTitle = "Heavy Duty Engine Air Filter",
                        UnitPrice = 65.00m,
                        Quantity = 20
                    },
                    new OrderDetail
                    {
                        Id = 5,
                        OrderId = 9902,
                        ProductId = 5,
                        ProductCode = "CB-55412",
                        ProductTitle = "Commercial Vehicle Battery 12V 100Ah",
                        UnitPrice = 380.00m,
                        Quantity = 4
                    }
                }
            };
            order2.Subtotal = order2.Details.Sum(d => d.LineTotal);
            order2.VatAmount = Math.Round(order2.Subtotal * 0.05m, 2);
            order2.GrandTotal = order2.Subtotal + order2.VatAmount;
            defaultOrders.Add(order2);

            return defaultOrders;
        }
    }
}
