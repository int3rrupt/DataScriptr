using MVVMBase;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace DataScriptr.ViewModels.Database
{
    public class RepoSettingsViewModel : ViewModelBase
    {
        private string _databaseSolutionPath;
        private Dictionary<string, string> _databaseNameAndPathList;
        private List<string> _databaseNames;

        public string DatabaseSolutionPath
        {
            get { return _databaseSolutionPath; }
            set
            {
                if (SetProperty(ref _databaseSolutionPath, value))
                {
                    Save.RaiseCanExecuteChanged();
                }
            }
        }

        public List<string> DatabaseNames
        {
            get { return _databaseNames; }
            set
            {
                if (SetProperty(ref _databaseNames, value))
                {
                    //
                }
            }
        }

        public RelayCommand Save { get; private set; }

        public RepoSettingsViewModel()
        {
            this.Save = new RelayCommand(SaveCmd, CanSave);
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(DatabaseSolutionPath);
        }

        private void SaveCmd(object parameter)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings["DatabaseSolutionPath"] == null)
                {
                    settings.Add("DatabaseSolutionPath", DatabaseSolutionPath);
                }
                else
                {
                    settings["DatabaseSolutionPath"].Value = DatabaseSolutionPath;
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                throw;
            }
        }

        private void EnumerateDatabases()
        {
            string directory = Path.GetDirectoryName(DatabaseSolutionPath);
            DirectoryInfo sourceDirectory = new DirectoryInfo(directory);
            FileInfo[] databaseProjects = sourceDirectory.GetFiles("*.sqlproj", SearchOption.AllDirectories);
            if (databaseProjects.Length > 0)
            {
                Dictionary<string, string> databaseNameAndPathList = new Dictionary<string, string>(databaseProjects.Length);
                List<string> databaseNames = new List<string>(databaseProjects.Length);
                foreach (FileInfo databaseProject in databaseProjects)
                {
                    string databaseName = Path.GetFileNameWithoutExtension(databaseProject.Name);
                    databaseNameAndPathList.Add(databaseName, databaseProject.DirectoryName);
                    databaseNames.Add(databaseName);
                }
                _databaseNameAndPathList = databaseNameAndPathList;
                DatabaseNames = databaseNames;
            }
        }
    }
}