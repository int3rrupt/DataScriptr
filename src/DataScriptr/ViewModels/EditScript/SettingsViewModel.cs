using MVVMBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataScriptr.ViewModels.EditScript
{
    public class SettingsViewModel : ViewModelBase
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
                    EnumerateDatabases();
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

        private void EnumerateDatabases()
        {
            string directory = System.IO.Path.GetDirectoryName(DatabaseSolutionPath);
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