using System.Windows;
using System.Windows.Controls;

namespace Jarvis.Views.Database
{
    /// <summary>
    /// Interaction logic for EditStaticDataScriptsPage.xaml
    /// </summary>
    public partial class EditStaticDataScriptsPage : Page
    {
        public EditStaticDataScriptsPage()
        {
            InitializeComponent();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            //this.TableDataGrid.ItemsSource = ((Jarvis.ViewModels.Database.EditStaticDataScriptsViewModel)this.DataContext).DataTable.DefaultView;
        }

        private void MenuItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var test = ((ViewModels.Database.EditStaticDataScriptsViewModel)this.DataContext).GridSelectedColumnLeft;
        }
    }
}