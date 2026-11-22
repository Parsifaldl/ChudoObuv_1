using System;
using System.Linq;
using System.Windows;
using ChudoObuv_1.Models;

namespace ChudoObuv_1
{
    public partial class OrderWindow : Window
    {
        private readonly UsersImport? _user;

        public OrderWindow(UsersImport? user)
        {
            InitializeComponent();
            _user = user;
            OrderGrid.ItemsSource = Cart.Items;
            UpdateTotal();
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
                        UnitPrice = (int?)Math.Round(item.UnitPrice)
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
    }
}