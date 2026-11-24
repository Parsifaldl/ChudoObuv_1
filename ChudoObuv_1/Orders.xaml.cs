using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ChudoObuv_1.Models;
using Microsoft.EntityFrameworkCore;

namespace ChudoObuv_1
{
    public partial class Orders : Window
    {
        private readonly UsersImport _user;
        private readonly bool _isAdmin;

        public Orders(UsersImport user)
        {
            InitializeComponent();
            _user = user;
            _isAdmin = user.FkIdRoles == 1;

            if (_isAdmin)
            {
                TbNewDateLabel.Visibility = Visibility.Visible;
                DpNewDate.Visibility = Visibility.Visible;
                BtnEditDate.Visibility = Visibility.Visible;
            }
            else
            {
                BtnDelete.Visibility = Visibility.Collapsed;
            }

            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                using var db = new AppDbContext();

                var orders = db.Orders
                    .Include(o => o.FkIdUsersNavigation)
                    .AsNoTracking()
                    .OrderByDescending(o => o.IdOrders)
                    .ToList();

                var rows = orders.Select(o => new OrderRow
                {
                    IdOrders = o.IdOrders,
                    DateText = o.OrderDate?.ToString("d") ?? "—",
                    ClientFio = o.FkIdUsersNavigation == null
                        ? "—"
                        : $"{o.FkIdUsersNavigation.FirstName} {o.FkIdUsersNavigation.Name} {o.FkIdUsersNavigation.LastName}".Trim()
                }).ToList();

                OrdersGrid.ItemsSource = rows;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить заказы:\n\n" + ex.Message,
                    "Чудо-обувь — ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private OrderRow? SelectedOrder()
        {
            return OrdersGrid.SelectedItem as OrderRow;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new OrderWindow(_user, staffMode: true) { Owner = this };
            addWindow.ShowDialog();

            if (addWindow.DialogResult == true)
            {
                LoadOrders();
            }
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            var selected = SelectedOrder();
            if (selected == null)
            {
                MessageBox.Show("Выберите заказ в списке.",
                    "Чудо-обувь — предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var viewWindow = new OrderWindow(_user, staffMode: true, editOrderId: selected.IdOrders) { Owner = this };
            viewWindow.ShowDialog();

            if (viewWindow.DialogResult == true)
            {
                LoadOrders();
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selected = SelectedOrder();
            if (selected == null)
            {
                MessageBox.Show("Выберите заказ в списке.",
                    "Чудо-обувь — предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var answer = MessageBox.Show(
                "Удалить заказ №" + selected.IdOrders + "? Он будет удалён безвозвратно, "
                + "товарные позиции вернутся на склад.",
                "Чудо-обувь — подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                using var db = new AppDbContext();

                var items = db.OrderItems
                    .Where(i => i.FkIdOrders == selected.IdOrders)
                    .ToList();

                if (items.Count > 0)
                {
                    var productIds = items
                        .Select(i => i.FkIdProducts)
                        .ToList();
                    var stockRows = db.StockItems
                        .Where(s => s.FkIdProducts != null && productIds.Contains(s.FkIdProducts))
                        .ToList();

                    foreach (var item in items)
                    {
                        var row = stockRows.FirstOrDefault(s =>
                            s.FkIdProducts == item.FkIdProducts
                            && s.FkIdSizes == item.FkIdSizes);
                        if (row != null)
                        {
                            row.Quantity += item.Quantity ?? 0;
                        }
                    }
                }

                db.OrderItems.RemoveRange(items);

                var order = db.Orders
                    .FirstOrDefault(o => o.IdOrders == selected.IdOrders);
                if (order != null)
                {
                    db.Orders.Remove(order);
                }

                db.SaveChanges();

                MessageBox.Show("Заказ №" + selected.IdOrders + " удалён, товар возвращён на склад.",
                    "Чудо-обувь — информация", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось удалить заказ:\n\n" + ex.Message,
                    "Чудо-обувь — ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEditDate_Click(object sender, RoutedEventArgs e)
        {
            var selected = SelectedOrder();
            if (selected == null)
            {
                MessageBox.Show("Выберите заказ в списке.",
                    "Чудо-обувь — предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DpNewDate.SelectedDate == null)
            {
                MessageBox.Show("Выберите новую дату заказа.",
                    "Чудо-обувь — предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var db = new AppDbContext();

                var order = db.Orders
                    .FirstOrDefault(o => o.IdOrders == selected.IdOrders);
                if (order == null)
                {
                    MessageBox.Show("Заказ не найден в базе.",
                        "Чудо-обувь — ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                order.OrderDate = DpNewDate.SelectedDate;
                db.SaveChanges();

                MessageBox.Show("Дата заказа №" + selected.IdOrders + " изменена на "
                    + order.OrderDate?.ToString("d") + ".",
                    "Чудо-обувь — информация", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось изменить дату заказа:\n\n" + ex.Message,
                    "Чудо-обувь — ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class OrderRow
    {
        public int IdOrders { get; set; }
        public string DateText { get; set; } = "";
        public string ClientFio { get; set; } = "";
    }
}