using Jarvis.ViewModels.Database;
using Jarvis.Views.Database;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;

namespace Jarvis
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<Type, Page> _pageCollection;
        private StackPanel _currentMenuStackPanel;
        private EditStaticDataScriptsViewModel _editStaticDataScriptsViewModel;

        private RepoSettingsPage RepoSettingsPage
        {
            get
            {
                if (!_pageCollection.ContainsKey(typeof(RepoSettingsPage)))
                {
                    _pageCollection.Add(typeof(RepoSettingsPage), new RepoSettingsPage());
                }
                return (RepoSettingsPage)_pageCollection[typeof(RepoSettingsPage)];
            }
        }

        private EditStaticDataScriptsPage EditStaticDataScriptsPage
        {
            get
            {
                if (!_pageCollection.ContainsKey(typeof(EditStaticDataScriptsPage)))
                {
                    _pageCollection.Add(typeof(EditStaticDataScriptsPage), new EditStaticDataScriptsPage());
                }
                return (EditStaticDataScriptsPage)_pageCollection[typeof(EditStaticDataScriptsPage)];
            }
        }

        private StackPanel CurrentMenuStackPanel
        {
            get { return _currentMenuStackPanel; }
            set
            {
                if (_currentMenuStackPanel != null)
                {
                    _currentMenuStackPanel.Visibility = Visibility.Collapsed;
                }
                _currentMenuStackPanel = value;
                if (_currentMenuStackPanel != null)
                {
                    _currentMenuStackPanel.Visibility = Visibility.Visible;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DatabaseMenuStackPanel.Visibility = Visibility.Collapsed;
            ImportDataMenuStackPanel.Visibility = Visibility.Collapsed;
            _pageCollection = new Dictionary<Type, Page>();
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }

        private void DatabaseButtonClick(object sender, RoutedEventArgs e)
        {
            //Page connectionSettingsPage = _pageCollection.ContainsKey(typeof(ConnectionSettingsPage)) ? _pageCollection[typeof(ConnectionSettingsPage)] : new ConnectionSettingsPage();
            //PageFrame.Navigate(connectionSettingsPage);
            CurrentMenuStackPanel = DatabaseMenuStackPanel;
        }

        private void EditStaticDataScriptsButtonClick(object sender, RoutedEventArgs e)
        {
            //_editStaticDataScriptsViewModel.DatabaseSolutionDirectoryPath = @"C:\Source\Repo\Database\DatabaseSolutionDirectory";
            EditStaticDataScriptsViewModel dataContext = EditStaticDataScriptsPage.DataContext as EditStaticDataScriptsViewModel;
            dataContext.DatabaseSolutionDirectoryPath = ConfigurationManager.AppSettings["DatabaseSolutionPath"];
            dataContext.StaticDataEnabled = true;
            PageFrame.Navigate(EditStaticDataScriptsPage);
        }

        private void DirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            //
        }

        private void ImportDataButtonClick(object sender, RoutedEventArgs e)
        {
            //this.NavigationService.Navigate(new ImportPage());/
            //Page connectionSettingsPage = _pageCollection.ContainsKey(typeof(ConnectionSettingsPage)) ? _pageCollection[typeof(ConnectionSettingsPage)] : new ConnectionSettingsPage();
            CurrentMenuStackPanel = ImportDataMenuStackPanel;
            //PageFrame.Navigate(connectionSettingsPage);
        }

        private void ConnectionButtonClick(object sender, RoutedEventArgs e)
        {
            //Page connectionSettingsPage = _pageCollection.ContainsKey(typeof(ConnectionSettingsPage)) ? _pageCollection[typeof(ConnectionSettingsPage)] : new ConnectionSettingsPage();
            //PageFrame.Navigate(connectionSettingsPage);
        }

        private void RepoSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            RepoSettingsViewModel dataContext = RepoSettingsPage.DataContext as RepoSettingsViewModel;
            dataContext.DatabaseSolutionPath = ConfigurationManager.AppSettings["DatabaseSolutionPath"];
            //((RepoSettingsViewModel)page.DataContext).DatabaseSolutionPath
            //CurrentMenuStackPanel = EditScriptMenuStackPanel;
            PageFrame.Navigate(RepoSettingsPage);
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.StackTrace, e.Exception.Message);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings["DatabaseSolutionPath"] == null)
                {
                    settings.Add("DatabaseSolutionPath", ((RepoSettingsViewModel)RepoSettingsPage.DataContext).DatabaseSolutionPath);
                }
                else
                {
                    settings["DatabaseSolutionPath"].Value = ((RepoSettingsViewModel)RepoSettingsPage.DataContext).DatabaseSolutionPath;
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error writing app settings");
            }
        }
    }
}