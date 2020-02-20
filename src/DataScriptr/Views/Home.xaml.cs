using DataScriptr.Views.ImportData;
using System.Windows;
using System.Windows.Controls;

namespace DataScriptr.Views
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : Page
    {
        public Home()
        {
            InitializeComponent();
        }

        private void BtnImportDataClick(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new ImportPage());
        }
    }
}