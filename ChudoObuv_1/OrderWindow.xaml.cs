using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ChudoObuv_1.Models;
using Microsoft.EntityFrameworkCore;

namespace ChudoObuv_1
{
    public partial class OrderWindow : Window
    {
        private readonly UsersImport? _user;
        private const decimal DiscountShare = 0.25m;
        private readonly bool _staffMode;
        private readonly int? _editOrderId;
        private readonly bool _isAdmin;
        private bool _changed;

        private List<Product> _products = new List<Product>();
        private HashSet<int?> _orderedInPrevMonth = new HashSet<int?>();
        private List<OrderItemRow> _staffRows = new List<OrderItemRow>();

        public OrderWindow(UsersImport? user)
        {
            InitializeComponent();
            _user = user;
            OrderGrid.ItemsSource = Cart.Items;
            UpdateTotal();
        }

        public OrderWindow(UsersImport? user, bool staffMode = false, int? editOrderId = null)
        {
            InitializeComponent();
            _user = user;
            _staffMode = staffMode;
            _editOrderId = editOrderId;
            _isAdmin = user != null && user.FkIdRoles == 1;

            Width = 940;
            Height = 700;
            BtnConfirm.Visibility = Visibility.Collapsed;

            if (_editOrderId.HasValue)
            {
                Title = "Состав заказа №" + _editOrderId.Value;
                TbOrderInfo.Visibility = Visibility.Visible;
                BtnCancel.Visibility = Visibility.Collapsed;
                BtnBack.Visibility = Visibility.Visible;
                OrderGrid.Columns[0].Visibility = Visibility.Visible;

                if (_isAdmin)
                {
                    BtnDeleteItem.Visibility = Visibility.Visible;
                }

                LoadOrderForEdit();
            }
            else
            {
                Title = "Новый заказ";
                BtnSave.Visibility = Visibility.Visible;
                SpClientPick.Visibility = Visibility.Visible;
                SpPick.Visibility = Visibility.Visible;
                LbProducts.Visibility = Visibility.Visible;

                LoadClients();
                LoadStaffProducts();
                RefreshStaffGrid();
            }
        }

        private void UpdateTotal()
        {
            decimal total = Cart.Items.Sum(i => i.Sum);
            TotalText.Text = "Итого: " + total.ToString("N2") + " руб.";
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (_user == null)
            {
                MessageBox.Show("Заказ может оформить только авторизованный пользователь.",
                    "Чудо-обувь — предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Cart.Items.Count == 0)
            {
                MessageBox.Show("В заказе нет ни одной товарной позиции.",
                    "Чудо-обувь — предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal total = Cart.Items.Sum(i => i.Sum);
            var answer = MessageBox.Show(
                "Подтвердить заказ на сумму " + total.ToString("N2") + " руб.?\n"
                + "Дата заказа будет сохранена, и она не сможет быть изменена.",
                "Чудо-обувь — подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                using var db = new AppDbContext();

                int nextOrderId = db.Orders.Any() ? db.Orders.Max(o => o.IdOrders) + 1 : 1;
                var order = new Order
                {
                    IdOrders = nextOrderId,
                    OrderDate = DateTime.Today,
                    FkIdUsers = _user.IdUsers
                };
                db.Orders.Add(order);
                db.SaveChanges();

                int nextItemId = db.OrderItems.Any() ? db.OrderItems.Max(i => i.IdOrderItems) + 1 : 1;
                foreach (var item in Cart.Items)
                {
                    db.OrderItems.Add(new OrderItem
                    {
                        IdOrderItems = nextItemId++,
                        FkIdOrders = order.IdOrders,
                        FkIdProducts = item.ProductId,
                        FkIdSizes = item.SizeId,
                        Quantity = item.Quantity,
                        UnitPrice = (int)Math.Round(item.UnitPrice, MidpointRounding.AwayFromZero)
                    });
                }
                db.SaveChanges();

                var productIds = Cart.Items
                    .Select(i => (int?)i.ProductId)
                    .ToList();
                var stockRows = db.StockItems
                    .Where(s => s.FkIdProducts != null && productIds.Contains(s.FkIdProducts))
                    .ToList();

                foreach (var group in Cart.Items
                    .GroupBy(i => new { i.ProductId, i.SizeId }))
                {
                    int orderedQty = group.Sum(i => i.Quantity);
                    var row = stockRows.FirstOrDefault(s =>
                        s.FkIdProducts == group.Key.ProductId
                        && s.FkIdSizes == group.Key.SizeId);
                    if (row != null)
                    {
                        row.Quantity -= orderedQty;
                    }
                }
                db.SaveChanges();

                MessageBox.Show(
                    "Заказ №" + order.IdOrders + " от " + order.OrderDate?.ToString("d")
                    + " успешно оформлен. Сумма: " + total.ToString("N2") + " руб.",
                    "Чудо-обувь — информация", MessageBoxButton.OK, MessageBoxImage.Information);

                Cart.Items.Clear();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось сохранить заказ:\n\n" + ex.Message,
                    "Чудо-обувь — ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            var answer = MessageBox.Show(
                "Отменить заказ? Все выбранные товарные позиции будут удалены.",
                "Чудо-обувь — подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            Cart.Items.Clear();
            DialogResult = false;
            Close();
        }

        private void LoadClients()
        {
            try
            {
                using var db = new AppDbContext();

                var clients = db.UsersImports
                    .AsNoTracking()
                    .ToList()
                    .OrderBy(FioOf)
                    .Select(u => new ClientOption
                    {
                        IdUsers = u.IdUsers,
                        Fio = FioOf(u),
                        Display = FioOf(u) + " (логин: " + u.Login + ")"
                    })
                    .ToList();

                CbClient.ItemsSource = clients;
                CbClient.DisplayMemberPath = "Display";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить список клиентов:\n\n" + ex.Message,
                    "Чудо-обувь - ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadStaffProducts()
        {
            try
            {
                using var db = new AppDbContext();

                _products = db.Products
                    .Include(p => p.FkIdManufacturersNavigation)
                    .AsNoTracking()
                    .OrderBy(p => p.NameProduct)
                    .ToList();

                var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                _orderedInPrevMonth = db.OrderItems
                    .Where(i => db.Orders.Any(o => o.IdOrders == i.FkIdOrders
                        && o.OrderDate >= monthStart.AddMonths(-1)
                        && o.OrderDate < monthStart))
                    .Select(i => i.FkIdProducts)
                    .Distinct()
                    .ToHashSet();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить товары:\n\n" + ex.Message,
                    "Чудо-обувь - ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            ApplyProductSearch();
        }

        private void ApplyProductSearch()
        {
            string search = TbSearch.Text.Trim().ToLower();

            var query = _products.AsEnumerable();
            if (search.Length > 0)
            {
                query = query.Where(p => (p.NameProduct ?? "").ToLower().Contains(search));
            }

            LbProducts.ItemsSource = query.ToList();
        }

        private void TbSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_staffMode && !_editOrderId.HasValue)
            {
                ApplyProductSearch();
            }
        }

        private void BtnPick_Click(object sender, RoutedEventArgs e)
        {
            var product = LbProducts.SelectedItem as Product;
            if (product == null)
            {
                MessageBox.Show("Выберите товар из списка (при необходимости воспользуйтесь поиском).",
                    "Чудо-обувь - предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isDiscounted = !_orderedInPrevMonth.Contains(product.IdProducts);
            decimal basePrice = product.Price ?? 0m;
            decimal unitPrice = isDiscounted
                ? Math.Round(basePrice * (1 - DiscountShare), 2)
                : basePrice;

            var view = new ProductViewWindow(product, _user, basePrice, unitPrice, isDiscounted, pickMode: true)
            {
                Owner = this
            };

            if (view.ShowDialog() != true || view.ChosenQuantity <= 0)
            {
                return;
            }

            _staffRows.Add(new OrderItemRow
            {
                ProductId = product.IdProducts,
                SizeId = view.ChosenSizeId,
                ProductTitle = (product.FkIdManufacturersNavigation?.NameManufacturer ?? "нет данных")
                    + " | " + (product.NameProduct ?? ""),
                SizeName = view.ChosenSizeName,
                Quantity = view.ChosenQuantity,
                UnitPrice = unitPrice
            });

            RefreshStaffGrid();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var client = CbClient.SelectedItem as ClientOption;
            if (client == null)
            {
                MessageBox.Show("Выберите клиента заказа.",
                    "Чудо-обувь - предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_staffRows.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы одну товарную позицию.",
                    "Чудо-обувь - предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal total = _staffRows.Sum(r => r.Sum);
            var answer = MessageBox.Show(
                "Оформить заказ для клиента " + client.Fio
                + " на сумму " + total.ToString("N2") + " руб.?",
                "Чудо-обувь - подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            if (TrySaveOrder(client.IdUsers))
            {
                DialogResult = true;
                Close();
            }
        }

        private bool TrySaveOrder(int clientId)
        {
            try
            {
                using var db = new AppDbContext();

                int nextOrderId = db.Orders.Any() ? db.Orders.Max(o => o.IdOrders) + 1 : 1;
                var order = new Order
                {
                    IdOrders = nextOrderId,
                    OrderDate = DateTime.Today,
                    FkIdUsers = clientId
                };
                db.Orders.Add(order);
                db.SaveChanges();

                int nextItemId = db.OrderItems.Any() ? db.OrderItems.Max(i => i.IdOrderItems) + 1 : 1;
                foreach (var row in _staffRows)
                {
                    db.OrderItems.Add(new OrderItem
                    {
                        IdOrderItems = nextItemId++,
                        FkIdOrders = order.IdOrders,
                        FkIdProducts = row.ProductId,
                        FkIdSizes = row.SizeId,
                        Quantity = row.Quantity,
                        UnitPrice = (int)Math.Round(row.UnitPrice, MidpointRounding.AwayFromZero)
                    });
                }
                db.SaveChanges();

                var productIds = _staffRows
                    .Select(r => (int?)r.ProductId)
                    .ToList();
                var stockRows = db.StockItems
                    .Where(s => s.FkIdProducts != null && productIds.Contains(s.FkIdProducts))
                    .ToList();

                foreach (var group in _staffRows
                    .GroupBy(r => new { r.ProductId, r.SizeId }))
                {
                    int orderedQty = group.Sum(r => r.Quantity);
                    var row = stockRows.FirstOrDefault(s =>
                        s.FkIdProducts == group.Key.ProductId
                        && s.FkIdSizes == group.Key.SizeId);
                    if (row != null)
                    {
                        row.Quantity -= orderedQty;
                    }
                }
                db.SaveChanges();

                decimal total = _staffRows.Sum(r => r.Sum);
                MessageBox.Show(
                    "Заказ №" + order.IdOrders + " от " + order.OrderDate?.ToString("d")
                    + " успешно оформлен. Сумма: " + total.ToString("N2") + " руб.",
                    "Чудо-обувь - информация", MessageBoxButton.OK, MessageBoxImage.Information);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось сохранить заказ:\n\n" + ex.Message,
                    "Чудо-обувь - ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void LoadOrderForEdit()
        {
            try
            {
                using var db = new AppDbContext();

                var order = db.Orders
                    .Include(o => o.FkIdUsersNavigation)
                    .AsNoTracking()
                    .FirstOrDefault(o => o.IdOrders == _editOrderId);

                if (order == null)
                {
                    MessageBox.Show("Заказ не найден в базе.",
                        "Чудо-обувь - ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                TbOrderInfo.Text = "Заказ №" + order.IdOrders
                    + " от " + (order.OrderDate?.ToString("d") ?? "нет даты")
                    + ", клиент: " + (order.FkIdUsersNavigation == null
                        ? "нет данных" : FioOf(order.FkIdUsersNavigation));

                var products = db.Products
                    .Include(p => p.FkIdManufacturersNavigation)
                    .AsNoTracking()
                    .ToList();

                var stockRows = db.StockItems
                    .Include(s => s.FkIdSizesNavigation)
                    .AsNoTracking()
                    .ToList();

                var items = db.OrderItems
                    .AsNoTracking()
                    .Where(i => i.FkIdOrders == _editOrderId)
                    .ToList();

                _staffRows = items.Select(i =>
                {
                    var product = products.FirstOrDefault(p => p.IdProducts == i.FkIdProducts);
                    var stock = stockRows.FirstOrDefault(s =>
                        s.FkIdProducts == i.FkIdProducts && s.FkIdSizes == i.FkIdSizes);

                    return new OrderItemRow
                    {
                        IdOrderItems = i.IdOrderItems,
                        ProductId = i.FkIdProducts ?? 0,
                        SizeId = i.FkIdSizes ?? 0,
                        ProductTitle = (product?.FkIdManufacturersNavigation?.NameManufacturer ?? "нет данных")
                            + " | " + (product?.NameProduct ?? ""),
                        SizeName = FormatSize(stock?.FkIdSizesNavigation?.NameSize),
                        Quantity = i.Quantity ?? 0,
                        UnitPrice = i.UnitPrice ?? 0m
                    };
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить состав заказа:\n\n" + ex.Message,
                    "Чудо-обувь - ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                _staffRows = new List<OrderItemRow>();
            }

            RefreshStaffGrid();
        }

        private void BtnDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            var selected = OrderGrid.SelectedItem as OrderItemRow;
            if (selected == null)
            {
                MessageBox.Show("Выберите товарную позицию в таблице.",
                    "Чудо-обувь - предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var answer = MessageBox.Show(
                "Удалить позицию №" + selected.IdOrderItems + " ("
                + selected.ProductTitle + ", размер " + selected.SizeName + ")? "
                + "Товар вернется на склад.",
                "Чудо-обувь - подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                using var db = new AppDbContext();

                var item = db.OrderItems
                    .FirstOrDefault(i => i.IdOrderItems == selected.IdOrderItems);

                if (item == null)
                {
                    MessageBox.Show("Позиция не найдена в базе.",
                        "Чудо-обувь - ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var stockRow = db.StockItems
                    .FirstOrDefault(s => s.FkIdProducts == item.FkIdProducts
                        && s.FkIdSizes == item.FkIdSizes);
                if (stockRow != null)
                {
                    stockRow.Quantity += item.Quantity ?? 0;
                }

                db.OrderItems.Remove(item);
                db.SaveChanges();

                _changed = true;

                MessageBox.Show("Позиция удалена, товар возвращен на склад.",
                    "Чудо-обувь - информация", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadOrderForEdit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось удалить позицию:\n\n" + ex.Message,
                    "Чудо-обувь - ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = _changed;
            Close();
        }

        private void RefreshStaffGrid()
        {
            OrderGrid.ItemsSource = null;
            OrderGrid.ItemsSource = _staffRows;

            decimal total = _staffRows.Sum(r => r.Sum);
            TotalText.Text = "Итого: " + total.ToString("N2") + " руб.";
        }

        private static string FioOf(UsersImport u)
        {
            return $"{u.FirstName} {u.Name} {u.LastName}".Trim();
        }

        private static string FormatSize(decimal? size)
        {
            return size.HasValue ? size.Value.ToString("0.#") : "?";
        }
    }

    public class OrderItemRow
    {
        public int IdOrderItems { get; set; }
        public int ProductId { get; set; }
        public int SizeId { get; set; }
        public string ProductTitle { get; set; } = "";
        public string SizeName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public decimal Sum => Quantity * UnitPrice;
    }

    public class ClientOption
    {
        public int IdUsers { get; set; }
        public string Fio { get; set; } = "";
        public string Display { get; set; } = "";
    }
}