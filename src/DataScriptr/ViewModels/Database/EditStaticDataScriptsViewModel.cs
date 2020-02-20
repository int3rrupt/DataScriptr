using DatabaseDevelopment;
using DatabaseDevelopment.Enums;
using DatabaseDevelopment.Models.Scripting;
using DataScriptr.Models.Exceptions;
using MVVMBase;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace DataScriptr.ViewModels.Database
{
    public class EditStaticDataScriptsViewModel : ViewModelBase
    {
        #region Backing Fields

        private string _databaseSolutionDirectoryPath;
        private List<string> _databaseProjectNames = new List<string>();
        private string _currentDatabaseProject;
        private List<string> _dataScriptNames = new List<string>();
        private string _currentDataScript;
        private bool _staticDataEnabled;
        private bool _testDataEnabled;
        private bool _deleteWhenNotMatchedLeft;
        private bool _deleteWhenNotMatchedRight;
        private bool _printChangesLeft;
        private bool _printChangesRight;
        private bool _dataGridRightVisible;

        private Dictionary<string, string> _databaseProjectInfo = new Dictionary<string, string>();
        private Dictionary<string, string> _databaseTableInfo = new Dictionary<string, string>();
        private DataScriptCollection _dataScriptCollection = new DataScriptCollection();
        private MergeScriptParseCollection _parsedMergeScripts = new MergeScriptParseCollection();
        private List<DatabaseEnvironment> _environmentList = new List<DatabaseEnvironment>();
        private DatabaseEnvironment? _currentEnvironmentLeft;
        private DatabaseEnvironment? _currentEnvironmentRight;
        private DataTable _dataTableLeft;
        private DataTable _dataTableRight;
        private object _gridSelectedRowLeft;
        private object _gridSelectedRowRight;
        private object _gridSelectedColumnLeft;
        private object _gridSelectedColumnRight;
        private string _errorsLeft;
        private string _errorsRight;

        #endregion

        #region Public Properties

        public string DatabaseSolutionDirectoryPath
        {
            get { return this._databaseSolutionDirectoryPath; }
            set
            {
                if (this.SetProperty(ref this._databaseSolutionDirectoryPath, value))
                {
                    this.DatabaseProjects = this.EnumerateDatabaseProjects(this._databaseSolutionDirectoryPath);
                }
            }
        }

        public List<string> DatabaseProjectNames
        {
            get { return this._databaseProjectNames; }
            set
            {
                value?.Sort();
                if (this.SetProperty(ref this._databaseProjectNames, value))
                {
                    if (this._databaseProjectNames.Count > 0)
                    {
                        this.CurrentDatabaseProject = this._databaseProjectNames[0];
                    }
                }
            }
        }
        public string CurrentDatabaseProject
        {
            get { return this._currentDatabaseProject; }
            set
            {
                if (this.SetProperty(ref this._currentDatabaseProject, value) && !string.IsNullOrWhiteSpace(this._currentDatabaseProject))
                {
                    this._dataScriptCollection.DatabaseProjectDirectoryPath = this._databaseProjectInfo[this._currentDatabaseProject];
                    this._databaseTableInfo = this.EnumerateDatabaseTables(this._databaseProjectInfo[this.CurrentDatabaseProject]);
                    var dataScriptNames = new List<string>();
                    if (this.StaticDataEnabled)
                    {
                        dataScriptNames.AddRange(this._dataScriptCollection.StaticDataScriptNames.ToList());
                    }
                    if (this.TestDataEnabled)
                    {
                        dataScriptNames.AddRange(this._dataScriptCollection.TestDataScriptNames.ToList());
                    }
                    this.DataScriptNames = dataScriptNames;
                }
            }
        }
        public List<string> DataScriptNames
        {
            get { return this._dataScriptNames; }
            set
            {
                value?.Sort();
                if (this.SetProperty(ref this._dataScriptNames, value))
                {
                    this.CurrentDataScript = string.Empty;
                }
            }
        }
        public string CurrentDataScript
        {
            get { return this._currentDataScript; }
            set
            {
                if (this.SetProperty(ref this._currentDataScript, value))
                {
                    if (string.IsNullOrWhiteSpace(this._currentDataScript))
                    {
                        this.ParsedMergeScripts = new MergeScriptParseCollection();
                    }
                    else
                    {
                        try
                        {
                            this.ParsedMergeScripts = this.CreateDataTableFromScript(this._currentDataScript, this._dataScriptCollection, this._databaseTableInfo);
                        }
                        catch (ValueSetException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            this.ErrorsLeft = ex.Message;
                            throw new ValueSetException("An error occured. Check the Errors tab for more details.");
                        }
                    }
                }
            }
        }
        public MergeScriptParseCollection ParsedMergeScripts
        {
            get { return this._parsedMergeScripts; }
            set
            {
                if (this.SetProperty(ref this._parsedMergeScripts, value))
                {
                    if (this._parsedMergeScripts.Items.Count < 1)
                    {
                        this.EnvironmentList = new List<DatabaseEnvironment>();
                    }
                    else
                    {
                        this.EnvironmentList = this.ParsedMergeScripts.Items.Keys.ToList();
                    }
                }
            }
        }
        public List<DatabaseEnvironment> EnvironmentList
        {
            get { return this._environmentList; }
            set
            {
                if (this.SetProperty(ref this._environmentList, value))
                {
                    this.CurrentEnvironmentLeft = null;
                    this.CurrentEnvironmentRight = null;
                    if (this._environmentList.Count > 0)
                    {
                        this.CurrentEnvironmentLeft = this.EnvironmentList[0];
                        if (this._environmentList.Count > 1)
                        {
                            this.CurrentEnvironmentRight = this.EnvironmentList[1];
                        }
                    }
                }
            }
        }
        public DatabaseEnvironment? CurrentEnvironmentLeft
        {
            get { return this._currentEnvironmentLeft; }
            set
            {
                if (this.SetProperty(ref this._currentEnvironmentLeft, value))
                {
                    if (this._currentEnvironmentLeft == null)
                    {
                        this.DeleteWhenNotMatchedLeft = false;
                        this.PrintChangesLeft = false;
                        this.DataTableLeft = null;
                        this.ErrorsLeft = string.Empty;
                    }
                    else
                    {
                        MergeScriptParseResult mergeScriptParseResult = this.ParsedMergeScripts.Items[(DatabaseEnvironment)this._currentEnvironmentLeft];
                        this.DeleteWhenNotMatchedLeft = mergeScriptParseResult.DeleteWhenNotMatched;
                        this.PrintChangesLeft = mergeScriptParseResult.PrintChanges;
                        this.DataTableLeft = mergeScriptParseResult.DataTable;
                        this.ErrorsLeft = string.Join("\r\n", mergeScriptParseResult.Warnings);
                        if (!string.IsNullOrWhiteSpace(this.ErrorsLeft))
                        {
                            throw new ValueSetException("An error occured. Check the Errors tab for more details.");
                        }
                    }
                }
            }
        }
        public DatabaseEnvironment? CurrentEnvironmentRight
        {
            get { return this._currentEnvironmentRight; }
            set
            {
                if (this.SetProperty(ref this._currentEnvironmentRight, value))
                {
                    if (this._currentEnvironmentRight == null)
                    {
                        this.DeleteWhenNotMatchedRight = false;
                        this.PrintChangesRight = false;
                        this.DataTableRight = null;
                        this.ErrorsRight = string.Empty;
                        this.DataGridRightVisible = false;
                    }
                    else
                    {
                        MergeScriptParseResult mergeScriptParseResult = this.ParsedMergeScripts.Items[(DatabaseEnvironment)this._currentEnvironmentRight];
                        this.DeleteWhenNotMatchedRight = mergeScriptParseResult.DeleteWhenNotMatched;
                        this.PrintChangesRight = mergeScriptParseResult.PrintChanges;
                        this.DataTableRight = mergeScriptParseResult.DataTable;
                        this.ErrorsRight = string.Join("\r\n", mergeScriptParseResult.Warnings);
                        this.DataGridRightVisible = true;
                        if (!string.IsNullOrWhiteSpace(this.ErrorsRight))
                        {
                            throw new ValueSetException("An error occured. Check the Errors tab for more details.");
                        }
                    }
                }
            }
        }
        public DataTable DataTableLeft
        {
            get { return this._dataTableLeft; }
            set
            {
                if (this.SetProperty(ref this._dataTableLeft, value) && this._dataTableLeft != null)
                {
                    this._dataTableLeft.TableNewRow += this.DataTable_TableNewRow;
                    this._dataTableLeft.RowChanged += this.DataTable_RowChanged;
                }
            }
        }
        public DataTable DataTableRight
        {
            get { return this._dataTableRight; }
            set
            {
                if (this.SetProperty(ref this._dataTableRight, value) && this._dataTableRight != null)
                {
                    this._dataTableRight.TableNewRow += this.DataTable_TableNewRow;
                    this._dataTableRight.RowChanged += this.DataTable_RowChanged;
                }
            }
        }
        public object GridSelectedRowLeft
        {
            get { return this._gridSelectedRowLeft; }
            set
            {
                if (this.SetProperty(ref this._gridSelectedRowLeft, value))
                {
                    this.GridSelectedRowRight = value;
                }
            }
        }
        public object GridSelectedRowRight
        {
            get { return this._gridSelectedRowRight; }
            set
            {
                if (this.DataTableRight != null)
                {
                    //if (this.SetProperty(ref this._gridSelectedRowRight, value))
                    //{

                    //this.DataTableRight.AsEnumerable().Where(row => row)

                    DataColumn[] primaryKeyColumns = this.DataTableRight.PrimaryKey;
                    string filterString = string.Empty;
                    foreach (DataColumn primaryKeyColumn in primaryKeyColumns)
                    {
                        filterString += $"{primaryKeyColumn.ColumnName} = {((DataRowView)value).Row[primaryKeyColumn.ColumnName]}" + (string.IsNullOrWhiteSpace(filterString) ? string.Empty : " AND ");
                    }
                    DataView view = this.DataTableRight.AsDataView();
                    view.RowFilter = filterString;
                    this.SetProperty(ref this._gridSelectedRowRight, view[0]);
                    //}
                }
            }
        }
        public object GridSelectedColumnLeft
        {
            get { return this._gridSelectedColumnLeft; }
            set
            {
                this.SetProperty(ref this._gridSelectedColumnLeft, value);
            }
        }
        public string ErrorsLeft
        {
            get { return this._errorsLeft; }
            set { this.SetProperty(ref this._errorsLeft, value); }
        }
        public string ErrorsRight
        {
            get { return this._errorsRight; }
            set { this.SetProperty(ref this._errorsRight, value); }
        }
        public bool StaticDataEnabled
        {
            get { return this._staticDataEnabled; }
            set
            {
                if (this.SetProperty(ref this._staticDataEnabled, value))
                {
                    this.DataScriptNames = this.GenerateDataScriptList(this._dataScriptCollection, this._staticDataEnabled, this.TestDataEnabled);
                }
            }
        }
        public bool TestDataEnabled
        {
            get { return this._testDataEnabled; }
            set
            {
                if (this.SetProperty(ref this._testDataEnabled, value))
                {
                    this.DataScriptNames = this.GenerateDataScriptList(this._dataScriptCollection, this.StaticDataEnabled, this._testDataEnabled);
                }
            }
        }
        public bool DeleteWhenNotMatchedLeft
        {
            get { return this._deleteWhenNotMatchedLeft; }
            set
            {
                this.SetProperty(ref this._deleteWhenNotMatchedLeft, value);
            }
        }
        public bool DeleteWhenNotMatchedRight
        {
            get { return this._deleteWhenNotMatchedRight; }
            set
            {
                this.SetProperty(ref this._deleteWhenNotMatchedRight, value);
            }
        }
        public bool PrintChangesLeft
        {
            get { return this._printChangesLeft; }
            set
            {
                this.SetProperty(ref this._printChangesLeft, value);
            }
        }
        public bool PrintChangesRight
        {
            get { return this._printChangesRight; }
            set
            {
                this.SetProperty(ref this._printChangesRight, value);
            }
        }
        public bool DataGridRightVisible
        {
            get { return this._dataGridRightVisible; }
            set { this.SetProperty(ref this._dataGridRightVisible, value); }
        }

        #region Private Properties

        private Dictionary<string, string> DatabaseProjects
        {
            get { return this._databaseProjectInfo; }
            set
            {
                this._databaseProjectInfo = value;
                this.DatabaseProjectNames = this._databaseProjectInfo.Keys.ToList();
            }
        }

        #endregion

        #endregion

        #region Commands

        public RelayCommand SaveLeft { get; private set; }
        public RelayCommand SaveRight { get; private set; }
        public RelayCommand ResetLeft { get; private set; }
        public RelayCommand ResetRight { get; private set; }

        #endregion

        #region Constructor

        public EditStaticDataScriptsViewModel()
        {
            this.SaveLeft = new RelayCommand(SaveLeftCmd, CanSaveLeft);
            this.SaveRight = new RelayCommand(SaveRightCmd, CanSaveRight);
            this.ResetLeft = new RelayCommand(ResetLeftCmd, CanSaveLeft);
            this.ResetRight = new RelayCommand(ResetRightCmd, CanSaveRight);
        }

        #endregion

        #region Command Definitions

        #region Save

        private void SaveLeftCmd(object parameter)
        {
            string mergeScript = SqlDataScriptHelper.NewMergeScriptFromDataTable(
                tableSchema: this.DataTableLeft.TableName.Split('.')[0],
                tableName: this.DataTableLeft.TableName.Split('.')[1],
                dataTable: this.DataTableLeft,
                deleteWhenNotMatched: this.DeleteWhenNotMatchedLeft,
                printChanges: this.PrintChangesLeft,
                environmentName: this.CurrentEnvironmentLeft.ToString(),
                primaryKeyColumnNames: this.DataTableLeft.PrimaryKey.Select(dataColumn => dataColumn.ColumnName).ToList());
            string scriptPath = this._dataScriptCollection.GetDataScriptByName(this.CurrentDataScript)[(DatabaseEnvironment)this.CurrentEnvironmentLeft].Path;
            SqlDataScriptHelper.WriteScriptToFile(scriptPath, mergeScript, true);
        }

        private bool CanSaveLeft()
        {
            //return this.DataTableLeft != null && this.DataTableLeft.GetChanges() != null && this.DataTableLeft.GetErrors().Length == 0;
            return true;
        }

        private void SaveRightCmd(object parameter)
        {
            string mergeScript = SqlDataScriptHelper.NewMergeScriptFromDataTable(
                tableSchema: this.DataTableRight.TableName.Split('.')[0],
                tableName: this.DataTableRight.TableName.Split('.')[1],
                dataTable: this.DataTableRight,
                deleteWhenNotMatched: this.DeleteWhenNotMatchedRight,
                printChanges: this.PrintChangesRight,
                environmentName: this.CurrentEnvironmentRight.ToString(),
                primaryKeyColumnNames: this.DataTableRight.PrimaryKey.Select(dataColumn => dataColumn.ColumnName).ToList());
            string scriptPath = this._dataScriptCollection.GetDataScriptByName(this.CurrentDataScript)[(DatabaseEnvironment)this.CurrentEnvironmentRight].Path;
            SqlDataScriptHelper.WriteScriptToFile(scriptPath, mergeScript, true);
        }

        private bool CanSaveRight()
        {
            //return this.DataTableRight != null && this.DataTableRight.GetChanges() != null && this.DataTableRight.GetErrors().Length == 0;
            return true;
        }

        #endregion

        #region Reset

        private void ResetLeftCmd(object parameter)
        {
            this.DataTableLeft.RejectChanges();
        }

        private void ResetRightCmd(object parameter)
        {
            this.DataTableRight.RejectChanges();
        }

        #endregion

        #endregion

        private Dictionary<string, string> EnumerateDatabaseProjects(string databaseSolutionDirectoryPath)
        {
            Dictionary<string, string> projectNameAndPath = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(databaseSolutionDirectoryPath))
            {
                string[] projectFiles = Directory.GetFiles(Path.GetDirectoryName(databaseSolutionDirectoryPath), "*.sqlproj", SearchOption.AllDirectories);
                foreach (string projectPath in projectFiles)
                {
                    projectNameAndPath.Add(Path.GetFileNameWithoutExtension(projectPath), projectPath);
                }
            }
            return projectNameAndPath;
        }

        private Dictionary<string, string> EnumerateDatabaseTables(string databaseProjectPathDirectoryPath)
        {
            DirectoryInfo databaseProjectPathDirectoryInfo = new DirectoryInfo(Path.GetDirectoryName(databaseProjectPathDirectoryPath));
            DirectoryInfo[] databaseTableDirectories = databaseProjectPathDirectoryInfo.GetDirectories("Tables", SearchOption.AllDirectories);
            Dictionary<string, string> databaseTableInfo = new Dictionary<string, string>();
            foreach (DirectoryInfo databaseTableDirectoryInfo in databaseTableDirectories)
            {
                FileInfo[] databaseTableScriptFiles = databaseTableDirectoryInfo.GetFiles("*.sql");
                foreach (FileInfo databaseTableScriptFile in databaseTableScriptFiles)
                {
                    databaseTableInfo.Add($"{databaseTableScriptFile.Directory.Parent.Name}.{Path.GetFileNameWithoutExtension(databaseTableScriptFile.Name)}", databaseTableScriptFile.FullName);
                }
            }
            return databaseTableInfo;
        }

        private List<string> GenerateDataScriptList(DataScriptCollection dataScriptCollection, bool includeStaticDataScripts, bool includeTestDataScripts)
        {
            List<string> dataScriptList = new List<string>();
            if (includeStaticDataScripts)
            {
                dataScriptList.AddRange(dataScriptCollection.StaticDataScriptNames);
            }
            if (includeTestDataScripts)
            {
                dataScriptList.AddRange(dataScriptCollection.TestDataScriptNames);
            }
            return dataScriptList;
        }

        private MergeScriptParseCollection CreateDataTableFromScript(string dataScriptKey, DataScriptCollection dataScriptCollection, Dictionary<string, string> databaseTableInfo)
        {
            string databaseTableScriptContents = File.ReadAllText(databaseTableInfo[dataScriptKey]);
            DataTable dataTable = SqlStatementParser.ParseTableCreate(databaseTableScriptContents);
            Dictionary<DatabaseEnvironment, DataScript> dataScripts = dataScriptCollection.GetDataScriptByName(dataScriptKey);
            Dictionary<DatabaseEnvironment, MergeScriptParseResult> mergeScriptParseResults = new Dictionary<DatabaseEnvironment, MergeScriptParseResult>(dataScripts.Count);
            bool containsErrors = false;
            foreach (DataScript dataScript in dataScripts.Values)
            {
                string dataScriptContents = File.ReadAllText(dataScript.Path);
                MergeScriptParseResult mergeScriptParseResult;
                try
                {
                    mergeScriptParseResult = SqlStatementParser.ParseMergeScript(dataScriptContents, dataTable);
                }
                catch (Exception ex)
                {
                    containsErrors = true;
                    mergeScriptParseResult = new MergeScriptParseResult(dataTable, false, false, new List<string> { $"Path: {dataScript.Path}\r\nException: {ex.Message}\r\nStack: {ex.StackTrace}" });
                }
                mergeScriptParseResults.Add(dataScript.Environment, mergeScriptParseResult);
            }
            return new MergeScriptParseCollection(mergeScriptParseResults, containsErrors);
        }

        private void ReevaluateCommandExecution()
        {
            this.SaveLeft.RaiseCanExecuteChanged();
            this.SaveRight.RaiseCanExecuteChanged();
        }

        #region Event Handlers

        private void DataTable_TableNewRow(object sender, DataTableNewRowEventArgs e)
        {
            DateTime editTS = DateTime.Now;
            editTS = editTS.AddMilliseconds(editTS.Millisecond * -1); 
            for (int i = 0; i < e.Row.Table.Columns.Count; i++)
            {
                DataColumn column = e.Row.Table.Columns[i];
                if (i < 2 && column.DataType == typeof(Guid) && column.ColumnName.ToLower().EndsWith("id"))
                {
                    e.Row[column] = Guid.NewGuid();
                }
                else if (i < 2 && column.DataType == typeof(int))
                {
                    var currentHighest = e.Row.Table.AsEnumerable()
                        .OrderByDescending(row => row[column])
                        .Select(row => row[column])
                        .FirstOrDefault() ?? -1;
                    e.Row[column] = (int)currentHighest + 1;
                }
                else if (i < 2 && column.DataType == typeof(byte))
                {
                    e.Row[column] = (byte)e.Row.Table.AsEnumerable().OrderByDescending(row => row[column]).Select(row => row[column]).First() + 1;
                }
                else if (column.ColumnName.ToLower() == "isactive")
                {
                    e.Row[column] = true;
                }
                else if (column.ColumnName.ToLower() == "createts" || column.ColumnName.ToLower() == "editts")
                {
                    e.Row[column] = editTS;
                }
            }
            this.ReevaluateCommandExecution();
        }

        private void DataTable_RowChanged(object sender, DataRowChangeEventArgs e)
        {
            DateTime editTS = DateTime.Now;
            editTS = editTS.AddMilliseconds(editTS.Millisecond * -1);
            e.Row.Table.RowChanged -= DataTable_RowChanged;
            try
            {
                if (e.Action == DataRowAction.Change)
                {
                    e.Row["EditTS"] = editTS;
                }
            }
            finally
            {
                e.Row.Table.RowChanged += DataTable_RowChanged;
            }
            this.ReevaluateCommandExecution();
        }

        #endregion
    }
}