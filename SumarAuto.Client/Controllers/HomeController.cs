using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using SumarAuto.Client.Models;
using SumarAuto.Data.Interfaces;
using SumarAuto.Data.Entities;
using SumarAuto.Data.Repositories;

namespace SumarAuto.Client.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICartRepository _cartRepository;

        public HomeController()
        {
            _productRepository = new ProductRepository();
            _cartRepository = new CartRepository(_productRepository);
        }

        public HomeController(IProductRepository productRepository, ICartRepository cartRepository)
        {
            _productRepository = productRepository;
            _cartRepository = cartRepository;
        }

        private int GetUserId()
        {
            var user = Session["CurrentUser"] as User;
            return user != null ? user.Id : 0;
        }

        public ActionResult Index(ProductFilter filter)
        {
            if (Session["CurrentUser"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Title = "Wholesale Catalog";
            ViewBag.CurrentNav = "Catalog";

            if (filter == null) filter = new ProductFilter();

            var vm = new CatalogViewModel
            {
                Stats = _productRepository.GetSummaryStats(),
                Products = _productRepository.GetAllProducts(filter),
                Categories = _productRepository.GetCategories(),
                Brands = _productRepository.GetBrands(),
                Filter = filter,
                Cart = _cartRepository.GetCart(GetUserId())
            };

            return View(vm);
        }

        [HttpGet]
        public ActionResult GetFilteredProductsJson(ProductFilter filter)
        {
            if (filter == null) filter = new ProductFilter();
            var products = _productRepository.GetAllProducts(filter);

            var list = products.Select(p => new
            {
                id = p.Id,
                code = p.Code,
                title = p.Title,
                brand = p.Brand,
                category = p.Category,
                image = p.Image,
                ean = p.Ean,
                oe = p.Oe,
                compatibility = p.Compatibility,
                specs = p.Specs,
                sharjah = p.SharjahStock,
                jebel = p.JebelStock,
                transit = p.TransitStock,
                price = p.Price,
                moq = p.Moq,
                offer = p.IsOffer,
                totalStock = p.TotalStock
            });

            return Json(new { count = list.Count(), products = list }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AddToCartJson(int productId, int quantity)
        {
            int userId = GetUserId();
            _cartRepository.AddToCart(productId, quantity, userId);
            var cart = _cartRepository.GetCart(userId);

            var dto = new
            {
                TotalItemsCount = cart.TotalItemsCount,
                Subtotal = cart.Subtotal,
                Vat = cart.Vat,
                GrandTotal = cart.GrandTotal,
                Items = cart.Items.Select(i => new
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    ItemTotal = i.ItemTotal,
                    Product = new
                    {
                        Code = i.Product.Code,
                        Title = i.Product.Title,
                        Price = i.Product.Price,
                        Image = i.Product.Image
                    }
                })
            };

            return Json(new { success = true, cart = dto });
        }

        [HttpPost]
        public ActionResult RemoveFromCartJson(int productId)
        {
            int userId = GetUserId();
            _cartRepository.RemoveFromCart(productId, userId);
            var cart = _cartRepository.GetCart(userId);

            var dto = new
            {
                TotalItemsCount = cart.TotalItemsCount,
                Subtotal = cart.Subtotal,
                Vat = cart.Vat,
                GrandTotal = cart.GrandTotal,
                Items = cart.Items.Select(i => new
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    ItemTotal = i.ItemTotal,
                    Product = new
                    {
                        Code = i.Product.Code,
                        Title = i.Product.Title,
                        Price = i.Product.Price,
                        Image = i.Product.Image
                    }
                })
            };

            return Json(new { success = true, cart = dto });
        }

        public ActionResult About()
        {
            ViewBag.Message = "SumarAuto Wholesale Spare Parts Catalog";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "SumarAuto Customer Support";
            return View();
        }
    }
}