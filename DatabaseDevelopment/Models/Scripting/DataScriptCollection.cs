using DatabaseDevelopment.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace DatabaseDevelopment.Models.Scripting
{
    public class DataScriptCollection
    {
        private string _databaseProjectDirectoryPath;
        private Dictionary<string, Dictionary<DatabaseEnvironment, DataScript>> _staticDataScripts = new Dictionary<string, Dictionary<DatabaseEnvironment, DataScript>>();
        private Dictionary<string, Dictionary<DatabaseEnvironment, DataScript>> _testDataScripts = new Dictionary<string, Dictionary<DatabaseEnvironment, DataScript>>();
        private List<string> _staticDataScriptNames = new List<string>();

        public string DatabaseProjectDirectoryPath
        {
            get { return this._databaseProjectDirectoryPath; }
            set
            {
                this._databaseProjectDirectoryPath = value;
                this._staticDataScripts = this.EnumerateDataScripts(this._databaseProjectDirectoryPath, DataScriptType.Static);
                this._testDataScripts = this.EnumerateDataScripts(this._databaseProjectDirectoryPath, DataScriptType.Test);
            }
        }

        public DataScriptCollection()
        {
        }

        public DataScriptCollection(string databaseProjectDirectoryPath)
        {
            this.DatabaseProjectDirectoryPath = databaseProjectDirectoryPath;
        }

        public ReadOnlyCollection<string> StaticDataScriptNames
        {
            get { return this._staticDataScripts.Keys.ToList().AsReadOnly(); }
        }

        public ReadOnlyCollection<string> TestDataScriptNames
        {
            get { return this._testDataScripts.Keys.ToList().AsReadOnly(); }
        }

        public Dictionary<DatabaseEnvironment, DataScript> GetDataScriptByName(string dataScriptName)
        {
            Dictionary<DatabaseEnvironment, DataScript> dataScripts = new Dictionary<DatabaseEnvironment, DataScript>();
            if (!this._staticDataScripts.TryGetValue(dataScriptName, out dataScripts)
                && !this._testDataScripts.TryGetValue(dataScriptName, out dataScripts))
            {
                return Enumerable.Empty<DataScript>().ToDictionary(x => x.Environment);
            }
            return dataScripts;
        }

        private Dictionary<string, Dictionary<DatabaseEnvironment, DataScript>> EnumerateDataScripts(string databaseProjectDirectoryPath, DataScriptType dataScriptType)
        {
            DirectoryInfo dataScriptsDirectoryInfo = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(databaseProjectDirectoryPath), "_Data", dataScriptType.ToString()));
            Dictionary<string, Dictionary<DatabaseEnvironment, DataScript>> dataScripts = new Dictionary<string, Dictionary<DatabaseEnvironment, DataScript>>();
            if (dataScriptsDirectoryInfo.Exists)
            {
                FileInfo[] dataScriptFiles = dataScriptsDirectoryInfo.GetFiles("*.sql", SearchOption.AllDirectories);
                List<string> failedScripts = new List<string>();
                foreach (FileInfo staticDataFileInfo in dataScriptFiles)
                {
                    try
                    {
                        DataScript dataScript = new DataScript
                        {
                            Name = Path.GetFileNameWithoutExtension(staticDataFileInfo.Name),
                            Type = dataScriptType,
                            Environment = this.ParseEnvironment(staticDataFileInfo.Directory.Name),
                            Path = staticDataFileInfo.FullName
                        };
                        // Check whether scripts with same name already exist
                        if (dataScripts.ContainsKey(dataScript.Name))
                        {
                            dataScripts[dataScript.Name].Add(dataScript.Environment, dataScript);
                        }
                        else
                        {
                            dataScripts.Add(dataScript.Name, new Dictionary<DatabaseEnvironment, DataScript> { { dataScript.Environment, dataScript } });
                        }
                    }
                    catch (Exception ex)
                    {
                        failedScripts.Add(staticDataFileInfo.FullName);
                    }
                }
            }
            return dataScripts;
        }

        private DatabaseEnvironment ParseEnvironment(string environment)
        {
            if (!Enum.TryParse(environment, true, out DatabaseEnvironment environmentEnum))
            {
                throw new Exception("Unable to parse script environment");
            }
            return environmentEnum;
        }
    }
}