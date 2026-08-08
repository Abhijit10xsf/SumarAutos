using System;
using System.Collections.Concurrent;
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
        private static readonly ConcurrentDictionary<int, List<CartItem>> InMemoryCarts = new ConcurrentDictionary<int, List<CartItem>>();

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
            if (userId <= 0) userId = 1;
            var summary = new CartSummary();

            try
            {
                var cart = GetOrCreateCartDb(userId);
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Cart DB Exception: " + ex.Message);
                return GetCartInMemory(userId);
            }
        }

        public void AddToCart(int productId, int quantity, int userId)
        {
            if (userId <= 0) userId = 1;
            var product = _productRepository.GetProductById(productId);
            if (product == null) return;

            try
            {
                var cart = GetOrCreateCartDb(userId);
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AddToCart DB Exception: " + ex.Message);
                AddToCartInMemory(productId, quantity, userId, product);
            }
        }

        public void UpdateQuantity(int productId, int quantity, int userId)
        {
            if (userId <= 0) userId = 1;
            if (quantity <= 0)
            {
                RemoveFromCart(productId, userId);
                return;
            }

            try
            {
                var cart = GetOrCreateCartDb(userId);
                var item = _db.CartItems.FirstOrDefault(ci => ci.CartId == cart.Id && ci.ProductId == productId);
                if (item != null)
                {
                    item.Quantity = quantity;
                    _db.SaveChanges();
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("UpdateQuantity DB Exception: " + ex.Message);
            }

            UpdateQuantityInMemory(productId, quantity, userId);
        }

        public void RemoveFromCart(int productId, int userId)
        {
            if (userId <= 0) userId = 1;

            try
            {
                var cart = GetOrCreateCartDb(userId);
                var item = _db.CartItems.FirstOrDefault(ci => ci.CartId == cart.Id && ci.ProductId == productId);
                if (item != null)
                {
                    _db.CartItems.Remove(item);
                    _db.SaveChanges();
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("RemoveFromCart DB Exception: " + ex.Message);
            }

            RemoveFromCartInMemory(productId, userId);
        }

        public void ClearCart(int userId)
        {
            if (userId <= 0) userId = 1;

            try
            {
                var cart = GetOrCreateCartDb(userId);
                var items = _db.CartItems.Where(ci => ci.CartId == cart.Id).ToList();
                if (items.Any())
                {
                    _db.CartItems.RemoveRange(items);
                    _db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ClearCart DB Exception: " + ex.Message);
            }

            ClearCartInMemory(userId);
        }

        private Cart GetOrCreateCartDb(int userId)
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

        #region In-Memory Fallback Cart Methods

        private CartSummary GetCartInMemory(int userId)
        {
            var summary = new CartSummary();
            if (InMemoryCarts.TryGetValue(userId, out var items))
            {
                lock (items)
                {
                    foreach (var item in items)
                    {
                        var product = _productRepository.GetProductById(item.ProductId);
                        if (product != null)
                        {
                            summary.Items.Add(new CartItem
                            {
                                Id = item.Id,
                                CartId = item.CartId,
                                ProductId = item.ProductId,
                                Quantity = item.Quantity,
                                Product = product,
                                CreatedAt = item.CreatedAt
                            });
                        }
                    }
                }
            }
            return summary;
        }

        private void AddToCartInMemory(int productId, int quantity, int userId, Product product)
        {
            var items = InMemoryCarts.GetOrAdd(userId, u => new List<CartItem>());
            lock (items)
            {
                var existing = items.FirstOrDefault(i => i.ProductId == productId);
                if (existing != null)
                {
                    existing.Quantity += quantity;
                }
                else
                {
                    int moq = product != null ? product.Moq : 1;
                    int finalQty = quantity < moq ? moq : quantity;
                    items.Add(new CartItem
                    {
                        Id = items.Count + 1,
                        CartId = userId,
                        ProductId = productId,
                        Quantity = finalQty,
                        Product = product,
                        CreatedAt = DateTime.Now
                    });
                }
            }
        }

        private void UpdateQuantityInMemory(int productId, int quantity, int userId)
        {
            if (InMemoryCarts.TryGetValue(userId, out var items))
            {
                lock (items)
                {
                    var item = items.FirstOrDefault(i => i.ProductId == productId);
                    if (item != null)
                    {
                        item.Quantity = quantity;
                    }
                }
            }
        }

        private void RemoveFromCartInMemory(int productId, int userId)
        {
            if (InMemoryCarts.TryGetValue(userId, out var items))
            {
                lock (items)
                {
                    items.RemoveAll(i => i.ProductId == productId);
                }
            }
        }

        private void ClearCartInMemory(int userId)
        {
            if (InMemoryCarts.TryGetValue(userId, out var items))
            {
                lock (items)
                {
                    items.Clear();
                }
            }
        }

        #endregion
    }
}
