using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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

        private readonly UsersImport? _currentUser;

        private List<Product> _products = new List<Product>();
        private Dictionary<int?, int> _stock = new Dictionary<int?, int>();
        private HashSet<int?> _orderedInPrevMonth = new HashSet<int?>();
        private bool _baseLoaded;

        public CatalogWindow() : this(null)
        {
        }

        public CatalogWindow(UsersImport? currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            UserFioText.Text = currentUser == null
                ? ""
                : $"{currentUser.FirstName} {currentUser.Name} {currentUser.LastName}".Trim();
            BtnCart.Visibility = currentUser == null
                ? Visibility.Collapsed
                : Visibility.Visible;

            bool isGuest = currentUser == null;
            SpFilters.Visibility = isGuest ? Visibility.Collapsed : Visibility.Visible;

            bool isStaff = currentUser != null && (currentUser.FkIdRoles == 1 || currentUser.FkIdRoles == 2);
            BtnOrders.Visibility = isStaff ? Visibility.Visible : Visibility.Collapsed;

            LoadSortOptions();
            LoadCategories();
            LoadProducts();
            UpdateCartButton();
        }

        private void LoadSortOptions()
        {
            CbSort.ItemsSource = new List<string>
            {
                "Без сортировки",
                "Цена: по возрастанию",
                "Цена: по убыванию"
            };
            CbSort.SelectedIndex = 0;
        }

        private void LoadCategories()
        {
            try
            {
                using var db = new AppDbContext();

                var categories = db.Categories
                    .AsNoTracking()
                    .OrderBy(c => c.NameCategory)
                    .ToList();

                categories.Insert(0, new Category { IdCategories = 0, NameCategory = "Все категории" });

                CbCategory.ItemsSource = categories;
                CbCategory.DisplayMemberPath = "NameCategory";
                CbCategory.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить категории:\n\n" + ex.Message,
                    "Чудо-обувь — ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProducts()
        {
            try
            {
                using var db = new AppDbContext();

                _stock = db.StockItems
                    .GroupBy(s => s.FkIdProducts)
                    .ToDictionary(g => g.Key, g => g.Sum(x => (int?)x.Quantity) ?? 0);

                var today = DateTime.Today;
                var thisMonthStart = new DateTime(today.Year, today.Month, 1);
                var prevMonthStart = thisMonthStart.AddMonths(-1);
                var prevMonthEnd = thisMonthStart.AddDays(-1);

                _orderedInPrevMonth = db.OrderItems
                    .Join(db.Orders,
                          oi => oi.FkIdOrders,
                          o => o.IdOrders,
                          (oi, o) => new { oi.FkIdProducts, o.OrderDate })
                    .Where(x => x.OrderDate >= prevMonthStart && x.OrderDate <= prevMonthEnd)
                    .Select(x => x.FkIdProducts)
                    .Distinct()
                    .ToHashSet();

                _products = db.Products
                    .Include(p => p.FkIdManufacturersNavigation)
                    .Include(p => p.FkIdCategoriesNavigation)
                    .AsNoTracking()
                    .OrderBy(p => p.NameProduct)
                    .ToList();

                _baseLoaded = true;
            }
            catch (Exception ex)
            {
                _baseLoaded = false;
                MessageBox.Show("Не удалось загрузить товары из базы:\n\n" + ex.Message,
                    "Чудо-обувь — ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            RefreshList();
        }

        private void RefreshList()
        {
            if (!_baseLoaded)
            {
                return;
            }

            IEnumerable<Product> query = _products;

            var category = CbCategory.SelectedItem as Category;
            if (category != null && category.IdCategories != 0)
            {
                query = query.Where(p => p.FkIdCategoriesNavigation != null
                    && p.FkIdCategoriesNavigation.IdCategories == category.IdCategories);
            }

            string search = TbSearch.Text.Trim().ToLower();
            if (search.Length > 0)
            {
                query = query.Where(p =>
                    (p.NameProduct ?? string.Empty).ToLower().Contains(search)
                    || (p.Description ?? string.Empty).ToLower().Contains(search));
            }

            switch (CbSort.SelectedIndex)
            {
                case 1:
                    query = query.OrderBy(p => p.Price ?? 0m);
                    break;
                case 2:
                    query = query.OrderByDescending(p => p.Price ?? 0m);
                    break;
            }

            var cards = query
                .Select(BuildCard)
                .ToList();

            ProductsList.ItemsSource = cards;
            HeaderInfo.Text = $"Каталог товаров ({cards.Count})";
            UpdateCartButton();
        }

        private ProductCard BuildCard(Product p)
        {
            int totalQty = _stock.TryGetValue(p.IdProducts, out int q) ? q : 0;
            bool isDiscounted = !_orderedInPrevMonth.Contains(p.IdProducts);
            decimal basePrice = p.Price ?? 0m;

            return new ProductCard
            {
                IdProducts = p.IdProducts,
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
        }

        private void ProductsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductsList.SelectedItem is ProductCard card)
            {
                var product = _products.FirstOrDefault(p => p.IdProducts == card.IdProducts);
                if (product != null)
                {
                    bool isDiscounted = !_orderedInPrevMonth.Contains(product.IdProducts);
                    decimal basePrice = product.Price ?? 0m;
                    decimal unitPrice = isDiscounted
                        ? CalculateDiscountedPrice(basePrice)
                        : basePrice;

                    var view = new ProductViewWindow(
                        product, _currentUser, basePrice, unitPrice, isDiscounted);
                    view.Owner = this;
                    view.ShowDialog();
                }
            }

            ProductsList.SelectedItem = null;
            UpdateCartButton();
        }

        private void BtnCart_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null)
            {
                MessageBox.Show("Заказ может оформить только авторизованный пользователь.",
                    "Чудо-обувь — предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Cart.Items.Count == 0)
            {
                MessageBox.Show("В заказе пока нет товарных позиций. Нажмите на товар в каталоге, выберите размер и количество.",
                    "Чудо-обувь — информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var orderWindow = new OrderWindow(_currentUser) { Owner = this };
            orderWindow.ShowDialog();

            if (orderWindow.DialogResult == true)
            {
                LoadProducts();
            }
            else
            {
                UpdateCartButton();
            }
        }

        private void BtnOrders_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null)
            {
                return;
            }

            var ordersWindow = new Orders(_currentUser) { Owner = this };
            ordersWindow.ShowDialog();
            LoadProducts();
        }

        private void UpdateCartButton()
        {
            if (_currentUser == null)
            {
                return;
            }

            BtnCart.Content = $"Оформить заказ ({Cart.Items.Count})";
        }

        private void TbSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshList();
        }

        private void CbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshList();
        }

        private void CbSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshList();
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
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var authWindow = new AuthWindow();
            authWindow.Show();
            this.Close();
        }
    }
}