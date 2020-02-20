using DataScriptr.ViewModels.EditScript;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace DataScriptr.Views.EditScript
{
    /// <summary>
    /// Interaction logic for DirectorySettings.xaml
    /// </summary>
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            DatabaseNameComboBox.IsEnabled = false;
        }

        private void DatabaseSolutionPathBrowseButton(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                CheckFileExists = true,
                DefaultExt = "sln",
                Filter = "Database Solution (*.sln)|*.sln",
                Title = "Open Database Solution",

            };
            if (openFileDialog.ShowDialog() == true)
            {
                string temp = @"C:\Repo\Database\DatabaseSolutionDirectory\Database.sln";
                ((SettingsViewModel)DataContext).DatabaseSolutionPath = temp;//openFileDialog.FileName;
                //DatabaseSolutionPathTextBox.Text = openFileDialog.FileName;
                DatabaseNameComboBox.IsEnabled = true;
            }

            //DirectoryInfo sourceDirectory = new DirectoryInfo(DatabaseSolutionPathTextBox.Text);
        }
    }
}