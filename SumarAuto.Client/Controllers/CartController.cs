using System;
using System.Linq;
using System.Web.Mvc;
using SumarAuto.Data.Entities;
using SumarAuto.Data.Interfaces;
using SumarAuto.Data.Repositories;

namespace SumarAuto.Client.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartRepository _cartRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUserRepository _userRepository;
        private readonly IOrderRepository _orderRepository;

        public CartController()
        {
            _productRepository = new ProductRepository();
            _cartRepository = new CartRepository(_productRepository);
            _userRepository = new UserRepository();
            _orderRepository = new OrderRepository();
        }

        public CartController(ICartRepository cartRepository, IProductRepository productRepository, IUserRepository userRepository, IOrderRepository orderRepository)
        {
            _cartRepository = cartRepository;
            _productRepository = productRepository;
            _userRepository = userRepository;
            _orderRepository = orderRepository;
        }

        private int GetUserId()
        {
            var user = Session["CurrentUser"] as User;
            return user != null ? user.Id : 0;
        }

        public ActionResult Index()
        {
            if (Session["CurrentUser"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Title = "Your Wholesale Cart";
            ViewBag.CurrentNav = "Cart";

            int userId = GetUserId();
            var cart = _cartRepository.GetCart(userId);
            var stats = _productRepository.GetSummaryStats();
            var currentUser = Session["CurrentUser"] as User ?? new User { Id = 1, Username = "SAP_User", EmailId = "" };

            ViewBag.Stats = stats;
            ViewBag.CurrentUser = currentUser;

            return View(cart);
        }

        [HttpPost]
        public ActionResult UpdateQuantity(int productId, int quantity)
        {
            _cartRepository.UpdateQuantity(productId, quantity, GetUserId());
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult RemoveFromCart(int productId)
        {
            _cartRepository.RemoveFromCart(productId, GetUserId());
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult ClearCart()
        {
            _cartRepository.ClearCart(GetUserId());
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Checkout()
        {
            if (Session["CurrentUser"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = GetUserId();
            var cart = _cartRepository.GetCart(userId);
            if (cart.Items.Count == 0)
            {
                TempData["ErrorMessage"] = "Your cart is empty. Please add products to place an order.";
                return RedirectToAction("Index");
            }

            ViewBag.Title = "B2B Checkout & Order Review";
            ViewBag.CurrentNav = "Cart";

            var stats = _productRepository.GetSummaryStats();
            var currentUser = Session["CurrentUser"] as User ?? new User { Id = 1, Username = "SAP_User", EmailId = "" };

            ViewBag.Stats = stats;
            ViewBag.CurrentUser = currentUser;

            return View(cart);
        }

        [HttpPost]
        public ActionResult Checkout(string shippingMethod, string paymentTerms, string notes)
        {
            int userId = GetUserId();
            var cart = _cartRepository.GetCart(userId);
            if (cart.Items.Count == 0)
            {
                TempData["ErrorMessage"] = "Your cart is empty. Please add products to place an order.";
                return RedirectToAction("Index");
            }

            var currentUser = Session["CurrentUser"] as User ?? new User { Id = 1, Username = "SAP_User", EmailId = "" };

            var orderNumber = "PO-2026-" + new Random().Next(10000, 99999);
            var order = new Order
            {
                OrderNumber = orderNumber,
                UserId = currentUser != null ? currentUser.Id : 1,
                FulfillmentMethod = shippingMethod,
                PaymentMethod = paymentTerms,
                Notes = notes,
                Subtotal = cart.Subtotal,
                VatAmount = cart.Vat,
                GrandTotal = cart.GrandTotal,
                OrderStatus = "Processing"
            };

            _orderRepository.CreateOrder(order, cart);

            TempData["SuccessOrderNumber"] = orderNumber;
            TempData["OrderTotal"] = cart.GrandTotal.ToString("N2");

            _cartRepository.ClearCart(userId);

            return RedirectToAction("OrderConfirmation");
        }

        public ActionResult OrderConfirmation()
        {
            ViewBag.Title = "Order Confirmed - TradeParts";
            ViewBag.OrderNumber = TempData["SuccessOrderNumber"] ?? "PO-2026-84920";
            ViewBag.OrderTotal = TempData["OrderTotal"] ?? "0.00";
            return View();
        }
    }
}
