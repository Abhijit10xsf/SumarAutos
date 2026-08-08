using System;

namespace SumarAuto.Data.Entities
{
    public class CartItem
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public Product Product { get; set; }
        public decimal ItemTotal => (Product?.Price ?? 0) * Quantity;
    }

    public class CartSummary
    {
        public System.Collections.Generic.List<CartItem> Items { get; set; } = new System.Collections.Generic.List<CartItem>();
        public int TotalItemsCount
        {
            get
            {
                int count = 0;
                if (Items != null)
                {
                    foreach (var i in Items) count += i.Quantity;
                }
                return count;
            }
        }
        public decimal Subtotal
        {
            get
            {
                decimal sum = 0;
                if (Items != null)
                {
                    foreach (var i in Items) sum += i.ItemTotal;
                }
                return sum;
            }
        }
        public decimal Vat => Subtotal * 0.05m;
        public decimal GrandTotal => Subtotal + Vat;
    }
}
