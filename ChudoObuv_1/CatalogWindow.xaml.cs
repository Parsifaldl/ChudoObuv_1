using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using ChudoObuv_1.Models;
using Microsoft.EntityFrameworkCore;

namespace ChudoObuv_1
{
    public partial class CatalogWindow : Window
    {
        private const decimal DiscountShare = 0.25m;
        private const int LowStockLimit = 3;
        private const int MuchStockLimit = 5;

        private readonly Dictionary<string, BitmapImage> _imageCache =
            new Dictionary<string, BitmapImage>();

        public CatalogWindow()
        {
            InitializeComponent();
            LoadProducts();
        }

        private void LoadProducts()
        {
            try
            {
                using var db = new AppDbContext();

                var stock = db.StockItems
                    .GroupBy(s => s.FkIdProducts)
                    .ToDictionary(g => g.Key, g => g.Sum(x => (int?)x.Quantity) ?? 0);

                var today = DateTime.Today;
                var thisMonthStart = new DateTime(today.Year, today.Month, 1);
                var prevMonthStart = thisMonthStart.AddMonths(-1);
                var prevMonthEnd = thisMonthStart.AddDays(-1);

                var orderedInPrevMonth = db.OrderItems
                    .Join(db.Orders,
                          oi => oi.FkIdOrders,
                          o => o.IdOrders,
                          (oi, o) => new { oi.FkIdProducts, o.OrderDate })
                    .Where(x => x.OrderDate >= prevMonthStart && x.OrderDate <= prevMonthEnd)
                    .Select(x => x.FkIdProducts)
                    .Distinct()
                    .ToList();

                var cards = db.Products
                    .Include(p => p.FkIdManufacturersNavigation)
                    .Include(p => p.FkIdCategoriesNavigation)
                    .AsNoTracking()
                    .OrderBy(p => p.NameProduct)
                    .ToList()
                    .Select(p =>
                    {
                        int totalQty = stock.TryGetValue(p.IdProducts, out int q) ? q : 0;
                        bool hasOrdersInPrevMonth = orderedInPrevMonth.Contains(p.IdProducts);
                        bool isDiscounted = !hasOrdersInPrevMonth;
                        decimal basePrice = p.Price ?? 0m;

                        return new ProductCard
                        {
                            Image = LoadProductImage(p.Image),
                            Title = (p.FkIdManufacturersNavigation?.NameManufacturer ?? "—")
                                    + " | " + (p.NameProduct ?? "—"),
                            Category = "Категория: " + (p.FkIdCategoriesNavigation?.NameCategory ?? "—"),
                            Stock = "Количество: " + (totalQty > MuchStockLimit ? "много" : "мало"),
                            StockToolTip = $"Доступно к заказу: {totalQty} пар.",
                            Composition = "Состав: " +
                                (string.IsNullOrWhiteSpace(p.Composition) ? "—" : p.Composition),
                            IsDiscounted = isDiscounted,
                            IsLowStock = totalQty <= LowStockLimit,
                            PriceText = "Цена: " + FormatMoney(basePrice),
                            DiscountBadge = "-25%",
                            OldPriceText = FormatMoney(basePrice),
                            NewPriceText = FormatMoney(CalculateDiscountedPrice(basePrice))
                        };
                    })
                    .ToList();

                ProductsList.ItemsSource = cards;
                HeaderInfo.Text = $"Каталог товаров ({cards.Count})";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить товары из базы:\n\n" + ex.Message,
                    "Чудо-обувь — ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static decimal CalculateDiscountedPrice(decimal basePrice)
        {
            return Math.Round(basePrice * (1 - DiscountShare), 2);
        }

        private static string FormatMoney(decimal value)
        {
            return value.ToString("N2") + " руб.";
        }

        private BitmapImage? LoadProductImage(string? imagePath)
        {
            if (!string.IsNullOrWhiteSpace(imagePath))
            {
                string[] candidates =
                {
                    imagePath,
                    Path.Combine(AppContext.BaseDirectory, imagePath),
                    Path.Combine(AppContext.BaseDirectory, "Images", imagePath),
                    Path.Combine(AppContext.BaseDirectory, "Import", imagePath)
                };

                string? found = candidates.FirstOrDefault(File.Exists);
                if (found != null)
                {
                    return LoadBitmap(found);
                }
            }

            return LoadPlaceholder();
        }

        private BitmapImage? LoadBitmap(string path)
        {
            if (_imageCache.TryGetValue(path, out BitmapImage? cached))
            {
                return cached;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                _imageCache[path] = bitmap;
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private BitmapImage? LoadPlaceholder()
        {
            string[] probes =
            {
                Path.Combine(AppContext.BaseDirectory, "picture.png"),
                Path.Combine(AppContext.BaseDirectory, "Images", "picture.png"),
                Path.Combine(AppContext.BaseDirectory, "Import", "picture.png")
            };

            foreach (string probe in probes)
            {
                BitmapImage? bitmap = LoadBitmap(probe);
                if (bitmap != null)
                {
                    return bitmap;
                }
            }

            try
            {
                var bitmap = new BitmapImage(new Uri("pack://application:,,,/picture.png"));
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}