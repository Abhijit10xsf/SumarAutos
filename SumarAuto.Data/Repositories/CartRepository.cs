using System;
using System.Collections.Generic;
using System.Linq;
using SumarAuto.Data.Interfaces;
using SumarAuto.Data.Entities;

namespace SumarAuto.Data.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly SumarDbContext _db;
        private readonly IProductRepository _productRepository;

        public CartRepository()
        {
            _db = new SumarDbContext();
            _productRepository = new ProductRepository(_db);
        }

        public CartRepository(IProductRepository productRepository)
        {
            _db = new SumarDbContext();
            _productRepository = productRepository ?? new ProductRepository(_db);
        }

        public CartRepository(SumarDbContext db, IProductRepository productRepository = null)
        {
            _db = db ?? new SumarDbContext();
            _productRepository = productRepository ?? new ProductRepository(_db);
        }

        public CartSummary GetCart(int userId)
        {
            var summary = new CartSummary();
            if (userId <= 0) return summary;

            var cart = GetOrCreateCart(userId);
            var cartItems = _db.CartItems.Where(ci => ci.CartId == cart.Id).ToList();

            foreach (var ci in cartItems)
            {
                var product = _productRepository.GetProductById(ci.ProductId);
                if (product != null)
                {
                    ci.Product = product;
                    summary.Items.Add(ci);
                }
            }

            return summary;
        }

        public void AddToCart(int productId, int quantity, int userId)
        {
            if (userId <= 0) return;
            var product = _productRepository.GetProductById(productId);
            if (product == null) return;

            var cart = GetOrCreateCart(userId);
            var existingItem = _db.CartItems.FirstOrDefault(ci => ci.CartId == cart.Id && ci.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                int finalQty = quantity < product.Moq ? product.Moq : quantity;
                _db.CartItems.Add(new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = finalQty,
                    CreatedAt = DateTime.Now
                });
            }

            _db.SaveChanges();
        }

        public void UpdateQuantity(int productId, int quantity, int userId)
        {
            if (userId <= 0) return;
            if (quantity <= 0)
            {
                RemoveFromCart(productId, userId);
                return;
            }

            var cart = GetOrCreateCart(userId);
            var item = _db.CartItems.FirstOrDefault(ci => ci.CartId == cart.Id && ci.ProductId == productId);
            if (item != null)
            {
                item.Quantity = quantity;
                _db.SaveChanges();
            }
        }

        public void RemoveFromCart(int productId, int userId)
        {
            if (userId <= 0) return;
            var cart = GetOrCreateCart(userId);
            var item = _db.CartItems.FirstOrDefault(ci => ci.CartId == cart.Id && ci.ProductId == productId);
            if (item != null)
            {
                _db.CartItems.Remove(item);
                _db.SaveChanges();
            }
        }

        public void ClearCart(int userId)
        {
            if (userId <= 0) return;
            var cart = GetOrCreateCart(userId);
            var items = _db.CartItems.Where(ci => ci.CartId == cart.Id).ToList();
            if (items.Any())
            {
                _db.CartItems.RemoveRange(items);
                _db.SaveChanges();
            }
        }

        private Cart GetOrCreateCart(int userId)
        {
            var cart = _db.Carts.FirstOrDefault(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _db.Carts.Add(cart);
                _db.SaveChanges();
            }
            return cart;
        }
    }
}
