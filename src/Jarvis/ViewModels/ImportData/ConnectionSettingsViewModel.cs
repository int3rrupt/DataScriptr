using MVVMBase;
using System;

namespace Jarvis.ViewModels.ImportData
{
    public class ConnectionSettingsViewModel : ViewModelBase
    {
        private string _serverHostName;
        private string _databaseName;
        private string _userName;

        public string ServerHostname
        {
            get { return _serverHostName; }
            set
            {
                if (SetProperty(ref _serverHostName, value))
                {
                    ConnectCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
            set
            {
                if (SetProperty(ref _databaseName, value))
                {
                    ConnectCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string UserName
        {
            get { return _userName; }
            set
            {
                SetProperty(ref _userName, value);
            }
        }

        public string Password { get; set; }

        public DelegateCommand ConnectCommand { get; set; }

        public ConnectionSettingsViewModel()
        {
            ConnectCommand = new DelegateCommand(Connect, CanConnect);
        }

        private bool CanConnect(object p) { return !String.IsNullOrWhiteSpace(ServerHostname) && !String.IsNullOrWhiteSpace(DatabaseName); }

        private void Connect(object p)
        {
            //DataExporter dataReader = new DataExporter(ServerHostname, DatabaseName, "(localdb)\ProjectsV13", "LocalDatabase");

        }
    }
}