using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SumarAuto.Data.Interfaces;
using SumarAuto.Data.Entities;

namespace SumarAuto.Data.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly IProductRepository _productRepository;
        private static readonly ConcurrentDictionary<int, CartSummary> FallbackCarts = new ConcurrentDictionary<int, CartSummary>();
        private const string SessionCartKey = "B2B_Wholesale_Cart";

        public CartRepository()
        {
            _productRepository = new ProductRepository();
        }

        public CartRepository(IProductRepository productRepository)
        {
            _productRepository = productRepository ?? new ProductRepository();
        }

        public CartRepository(SumarDbContext db, IProductRepository productRepository = null)
        {
            _productRepository = productRepository ?? new ProductRepository(db);
        }

        private CartSummary GetSessionCart(int userId)
        {
            if (userId <= 0) userId = 1;

            try
            {
                if (HttpContext.Current != null && HttpContext.Current.Session != null)
                {
                    var cart = HttpContext.Current.Session[SessionCartKey] as CartSummary;
                    if (cart == null)
                    {
                        cart = new CartSummary();
                        HttpContext.Current.Session[SessionCartKey] = cart;
                    }
                    return cart;
                }
            }
            catch { }

            return FallbackCarts.GetOrAdd(userId, u => new CartSummary());
        }

        public CartSummary GetCart(int userId)
        {
            var cart = GetSessionCart(userId);
            foreach (var item in cart.Items)
            {
                if (item.Product == null && item.ProductId > 0)
                {
                    item.Product = _productRepository.GetProductById(item.ProductId);
                }
            }
            return cart;
        }

        public void AddToCart(int productId, int quantity, int userId)
        {
            var product = _productRepository.GetProductById(productId);
            if (product == null) return;

            var cart = GetSessionCart(userId);
            lock (cart)
            {
                var existing = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (existing != null)
                {
                    existing.Quantity += quantity;
                }
                else
                {
                    int moq = product.Moq > 0 ? product.Moq : 1;
                    int finalQty = quantity < moq ? moq : quantity;
                    cart.Items.Add(new CartItem
                    {
                        Id = cart.Items.Count + 1,
                        CartId = userId,
                        ProductId = productId,
                        Quantity = finalQty,
                        Product = product,
                        CreatedAt = DateTime.Now
                    });
                }
            }
        }

        public void UpdateQuantity(int productId, int quantity, int userId)
        {
            if (quantity <= 0)
            {
                RemoveFromCart(productId, userId);
                return;
            }

            var cart = GetSessionCart(userId);
            lock (cart)
            {
                var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (item != null)
                {
                    item.Quantity = quantity;
                }
            }
        }

        public void RemoveFromCart(int productId, int userId)
        {
            var cart = GetSessionCart(userId);
            lock (cart)
            {
                cart.Items.RemoveAll(i => i.ProductId == productId);
            }
        }

        public void ClearCart(int userId)
        {
            var cart = GetSessionCart(userId);
            lock (cart)
            {
                cart.Items.Clear();
            }
        }
    }
}
