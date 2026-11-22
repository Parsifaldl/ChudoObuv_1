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
    public partial class ProductViewWindow : Window
    {
        private readonly Product _product;
        private readonly UsersImport? _user;
        private readonly decimal _basePrice;
        private readonly decimal _unitPrice;
        private readonly bool _isDiscounted;

        private List<StockItem> _sizeRows = new List<StockItem>();

        public ProductViewWindow(Product product, UsersImport? user,
            decimal basePrice, decimal unitPrice, bool isDiscounted)
        {
            InitializeComponent();
            _product = product;
            _user = user;
            _basePrice = basePrice;
            _unitPrice = unitPrice;
            _isDiscounted = isDiscounted;

            FillProductInfo();
            LoadSizes();

            if (_user == null)
            {
                OrderPanel.Visibility = Visibility.Collapsed;
                GuestNote.Visibility = Visibility.Visible;
            }
        }

        private void FillProductInfo()
        {
            ProductImage.Source = LoadProductImage(_product.Image);

            TitleBlock.Text = (_product.FkIdManufacturersNavigation?.NameManufacturer ?? "—")
                + " | " + (_product.NameProduct ?? "—");
            ManufacturerBlock.Text = "Производство: "
                + (_product.FkIdManufacturersNavigation?.NameManufacturer ?? "—");
            CategoryBlock.Text = "Категория: "
                + (_product.FkIdCategoriesNavigation?.NameCategory ?? "—");

            if (_isDiscounted)
            {
                PriceBlock.Text = $"Цена со скидкой: {_unitPrice:N2} руб. "
                    + $"(без скидки: {_basePrice:N2} руб.)";
            }
            else
            {
                PriceBlock.Text = $"Цена: {_basePrice:N2} руб.";
            }

            CompositionBlock.Text = "Состав: " +
                (string.IsNullOrWhiteSpace(_product.Composition) ? "—" : _product.Composition);
            DescriptionBlock.Text = "Описание: " +
                (string.IsNullOrWhiteSpace(_product.Description) ? "—" : _product.Description);
        }

        private void LoadSizes()
        {
            try
            {
                using var db = new AppDbContext();

                _sizeRows = db.StockItems
                    .Include(s => s.FkIdSizesNavigation)
                    .AsNoTracking()
                    .Where(s => s.FkIdProducts == _product.IdProducts)
                    .ToList()
                    .OrderBy(s => s.FkIdSizesNavigation?.NameSize ?? 0)
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить размеры из базы:\n\n" + ex.Message,
                    "Чудо-обувь — ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var allSizes = _sizeRows
                .Select(s => FormatSize(s.FkIdSizesNavigation?.NameSize))
                .Distinct()
                .ToList();

            SizeRangeBlock.Text = allSizes.Count > 0
                ? "Размерный ряд: " + string.Join(", ", allSizes)
                : "Размерный ряд: для этого товара размеры не заданы.";

            var available = _sizeRows
                .Where(s => s.Quantity > 0)
                .Select(s => new SizeOption
                {
                    Row = s,
                    SizeName = FormatSize(s.FkIdSizesNavigation?.NameSize),
                    Display = $"Размер {FormatSize(s.FkIdSizesNavigation?.NameSize)} "
                        + $"— доступно {s.Quantity} пар."
                })
                .ToList();

            CbSizes.ItemsSource = available;
            CbSizes.DisplayMemberPath = "Display";
            if (available.Count > 0)
            {
                CbSizes.SelectedIndex = 0;
            }
            else
            {
                CbSizes.IsEnabled = false;
                TbQuantity.IsEnabled = false;
                BtnAddToOrder.IsEnabled = false;
                MessageBox.Show("В наличии нет ни одного размера этого товара — оформить заказ нельзя.",
                    "Чудо-обувь — информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private static string FormatSize(decimal? size)
        {
            return size.HasValue ? size.Value.ToString("0.#") : "?";
        }

        private void BtnAddToOrder_Click(object sender, RoutedEventArgs e)
        {
            if (_user == null)
            {
                return;
            }

            var option = CbSizes.SelectedItem as SizeOption;
            if (option == null)
            {
                MessageBox.Show("Выберите размер из списка доступных.",
                    "Чудо-обувь — предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string qtyText = TbQuantity.Text.Trim();
            if (!int.TryParse(qtyText, out int quantity))
            {
                MessageBox.Show("Количество должно быть целым числом, например 1 или 2.",
                    "Чудо-обувь — предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (quantity <= 0)
            {
                MessageBox.Show("Количество должно быть больше нуля.",
                    "Чудо-обувь — предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (quantity > option.Row.Quantity)
            {
                MessageBox.Show(
                    $"Столько пар размера {option.SizeName} нет в наличии — доступно {option.Row.Quantity}. Уменьшите количество.",
                    "Чудо-обувь — предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Cart.Items.Add(new CartItem
            {
                ProductId = _product.IdProducts,
                ProductTitle = TitleBlock.Text,
                SizeId = option.Row.FkIdSizes ?? 0,
                SizeName = option.SizeName,
                Quantity = quantity,
                UnitPrice = _unitPrice
            });

            MessageBox.Show(
                "Товар добавлен в заказ. Продолжите выбирать другие позиции или подтвердите заказ в каталоге.",
                "Чудо-обувь — информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
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

    public class SizeOption
    {
        public StockItem Row { get; set; } = null!;
        public string SizeName { get; set; } = "";
        public string Display { get; set; } = "";
    }
}