using System;
using System.Collections.Generic;

namespace SumarAuto.Data.Entities
{
    public class OrderDetail
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductCode { get; set; }
        public string ProductTitle { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }

    public class Order
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public int UserId { get; set; }
        public int? ShippingAddressId { get; set; }
        public string FulfillmentMethod { get; set; }
        public string PaymentMethod { get; set; }
        public decimal Subtotal { get; set; }
        public decimal VatAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public string Notes { get; set; }
        public string OrderStatus { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public List<OrderDetail> Details { get; set; } = new List<OrderDetail>();
    }
}
