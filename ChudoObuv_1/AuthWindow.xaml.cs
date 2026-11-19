using System.Windows;

namespace ChudoObuv_1
{
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            InitializeComponent();
        }

        private void GuestButton_Click(object sender, RoutedEventArgs e)
        {
            var catalog = new CatalogWindow();
            catalog.Show();
            Close();
        }
    }
}