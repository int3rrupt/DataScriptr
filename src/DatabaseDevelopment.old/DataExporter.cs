using DatabaseDevelopment.Enums;
using DatabaseDevelopment.Models;
using DatabaseDevelopment.Models.Schema;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using ForeignKeyConstraint = DatabaseDevelopment.Models.Schema.ForeignKeyConstraint;

namespace DatabaseDevelopment
{
    public class DataExporter
    {
        public string ServerName { get; }
        public string DatabaseName { get; }
        public DataTable PrimaryKeyConstraints { get; }
        public List<ForeignKeyConstraint> ForeignKeyConstraints { get; }
        public List<ForeignKeyConstraint> SoftConstraints { get; }
        public IgnoredDependencyLists IgnoredDependencyLists { get; }
        public Dictionary<string, TableNode> DependencyGraph { get; }

        public DataExporter(string serverName, string databaseName, string configServerName, string configDatabaseName)
        {
            ServerName = serverName;
            DatabaseName = databaseName;
            PrimaryKeyConstraints = DataReader.GetPrimaryKeyConstraintsFromDatabase(ServerName, DatabaseName);
            ForeignKeyConstraints = DataReader.GetForeignKeyConstraintsFromDatabase(ServerName, DatabaseName);
            SoftConstraints = DataReader.GetSoftConstraintsFromDatabase(configServerName, configDatabaseName);
            IgnoredDependencyLists = DataReader.GetIgnoredDependenciesFromDatabase(configServerName, configDatabaseName);
            List<ForeignKeyConstraint> allDependencies = new List<ForeignKeyConstraint>(ForeignKeyConstraints);
            allDependencies.AddRange(SoftConstraints);
            DependencyBuilder dependencyBuilder = new DependencyBuilder();
            DependencyGraph = dependencyBuilder.BuildDependency(allDependencies);
        }

        public void ExportDataToScripts(string query, string _DataDirectory, DataScriptType dataScriptType, SqlScriptType sqlScriptType, bool includeSoftDependencies, bool excludeUnwantedDependencies, bool autoMerge, string environmentName)
        {
            Dictionary<string, ExtractedTable> results = ExportDataFromDatabase(query, includeSoftDependencies, excludeUnwantedDependencies);

            string dataScriptDirectory = string.Empty;
            if (dataScriptType == DataScriptType.Static)
            {
                dataScriptDirectory = Path.Combine(_DataDirectory, "Static");
            }
            else if (dataScriptType == DataScriptType.Test)
            {
                dataScriptDirectory = Path.Combine(_DataDirectory, "Test");
            }

            if (!Directory.Exists(dataScriptDirectory))
            {
                Directory.CreateDirectory(dataScriptDirectory);
            }

            // Check for existing script files if autoMerge not set
            if (!autoMerge)
            {
                List<string> existingScripts = new List<string>();
                foreach (string tableName in results.Keys)
                {
                    string scriptName = $"{tableName}.sql";
                    if (File.Exists(Path.Combine(dataScriptDirectory, scriptName)))
                    {
                        existingScripts.Add(scriptName);
                    }
                }
                if (existingScripts.Count > 0)
                {
                    throw new System.Exception($"{sqlScriptType.ToString()} script already exists for: {string.Join(", ", existingScripts)}. Enable auto merge to merge the new data with the existing data.");
                }
            }
            // Cleanse data
            DataCleanser dataCleanser = new DataCleanser();
            dataCleanser.ReplaceImagePathFields(results.Values.Select(et => et.DataTable).ToList());

            // Create scripts for each table
            foreach (string tableName in results.Keys)
            {
                FileInfo scriptFileInfo = new FileInfo(Path.Combine(dataScriptDirectory, $"{tableName}.sql"));
                // If script already exists, merge
                if (scriptFileInfo.Exists)
                {
                    DataTable scriptDataTable = null;
                    if (sqlScriptType == SqlScriptType.Insert)
                    {
                        scriptDataTable = SqlDataScriptHelper.NewDataTableFromInsertScript(scriptFileInfo.FullName, DataParser.GetXmlSchemaFromDataTable(results[tableName].DataTable));
                    }
                    else if (sqlScriptType == SqlScriptType.Merge)
                    {
                        // todo
                        throw new System.Exception("Not implemented");
                    }
                    results[tableName].DataTable.Merge(scriptDataTable);
                }
                string script = null;
                if (sqlScriptType == SqlScriptType.Insert)
                {
                    script = SqlDataScriptHelper.NewInsertScriptFromDataTable(results[tableName].TableName.Schema, results[tableName].TableName.Name, results[tableName].DataTable, results[tableName].PrimaryKeyColumnNames);
                }
                else if (sqlScriptType == SqlScriptType.Merge)
                {
                    script = SqlDataScriptHelper.NewMergeScriptFromDataTable(results[tableName].TableName.Schema, results[tableName].TableName.Name, results[tableName].DataTable, false, true, environmentName, results[tableName].PrimaryKeyColumnNames);
                }
                SqlDataScriptHelper.WriteScriptToFile(scriptFileInfo.Name, script, dataScriptDirectory, true);
            }

            string sqlCmdScript = SqlDataScriptHelper.NewSqlCmdScript(dataScriptDirectory, dataScriptType);
            string sqlCmdScriptName = string.Empty;
            if (dataScriptType == DataScriptType.Static)
            {
                sqlCmdScriptName = "StaticData.sql";
            }
            else if (dataScriptType == DataScriptType.Test)
            {
                sqlCmdScriptName = "TestData.sql";
            }
            SqlDataScriptHelper.WriteScriptToFile(sqlCmdScriptName, sqlCmdScript, _DataDirectory, true);
        }

        public Dictionary<string, ExtractedTable> ExportDataFromDatabase(string query, bool includeSoftDependencies, bool excludeUnwantedDependencies)
        {
            Dictionary<string, ExtractedTable> extractedTables = new Dictionary<string, ExtractedTable>();
            //DataTable primaryKeyConstraints = DataReader.GetPrimaryKeyConstraintsFromDatabase(ServerName, DatabaseName);
            List<ForeignKeyConstraint> foreignKeyConstraints = new List<ForeignKeyConstraint>(ForeignKeyConstraints);
            //IgnoredDependencyLists ignoredDependencyLists = new IgnoredDependencyLists();
            if (includeSoftDependencies)
            {
                foreignKeyConstraints.AddRange(SoftConstraints);
            }
            // Parse query into separate selects
            List<SelectStatement> selectStatementsFromQuery = SqlStatementParser.ParseSelectStatement(query);

            foreach (SelectStatement selectStatement in selectStatementsFromQuery)
            {
                // Execute unmodified select to pull primary keys for new clean select query
                DataSet result = DataReader.ExecuteQuery(ServerName, DatabaseName, selectStatement.SelectText);
                // Get primary key constraints for current table
                List<string> primaryKeyColumnNames = DataReader.GetListOfPrimaryKeyConstraints(selectStatement.PrimaryTableName, PrimaryKeyConstraints).Select(row => row["ColumnName"].ToString()).ToList();
                // Get primary keys from query results
                Dictionary<string, List<string>> primaryKeysAndValues = DataReader.GetPrimaryKeyValues(selectStatement.PrimaryTableName, result.Tables[0], PrimaryKeyConstraints);
                // Create new select using primary keys
                string selectUsingPrimaryKeys = SqlStatementParser.NewSelectWhereKeysInValues(selectStatement.PrimaryTableName.FullNameDelimited, primaryKeysAndValues);
                // Execute new select
                ExtractedTable extractedTable = new ExtractedTable
                {
                    TableName = selectStatement.PrimaryTableName,
                    PrimaryKeyColumnNames = primaryKeyColumnNames,
                    DataTable = DataReader.ExecuteQueryUsingForXml(ServerName, DatabaseName, selectUsingPrimaryKeys).Tables[0]
                };
                // Set primary keys on DataTable
                DataParser.SetDataTablePrimaryKey(primaryKeyColumnNames, extractedTable.DataTable);
                extractedTable.DataTable.AcceptChanges();
                // Add to extracted tables collection
                extractedTables.Add(extractedTable.TableName.FullName, extractedTable);
                // Get dependencies
                List<TableDependency> parentDependencies = DataParser.GetParentDependenciesList(selectStatement.PrimaryTableName, foreignKeyConstraints, true);
                List<TableDependency> childDependencies = DataParser.GetChildDependenciesList(selectStatement.PrimaryTableName, foreignKeyConstraints, true);
                // Get child dependencies for all parent dependencies
                //foreach (TableDependency tableDependency in parentDependencies)
                //{
                //    childDependencies.AddRange(DataParser.GetChildDependenciesList(tableDependency.ParentTableName.Schema, tableDependency.ParentTableName.Name, foreignKeyConstraints, true));
                //}
                childDependencies = childDependencies.Distinct().ToList();
                if (excludeUnwantedDependencies)
                {
                    DataParser.RemoveUnwantedDependencies(childDependencies, IgnoredDependencyLists.ChildDependencies);
                }
                // Get parent dependencies for all child dependencies
                foreach (TableDependency tableDependency in childDependencies)
                {
                    parentDependencies.AddRange(DataParser.GetParentDependenciesList(tableDependency.ChildTableName.Schema, tableDependency.ChildTableName.Name, foreignKeyConstraints, true));
                }
                parentDependencies = parentDependencies.Distinct().ToList();
                if (excludeUnwantedDependencies)
                {
                    DataParser.RemoveUnwantedDependencies(parentDependencies, IgnoredDependencyLists.ParentDependencies);
                }
                Queue<TableDependency> parentDependencyQueue = new Queue<TableDependency>(parentDependencies);
                Queue<TableDependency> childDependencyQueue = new Queue<TableDependency>(childDependencies);
                List<TableDependency> failedParentDependencies = new List<TableDependency>();
                List<TableDependency> failedChildDependencies = new List<TableDependency>();
                while (parentDependencyQueue.Count > 0 || childDependencyQueue.Count > 0)
                {
                    while (parentDependencyQueue.Count > 0)
                    {
                        TableDependency tableDependency = parentDependencyQueue.Dequeue();
                        if (extractedTables.ContainsKey(tableDependency.ChildTableName.FullName))
                        {
                            //Write-Host "Retrieving [{foreignKeyConstraint.ConstraintColumnName)] foreign key values" todo
                            // Get foreign key values, exclude null values
                            Dictionary<string, List<string>> keyValuesList = DataParser.GetColumnValues(
                                extractedTables[tableDependency.ChildTableName.FullName].DataTable,
                                tableDependency.ColumnDependencies,
                                TableDependencyType.ChildDependency,
                                TableDependencyType.ParentDependency,
                                true,
                                true);
                            if (keyValuesList.Count > 0)
                            {
                                DataTable dataTable = RetrieveTable(ServerName, DatabaseName, tableDependency.ParentTableName, keyValuesList, PrimaryKeyConstraints);
                                int originalRowCount = 0;
                                // Add parent table to overall results
                                if (extractedTables.ContainsKey(tableDependency.ParentTableName.FullName))
                                {
                                    originalRowCount = extractedTables[tableDependency.ParentTableName.FullName].DataTable.Rows.Count;
                                    extractedTables[tableDependency.ParentTableName.FullName].DataTable.Merge(dataTable);
                                }
                                else
                                {
                                    extractedTables.Add(
                                        tableDependency.ParentTableName.FullName,
                                        new ExtractedTable
                                        {
                                            TableName = tableDependency.ParentTableName,
                                            PrimaryKeyColumnNames = primaryKeyColumnNames,
                                            DataTable = dataTable
                                        }
                                    );
                                    failedParentDependencies.Remove(tableDependency);
                                }
                                //if (originalRowCount < extractedTables[tableDependency.ParentTableName.FullName].DataTable.Rows.Count)
                                //{
                                //    List<TableDependency> tmp = childDependencyQueue.ToList();
                                //    foreach (TableDependency dependency in DataParser.GetChildDependenciesList(tableDependency.ParentTableName.Schema, tableDependency.ParentTableName.Name, foreignKeyConstraints, true))
                                //    {
                                //        tmp.Add(dependency);
                                //    }
                                //    tmp = tmp.Distinct().ToList();
                                //    DataParser.RemoveUnwantedDependencies(tmp, IgnoredDependencyLists.ChildDependencies);
                                //    childDependencyQueue = new Queue<TableDependency>(tmp);
                                //}
                            }
                            else
                            {
                                //Write-Host "Table $parentTableNameDelimited not retrieved. $childTableNameDelimited.[$(foreignKeyConstraint.ConstraintColumnName)] values are all DBNull"; todo
                            }
                        }
                        else
                        {
                            if (!failedParentDependencies.Contains(tableDependency))
                            {
                                failedParentDependencies.Add(tableDependency);
                                parentDependencyQueue.Enqueue(tableDependency);
                            }
                        }
                    }
                    while (childDependencyQueue.Count > 0)
                    {
                        TableDependency tableDependency = childDependencyQueue.Dequeue();
                        if (extractedTables.ContainsKey(tableDependency.ParentTableName.FullName))
                        {
                            //Write-Host "Retrieving [{foreignKeyConstraint.ConstraintColumnName)] foreign key values" todo
                            // Get foreign key values, exclude null values
                            Dictionary<string, List<string>> keyValuesList = DataParser.GetColumnValues(
                                extractedTables[tableDependency.ParentTableName.FullName].DataTable,
                                tableDependency.ColumnDependencies,
                                TableDependencyType.ParentDependency,
                                TableDependencyType.ChildDependency,
                                true,
                                true);
                            if (keyValuesList.Count > 0)
                            {
                                DataTable dataTable = RetrieveTable(ServerName, DatabaseName, tableDependency.ChildTableName, keyValuesList, PrimaryKeyConstraints);
                                if (dataTable.Rows.Count > 0)
                                {
                                    int originalRowCount = 0;
                                    // Add parent table to overall results
                                    if (extractedTables.ContainsKey(tableDependency.ChildTableName.FullName))
                                    {
                                        originalRowCount = extractedTables[tableDependency.ChildTableName.FullName].DataTable.Rows.Count;
                                        extractedTables[tableDependency.ChildTableName.FullName].DataTable.Merge(dataTable);
                                    }
                                    else
                                    {
                                        extractedTables.Add(
                                            tableDependency.ChildTableName.FullName,
                                            new ExtractedTable
                                            {
                                                TableName = tableDependency.ChildTableName,
                                                PrimaryKeyColumnNames = primaryKeyColumnNames,
                                                DataTable = dataTable
                                            }
                                        );
                                    }
                                    if (originalRowCount < extractedTables[tableDependency.ChildTableName.FullName].DataTable.Rows.Count)
                                    {
                                        List<TableDependency> tmp = parentDependencyQueue.ToList();
                                        foreach (TableDependency dependency in DataParser.GetParentDependenciesList(tableDependency.ChildTableName.Schema, tableDependency.ChildTableName.Name, foreignKeyConstraints, true))
                                        {
                                            tmp.Add(dependency);
                                        }
                                        tmp = tmp.Distinct().ToList();
                                        DataParser.RemoveUnwantedDependencies(tmp, IgnoredDependencyLists.ParentDependencies);
                                        parentDependencyQueue = new Queue<TableDependency>(tmp);
                                    }
                                }
                                else
                                {
                                    // todo write warning in log
                                }
                            }
                            else
                            {
                                //Write-Host "Table $parentTableNameDelimited not retrieved. $childTableNameDelimited.[$(foreignKeyConstraint.ConstraintColumnName)] values are all DBNull"; todo
                            }
                        }
                        else
                        {
                            if (!failedChildDependencies.Contains(tableDependency))
                            {
                                failedChildDependencies.Add(tableDependency);
                                childDependencyQueue.Enqueue(tableDependency);
                            }
                        }
                    }
                }
            }

            return extractedTables;
        }

        public static DataTable RetrieveTable(string serverName, string databaseName, TableName parentTableName, Dictionary<string, List<string>> keysAndValues, DataTable primaryKeyConstraints)
        {// TODO move this logic into RetrieveTableWhereKeysInValues
            DataTable dataTable = new DataTable();
            if (keysAndValues.Count > 0)
            {
                // Determine primary key constraints
                List<string> primaryKeyColumnNames = DataReader.GetListOfPrimaryKeyConstraints(parentTableName.Schema, parentTableName.Name, primaryKeyConstraints).Select(row => row["ColumnName"].ToString()).ToList();
                dataTable = DataReader.RetrieveTableWhereKeysInValues(serverName, databaseName, parentTableName, keysAndValues, primaryKeyColumnNames);
            }
            else
            {
                //Write-Host "Table $parentTableNameDelimited not retrieved. $childTableNameDelimited.[$(foreignKeyConstraint.ConstraintColumnName)] values are all DBNull"; todo
            }

            return dataTable;
        }

        //public void ExportDataFromDatabase2(string query, bool includeSoftDependencies, bool excludeUnwantedDependencies)
        //{
        //    Dictionary<string, ExtractedTable> extractedTables = new Dictionary<string, ExtractedTable>();

        //    // Parse query into separate selects
        //    List<SelectStatement> selectStatementsFromQuery = SqlQueryParser.ParseSelectStatement(query);

        //    foreach (SelectStatement selectStatement in selectStatementsFromQuery)
        //    {
        //        // Execute unmodified select to pull primary keys for new clean select query
        //        DataSet result = DataReader.ExecuteQueryUsingForXml(ServerName, DatabaseName, selectStatement.SelectText);
        //        // Get primary key constraints for current table
        //        List<string> primaryKeyColumnNames = DataReader.GetListOfPrimaryKeyConstraints(selectStatement.PrimaryTableSchema, selectStatement.PrimaryTableName, PrimaryKeyConstraints).Select(row => row["ColumnName"].ToString()).ToList();
        //        // Get primary keys from query results
        //        Dictionary<string, List<string>> primaryKeysAndValues = DataReader.GetPrimaryKeyValues(selectStatement.PrimaryTableSchema, selectStatement.PrimaryTableName, result.Tables[selectStatement.PrimaryTableFullName], PrimaryKeyConstraints);
        //        // Create new select using primary keys
        //        string selectUsingPrimaryKeys = SqlQueryParser.NewSelectWhereKeysInValues(selectStatement.PrimaryTableFullNameDelimited, primaryKeysAndValues);
        //        // Execute new select
        //        ExtractedTable extractedTable = new ExtractedTable
        //        {
        //            TableName = new TableName(selectStatement.PrimaryTableSchema, selectStatement.PrimaryTableName),
        //            PrimaryKeyColumnNames = primaryKeyColumnNames,
        //            DataTable = DataReader.ExecuteQueryUsingForXml(ServerName, DatabaseName, selectUsingPrimaryKeys).Tables[0]
        //        };
        //        // Set primary keys on DataTable
        //        DataParser.SetDataTablePrimaryKey(primaryKeyColumnNames, extractedTable.DataTable);
        //        extractedTable.DataTable.AcceptChanges();
        //        // Add to extracted tables collection
        //        extractedTables.Add(extractedTable.TableName.FullName, extractedTable);


        //        TableNode filing = DependencyGraph["CLERK.FBN_FILING"];
        //        Dictionary<string, List<string>> keyValuesList = DataParser.GetColumnValues(
        //            dataTableContainingKeys,
        //            dependency.ColumnDependencies,
        //            tableDependencyType,
        //            outputColumnDependencyType,
        //            true,
        //            true);
        //        GetTables(filing, extractedTables, TableDependencyType.ChildDependency);
        //    }
        //}

        //public void GetTables(TableNode tableNode, Dictionary<string, ExtractedTable> extractedTables, TableDependencyType tableDependencyType)
        //{
        //    Stack<TableNode> stack = new Stack<TableNode>();
        //    stack.Push(tableNode);

        //    while (stack.Count > 0)
        //    {
        //        TableNode currentTableNode = stack.Pop();
        //        foreach (List<TableDependency2> tableDependency2List in currentTableNode.ChildDependencies.Values)
        //        {
        //            foreach (TableDependency2 dependency in tableDependency2List)
        //            {
        //                Dictionary<string, List<string>> keyValuesList = DataParser.GetColumnValues(
        //                    extractedTables[tableNode.Name.FullName].DataTable,
        //                    dependency.ColumnDependencies,
        //                    TableDependencyType.ParentDependency,
        //                    TableDependencyType.ChildDependency,
        //                    true,
        //                    true);
        //                DataTable dataTable = RetrieveTable2(keyValuesList, dependency.Table.Name, extractedTables);
        //                GetTables(dependency.Table, extractedTables, tableDependencyType);
        //            }
        //        }
        //    }

        //    if (dataTable != null)
        //    {

        //    }
        //}

        //public DataTable RetrieveTable2(Dictionary<string, List<string>> keyValuesList, TableName tableName, Dictionary<string, ExtractedTable> extractedTables)
        //{
        //    DataTable dataTable = null;

        //    // This would only work when retrieving using primary key (retrieving parent)
        //    //if (extractedTables.ContainsKey(dependency.Table.Name.FullName))
        //    //{
        //    //    string whereClause = SqlQueryParser.NewWhereClauseFromKeys(keyValuesList);
        //    //    DataRow[] tmp = extractedTables[dependency.Table.Name.FullName].DataTable.Select(whereClause);
        //    //}
        //    if (keyValuesList.Count > 0)
        //    {
        //        dataTable = RetrieveTable(ServerName, DatabaseName, tableName, keyValuesList, PrimaryKeyConstraints);
        //        if (dataTable.Rows.Count > 0)
        //        {
        //            // Get primary key constraints for current table
        //            List<string> primaryKeyColumnNames = DataReader.GetListOfPrimaryKeyConstraints(
        //                tableName.Schema,
        //                tableName.Name,
        //                PrimaryKeyConstraints).Select(row => row["ColumnName"].ToString()).ToList();
        //            int originalRowCount = 0;
        //            // Add table to overall results
        //            if (extractedTables.ContainsKey(tableName.FullName))
        //            {
        //                originalRowCount = extractedTables[tableName.FullName].DataTable.Rows.Count;
        //                extractedTables[tableName.FullName].DataTable.Merge(dataTable);
        //            }
        //            else
        //            {
        //                extractedTables.Add(
        //                    tableName.FullName,
        //                    new ExtractedTable
        //                    {
        //                        TableName = tableName,
        //                        PrimaryKeyColumnNames = primaryKeyColumnNames,
        //                        DataTable = dataTable
        //                    }
        //                );
        //            }
        //            //if (originalRowCount < extractedTables[tableDependency.ChildTableName.FullName].DataTable.Rows.Count)
        //            //{
        //            //    List<TableDependency> tmp = parentDependencyQueue.ToList();
        //            //    foreach (TableDependency dependency in DataParser.GetParentDependenciesList(tableDependency.ChildTableName.Schema, tableDependency.ChildTableName.Name, foreignKeyConstraints, true))
        //            //    {
        //            //        tmp.Add(dependency);
        //            //    }
        //            //    tmp = tmp.Distinct().ToList();
        //            //    DataParser.RemoveUnwantedDependencies(tmp, ignoredDependencyLists.ParentDependencies);
        //            //    parentDependencyQueue = new Queue<TableDependency>(tmp);
        //            //}
        //        }
        //        else
        //        {
        //            // todo write warning in log
        //            dataTable = null;
        //        }
        //    }

        //    return dataTable;
        //}
    }
}