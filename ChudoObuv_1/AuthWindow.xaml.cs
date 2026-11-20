using System;
using System.Linq;
using System.Windows;
using ChudoObuv_1.Models;
using Microsoft.EntityFrameworkCore;

namespace ChudoObuv_1
{
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = TbLogin.Text.Trim();

            if (login.Length == 0)
            {
                MessageBox.Show("Введите логин.", "Авторизация",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var db = new AppDbContext();

                var user = db.UsersImports
                    .AsNoTracking()
                    .FirstOrDefault(u => u.Login == login);

                if (user == null)
                {
                    MessageBox.Show("Пользователь с таким логином не найден.",
                        "Авторизация", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                OpenCatalog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка обращения к базе: " + ex.Message,
                    "Авторизация", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnGuest_Click(object sender, RoutedEventArgs e)
        {
            OpenCatalog();
        }

        private void OpenCatalog()
        {
            var catalog = new CatalogWindow();
            catalog.Show();
            Close();
        }
    }
}