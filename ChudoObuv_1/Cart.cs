using System.Collections.Generic;

namespace ChudoObuv_1
{
    public static class Cart
    {
        public static List<CartItem> Items { get; } = new List<CartItem>();
    }

    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductTitle { get; set; } = "";
        public int SizeId { get; set; }
        public string SizeName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public decimal Sum => Quantity * UnitPrice;
    }
}