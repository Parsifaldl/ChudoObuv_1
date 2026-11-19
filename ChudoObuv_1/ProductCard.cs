using System.Windows.Media.Imaging;

namespace ChudoObuv_1
{
    public class ProductCard
    {
        public BitmapImage? Image { get; set; }
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string Stock { get; set; } = "";
        public string StockToolTip { get; set; } = "";
        public string Composition { get; set; } = "";

        public bool IsDiscounted { get; set; }
        public bool IsLowStock { get; set; }

        public string PriceText { get; set; } = "";
        public string DiscountBadge { get; set; } = "";
        public string OldPriceText { get; set; } = "";
        public string NewPriceText { get; set; } = "";
    }
}