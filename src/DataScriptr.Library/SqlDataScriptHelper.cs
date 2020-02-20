using DataScriptr.Library.Enums;
using DataScriptr.Library.Models.Schema;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TSQL;
using TSQL.Tokens;

namespace DataScriptr.Library
{
    public static class SqlDataScriptHelper
    {
        #region Insert Scripts

        public static string NewInsertScriptFromDataTable(string tableSchema, string tableName, DataTable dataTable, List<string> primaryKeyColumnNames = null)
        {
            string indent = "    ";
            string script = $"INSERT INTO [{tableSchema}].[{tableName}]";
            // Add column names
            script += Environment.NewLine;
            script += $"{indent}(";
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                script += $"[{dataTable.Columns[i].ColumnName}]";
                if (i < (dataTable.Columns.Count - 1))
                {
                    script += ", ";
                }
            }
            script += ")";
            script += Environment.NewLine;
            script += "VALUES";
            script += Environment.NewLine;
            // Get primary keys
            string primaryKeyNamesString = string.Join(", ", dataTable.PrimaryKey.Select(dataColumn => dataColumn.ColumnName));
            //string primaryKeyNamesString = string.Join(", ", primaryKeyColumnNames);
            DataView dataView = new DataView(dataTable);
            dataView.Sort = primaryKeyNamesString;
            // Add rows as values
            //foreach (DataRow row in extractedTable.DataTable.AsEnumerable().OrderBy(row => row["TransactionNumber"]))
            //foreach (DataRow row in extractedTable.DataTable.Rows)
            int rowCount = 0;
            foreach (DataRowView row in dataView)
            {
                script += $"{indent}(";
                // Iterate through row fields
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    script += TypedObjectToSqlValueString(row[dataTable.Columns[i].ColumnName]);
                    if (i != (dataTable.Columns.Count - 1))
                    {
                        script += ", ";
                    }
                }
                script += ")";
                rowCount++;
                if (rowCount != dataView.Count)
                //if ((extractedTable.DataTable.Rows.IndexOf(row) + 1) != extractedTable.DataTable.Rows.Count)
                {
                    script += ",";
                    script += Environment.NewLine;
                }
            }

            return script;
        }

        public static Dictionary<string, string> NewInsertScriptFromDataTables(Dictionary<string, ExtractedTable> exportList, bool overwriteExistingFiles)
        {
            Dictionary<string, string> insertScripts = new Dictionary<string, string>();
            string indent = "    ";
            foreach (ExtractedTable extractedTable in exportList.Values)
            {
                string script = $"INSERT INTO [{extractedTable.TableName.Schema}].[{extractedTable.TableName}]";
                // Add column names
                script += Environment.NewLine;
                script += $"{indent}(";
                for (int i = 0; i < extractedTable.DataTable.Columns.Count; i++)
                {
                    script += $"[{extractedTable.DataTable.Columns[i].ColumnName}]";
                    if (i < (extractedTable.DataTable.Columns.Count - 1))
                    {
                        script += ", ";
                    }
                }
                script += ")";
                script += Environment.NewLine;
                script += "VALUES";
                script += Environment.NewLine;
                string primaryKeyNamesString = string.Join(", ", extractedTable.PrimaryKeyColumnNames);
                DataView dataView = new DataView(extractedTable.DataTable);
                dataView.Sort = primaryKeyNamesString;
                // Add rows as values
                //foreach (DataRow row in extractedTable.DataTable.AsEnumerable().OrderBy(row => row["TransactionNumber"]))
                //foreach (DataRow row in extractedTable.DataTable.Rows)
                int rowCount = 0;
                foreach (DataRowView row in dataView)
                {
                    script += $"{indent}(";
                    // Iterate through row fields
                    for (int i = 0; i < extractedTable.DataTable.Columns.Count; i++)
                    {
                        script += TypedObjectToSqlValueString(row[extractedTable.DataTable.Columns[i].ColumnName]);
                        if (i != (extractedTable.DataTable.Columns.Count - 1))
                        {
                            script += ", ";
                        }
                    }
                    script += ")";
                    rowCount++;
                    if (rowCount != dataView.Count)
                    //if ((extractedTable.DataTable.Rows.IndexOf(row) + 1) != extractedTable.DataTable.Rows.Count)
                    {
                        script += ",";
                        script += Environment.NewLine;
                    }
                }

                insertScripts.Add(extractedTable.TableName.FullName, script);
            }

            return insertScripts;
        }

        #endregion

        #region Merge Scripts

        public static string NewMergeScriptFromDataTable(string tableSchema, string tableName, DataTable dataTable, bool deleteWhenNotMatched, bool printChanges, string environmentName, List<string> primaryKeyColumnNames = null)
        {
            // Get primary keys
            List<string> primaryKeyNameList = null;
            if (primaryKeyColumnNames == null)
            {
                primaryKeyNameList = dataTable.PrimaryKey.Select(dataColumn => dataColumn.ColumnName).ToList();
            }
            else
            {
                primaryKeyNameList = primaryKeyColumnNames;
            }
            string mergeOnString = string.Empty;
            foreach (string primaryKeyName in primaryKeyNameList)
            {
                mergeOnString += string.IsNullOrWhiteSpace(mergeOnString) ? string.Empty : " AND ";
                mergeOnString += $"[Target].[{primaryKeyName}] = [Source].[{primaryKeyName}]";
            }

            // Get column names
            string sourceTargetColumnMatchString = string.Empty;
            string sourceUpdateValuesString = string.Empty;
            string sourceInsertValuesString = string.Empty;
            string columnNamesString = string.Empty; //$"(";
            string changesTableDefinition = string.Empty;
            string changesTableDeletedColumns = string.Empty;
            string changesTableInsertedColumns = string.Empty;
            string changesOutputClause = string.Empty;
            string changesOutputDeletedColumns = string.Empty;
            string changesOutputInsertedColumns = string.Empty;
            string changesJsonVariable = string.Empty;
            List<string> columnNameList = new List<string>();
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                columnNameList.Add(dataTable.Columns[i].ColumnName);
                string currentColumnName = columnNameList[i];
                if (!primaryKeyNameList.Contains(currentColumnName))
                {
                    sourceTargetColumnMatchString += $"  NULLIF([Source].[{currentColumnName}], [Target].[{currentColumnName}]) IS NOT NULL OR NULLIF([Target].[{currentColumnName}], [Source].[{currentColumnName}]) IS NOT NULL";
                    sourceUpdateValuesString += $"  [Target].[{currentColumnName}] = [Source].[{currentColumnName}]";
                    if (i < (dataTable.Columns.Count - 1))
                    {
                        sourceTargetColumnMatchString += " OR";
                        sourceTargetColumnMatchString += System.Environment.NewLine;
                        sourceUpdateValuesString += ",";
                        sourceUpdateValuesString += System.Environment.NewLine;
                    }
                }
                sourceInsertValuesString += $"[Source].[{currentColumnName}]";
                columnNamesString += $"[{currentColumnName}]";
                if (printChanges)
                {
                    changesTableDeletedColumns += $",[DELETED_{currentColumnName}] {dataTable.Columns[currentColumnName].ExtendedProperties["SqlDbEngineType"]}";
                    changesTableInsertedColumns += $",[INSERTED_{currentColumnName}] {dataTable.Columns[currentColumnName].ExtendedProperties["SqlDbEngineType"]}";
                    changesOutputDeletedColumns += $",DELETED.[{currentColumnName}]";
                    changesOutputInsertedColumns += $",INSERTED.[{currentColumnName}]";
                }
                if (i < (dataTable.Columns.Count - 1))
                {
                    sourceInsertValuesString += ",";
                    columnNamesString += ",";
                }
            }

            DataView dataView = new DataView(dataTable);
            dataView.Sort = string.Join(", ", primaryKeyNameList);
            string rowValuesString = string.Empty;
            if (dataView.Count > 0)
            {
                rowValuesString = $"VALUES{Environment.NewLine}";
                int rowCount = 0;
                foreach (DataRowView row in dataView)
                {
                    rowValuesString += rowCount == 0 ? string.Empty : Environment.NewLine;
                    rowValuesString += rowCount == 0 ? "  " : " ,";
                    rowValuesString += DataRowValuesToString(row, columnNameList, false);
                    rowCount++;
                }
            }
            else
            {
                rowValuesString = $"{Environment.NewLine}  SELECT {columnNamesString} FROM [{tableSchema}].[{tableName}] WHERE 1 = 0 -- Empty dataset";
            }
            string libraryName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            string libraryVersion = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).ProductVersion;

            string uniqueVar = $"{tableSchema}_{tableName}_{environmentName}";
            string mergeScript = string.Empty;
            // Comment
            mergeScript += $"-- MERGE script generated by '{libraryName}' library, Version {libraryVersion}" + Environment.NewLine;
            mergeScript += $"-- Date {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}{changesTableDefinition}" + Environment.NewLine;
            // Changes Table Declaration
            if (printChanges)
            {
                mergeScript += $"DECLARE @CHANGES_{uniqueVar} TABLE([Action] NVARCHAR(10){changesTableDeletedColumns}{changesTableInsertedColumns})" + Environment.NewLine;
            }
            // Merge Statement
            mergeScript += $"MERGE INTO [{tableSchema}].[{tableName}] AS [Target]" + Environment.NewLine;
            mergeScript += $"USING ({rowValuesString}" + Environment.NewLine;
            mergeScript += $") AS [Source] ({columnNamesString})" + Environment.NewLine;
            mergeScript += $"ON ({mergeOnString})" + Environment.NewLine;
            // When matched, Update
            mergeScript += $"WHEN MATCHED AND (" + Environment.NewLine;
            mergeScript += $"{sourceTargetColumnMatchString}) THEN" + Environment.NewLine;
            mergeScript += $" UPDATE SET" + Environment.NewLine;
            mergeScript += $"{sourceUpdateValuesString}" + Environment.NewLine;
            // When not in target, insert
            mergeScript += $"WHEN NOT MATCHED BY TARGET THEN" + Environment.NewLine;
            mergeScript += $" INSERT({columnNamesString})" + Environment.NewLine;
            mergeScript += $" VALUES({sourceInsertValuesString})" + Environment.NewLine;
            // When not in source, delete
            if (deleteWhenNotMatched)
            {
                mergeScript += $"WHEN NOT MATCHED BY SOURCE THEN" + Environment.NewLine;
                mergeScript += $" DELETE" + Environment.NewLine;
            }
            // Output
            if (printChanges)
            {
                mergeScript += $"OUTPUT" + Environment.NewLine;
                mergeScript += $" $ACTION AS [Action]{changesOutputDeletedColumns}{changesOutputInsertedColumns}" + Environment.NewLine;
                mergeScript += $" INTO @CHANGES_{uniqueVar}";
            }
            // End Merge Statement
            mergeScript += $";" + Environment.NewLine;
            // Print rows affected
            mergeScript += $"PRINT '[DATA]:  ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' row(s) affected on [{tableSchema}].[{tableName}]'" + Environment.NewLine;
            // Print changes
            if (printChanges)
            {
                // Check for changes
                mergeScript += $"IF EXISTS(SELECT * FROM @CHANGES_{uniqueVar}) BEGIN" + Environment.NewLine;
                // JSON
                mergeScript += $" DECLARE @JSON_{uniqueVar} NVARCHAR(MAX) = (SELECT * FROM @CHANGES_{uniqueVar} FOR JSON PATH, ROOT('{tableSchema}.{tableName}'))" + Environment.NewLine;
                mergeScript += $" -- JSON changes prefix" + Environment.NewLine;
                mergeScript += $" DECLARE @JSON_ChangeLinePrefix_{uniqueVar} varchar(20) = '[JSON_CHANGES]:'" + Environment.NewLine;
                mergeScript += $" -- Maximum number of characters per print statement" + Environment.NewLine;
                mergeScript += $" DECLARE @PRINT_MaxCharLength_{uniqueVar} INT = 4000 - DATALENGTH(@JSON_ChangeLinePrefix_{uniqueVar})" + Environment.NewLine;
                mergeScript += $" -- Print statement counter" + Environment.NewLine;
                mergeScript += $" DECLARE @COUNTER_{uniqueVar} INT = 0" + Environment.NewLine;
                mergeScript += $" -- JSON changes string length" + Environment.NewLine;
                mergeScript += $" DECLARE @JSON_ChangesLength_{uniqueVar} int = LEN(@JSON_{uniqueVar})" + Environment.NewLine;
                mergeScript += $" -- Determine number of print statements needed" + Environment.NewLine;
                mergeScript += $" DECLARE @PRINT_Count_{uniqueVar} INT = (@JSON_ChangesLength_{uniqueVar} / @PRINT_MaxCharLength_{uniqueVar})" + Environment.NewLine;
                mergeScript += $" -- Additional print statement is needed when there is a remainder" + Environment.NewLine;
                mergeScript += $" IF(@JSON_ChangesLength_{uniqueVar} % @PRINT_MaxCharLength_{uniqueVar} > 0) BEGIN" + Environment.NewLine;
                mergeScript += $"   SET @PRINT_Count_{uniqueVar} = @PRINT_Count_{uniqueVar} + 1" + Environment.NewLine;
                mergeScript += $" END" + Environment.NewLine;
                mergeScript += $" -- Print changes" + Environment.NewLine;



                mergeScript += $" PRINT '[JSON_CHANGES_BEGIN]'" + Environment.NewLine;
                mergeScript += $" WHILE @COUNTER_{uniqueVar} < @PRINT_Count_{uniqueVar} BEGIN" + Environment.NewLine;
                mergeScript += $"   PRINT @JSON_ChangeLinePrefix_{uniqueVar} + SUBSTRING(@JSON_{uniqueVar}, @PRINT_MaxCharLength_{uniqueVar} * @COUNTER_{uniqueVar}, @PRINT_MaxCharLength_{uniqueVar}) " + Environment.NewLine;
                mergeScript += $"   SET @COUNTER_{uniqueVar} = @COUNTER_{uniqueVar} + 1" + Environment.NewLine;
                mergeScript += $" END" + Environment.NewLine;
                mergeScript += $" PRINT '[JSON_CHANGES_END]'" + Environment.NewLine;
                mergeScript += $"END" + Environment.NewLine;
            }

            return mergeScript;
        }

        #endregion

        #region SQL CMD Scripts

        public static string NewSqlCmdScript(string scriptsDirectory, DataScriptType dataScriptType)
        {
            if (!Directory.Exists(scriptsDirectory))
            {
                throw new Exception($"Direcotry {scriptsDirectory} does not exist.");
            }
            string[] files = Directory.GetFiles(scriptsDirectory, "*.sql");
            List<string> remainingScripts = new List<string>();
            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file).ToUpper();
                // Check for schema and table name
                if (fileName.Split('.').Length != 2)
                {
                    // TODO log event
                    continue;
                }
                remainingScripts.Add(fileName);
            }
            Queue<string> scriptQueue = new Queue<string>(remainingScripts);
            string disableConstraintTables = string.Empty;
            string scripts = string.Empty;
            string dataScriptRootVariable = string.Empty;
            string dataScriptRoot = string.Empty;
            string dataScriptTypeString = string.Empty;
            if (dataScriptType == DataScriptType.Static)
            {
                dataScriptTypeString = "Static";

            }
            else if (dataScriptType == DataScriptType.Test)
            {
                dataScriptTypeString = "Test";
            }
            dataScriptRootVariable = $"{dataScriptTypeString}DataScriptRoot";
            dataScriptRoot = $@".\{dataScriptTypeString}";
            while (scriptQueue.Count > 0)
            {
                string fileName = scriptQueue.Dequeue();
                string[] splitFileName = fileName.Split('.');
                string schema = splitFileName[0];
                string tableName = splitFileName[1];
                //bool tmp = false;
                //while (!tmp)
                //{
                //foreach (TableDependency dependency in tableDependencies.Where(td => td.ChildTableName.Schema == schema && td.ChildTableName.Name == tableName))
                //{
                disableConstraintTables += $"    (N'{schema}', N'{tableName}')";
                scripts += $@":R $({dataScriptRootVariable})\{fileName}.sql";
                if (scriptQueue.Count > 0)
                {
                    disableConstraintTables += ",";
                    disableConstraintTables += System.Environment.NewLine;
                    scripts += System.Environment.NewLine;
                }
                //}
                //}
            }

            string sqlCmdScript = string.Empty;
            sqlCmdScript += $"/*" + Environment.NewLine;
            sqlCmdScript += $"* {dataScriptTypeString} Data Post-Deployment Script" + Environment.NewLine;
            sqlCmdScript += $"*" + Environment.NewLine;
            sqlCmdScript += $"* This file executes all the {dataScriptTypeString.ToLower()} data files for the current database." + Environment.NewLine;
            sqlCmdScript += $"* SQLCMD Variables must be set in the sql project properties." + Environment.NewLine;
            sqlCmdScript += $"*/" + Environment.NewLine;
            sqlCmdScript += Environment.NewLine;
            sqlCmdScript += $"SET NOCOUNT ON" + Environment.NewLine;
            sqlCmdScript += Environment.NewLine;
            sqlCmdScript += $"PRINT '--------------------------------------------------------------------------------------------------------'" + Environment.NewLine;
            sqlCmdScript += $"PRINT 'Starting {dataScriptTypeString} Data Post-Deployment Script for $(DatabaseName)...'" + Environment.NewLine;
            sqlCmdScript += $"PRINT '--------------------------------------------------------------------------------------------------------'" + Environment.NewLine;
            sqlCmdScript += Environment.NewLine;
            sqlCmdScript += $"DECLARE @{dataScriptTypeString}DataScripts TABLE (TableSchema NVARCHAR(50), TableName NVARCHAR(50))" + Environment.NewLine;
            sqlCmdScript += $"DECLARE @{dataScriptTypeString}DataTableSchema NVARCHAR(50)" + Environment.NewLine;
            sqlCmdScript += $"DECLARE @{dataScriptTypeString}DataTableName NVARCHAR(50)" + Environment.NewLine;
            sqlCmdScript += $"DECLARE @{dataScriptTypeString}DataCommand NVARCHAR(2000) = N''" + Environment.NewLine;
            sqlCmdScript += $"DECLARE {dataScriptTypeString}DataScriptsCursor CURSOR FOR" + Environment.NewLine;
            sqlCmdScript += $"    SELECT TableSchema, TableName" + Environment.NewLine;
            sqlCmdScript += $"    FROM @{dataScriptTypeString}DataScripts" + Environment.NewLine;
            sqlCmdScript += Environment.NewLine;
            sqlCmdScript += Environment.NewLine;
            sqlCmdScript += $"--------------------------------------------------------------------------------------" + Environment.NewLine;
            sqlCmdScript += $"-- Tables for which constraint checks will be disabled" + Environment.NewLine;
            sqlCmdScript += $"--------------------------------------------------------------------------------------" + Environment.NewLine;
            sqlCmdScript += $"INSERT INTO @{dataScriptTypeString}DataScripts" + Environment.NewLine;
            sqlCmdScript += $"VALUES" + Environment.NewLine;
            sqlCmdScript += $"    -- Schema Name                  Table Name" + Environment.NewLine;
            sqlCmdScript += $"{disableConstraintTables}" + Environment.NewLine;
            sqlCmdScript += Environment.NewLine;
            sqlCmdScript += Environment.NewLine;
            sqlCmdScript += $"--------------------------------------------------------------------------------------" + Environment.NewLine;
            sqlCmdScript += $"-- DISABLE constraint checks for listed tables" + Environment.NewLine;
            sqlCmdScript += $"--------------------------------------------------------------------------------------" + Environment.NewLine;
            sqlCmdScript += $"OPEN {dataScriptTypeString}DataScriptsCursor" + Environment.NewLine;
            sqlCmdScript += $"FETCH NEXT FROM {dataScriptTypeString}DataScriptsCursor" + Environment.NewLine;
            sqlCmdScript += $"INTO @{dataScriptTypeString}DataTableSchema, @{dataScriptTypeString}DataTableName" + Environment.NewLine;
            sqlCmdScript += Environment.NewLine;
            sqlCmdScript += $"WHILE @@FETCH_STATUS = 0" + Environment.NewLine;
            sqlCmdScript += $"BEGIN" + Environment.NewLine;
            sqlCmdScript += $"    PRINT '[INFO]: Disabling Constraints [' + @{dataScriptTypeString}DataTableSchema + '].[' + @{dataScriptTypeString}DataTableName + ']'" + Environment.NewLine;
            sqlCmdScript += $"    SET @{dataScriptTypeString}DataCommand += N'ALTER TABLE [' + @{dataScriptTypeString}DataTableSchema + N'].[' + @{dataScriptTypeString}DataTableName + N'] NOCHECK CONSTRAINT ALL; '" + Environment.NewLine;
            sqlCmdScript += $"    FETCH NEXT FROM {dataScriptTypeString}DataScriptsCursor" + Environment.NewLine;
            sqlCmdScript += $"    INTO @{dataScriptTypeString}DataTableSchema, @{dataScriptTypeString}DataTableName" + Environment.NewLine;
            sqlCmdScript += $"END" + Environment.NewLine;
            sqlCmdScript += $"CLOSE {dataScriptTypeString}DataScriptsCursor" + Environment.NewLine;
            sqlCmdScript += $"" + Environment.NewLine;
            sqlCmdScript += $"EXEC sp_executesql @{dataScriptTypeString}DataCommand" + Environment.NewLine;
            sqlCmdScript += Environment.NewLine;
            sqlCmdScript += Environment.NewLine;
            sqlCmdScript += $"--------------------------------------------------------------------------------------" + Environment.NewLine;
            sqlCmdScript += $"-- Variable Declarations" + Environment.NewLine;
            sqlCmdScript += $"--------------------------------------------------------------------------------------" + Environment.NewLine;
            sqlCmdScript += $":SETVAR {dataScriptRootVariable} \"{dataScriptRoot}\"" + Environment.NewLine;
            sqlCmdScript += Environment.NewLine;
            sqlCmdScript += Environment.NewLine;
            sqlCmdScript += $"--------------------------------------------------------------------------------------" + Environment.NewLine;
            sqlCmdScript += $"-- {dataScriptTypeString} Data Files" + Environment.NewLine;
            sqlCmdScript += $"--------------------------------------------------------------------------------------" + Environment.NewLine;
            sqlCmdScript += $"{scripts}" + Environment.NewLine;
            sqlCmdScript += Environment.NewLine;
            sqlCmdScript += Environment.NewLine;
            sqlCmdScript += $"--------------------------------------------------------------------------------------" + Environment.NewLine;
            sqlCmdScript += $"-- ENABLE constraint checks for listed tables" + Environment.NewLine;
            sqlCmdScript += $"--------------------------------------------------------------------------------------" + Environment.NewLine;
            sqlCmdScript += $"SET @{dataScriptTypeString}DataCommand = N''" + Environment.NewLine;
            sqlCmdScript += Environment.NewLine;
            sqlCmdScript += $"OPEN {dataScriptTypeString}DataScriptsCursor" + Environment.NewLine;
            sqlCmdScript += $"FETCH NEXT FROM {dataScriptTypeString}DataScriptsCursor" + Environment.NewLine;
            sqlCmdScript += $"INTO @{dataScriptTypeString}DataTableSchema, @{dataScriptTypeString}DataTableName" + Environment.NewLine;
            sqlCmdScript += Environment.NewLine;
            sqlCmdScript += $"WHILE @@FETCH_STATUS = 0" + Environment.NewLine;
            sqlCmdScript += $"BEGIN" + Environment.NewLine;
            sqlCmdScript += $"    PRINT '[INFO]: Re-enabling Constraints [' + @{dataScriptTypeString}DataTableSchema + '].[' + @{dataScriptTypeString}DataTableName + ']'" + Environment.NewLine;
            sqlCmdScript += $"    SET @{dataScriptTypeString}DataCommand += N'ALTER TABLE [' + @{dataScriptTypeString}DataTableSchema + N'].[' + @{dataScriptTypeString}DataTableName + N'] WITH CHECK CHECK CONSTRAINT ALL; '" + Environment.NewLine;
            sqlCmdScript += $"    FETCH NEXT FROM {dataScriptTypeString}DataScriptsCursor" + Environment.NewLine;
            sqlCmdScript += $"    INTO @{dataScriptTypeString}DataTableSchema, @{dataScriptTypeString}DataTableName" + Environment.NewLine;
            sqlCmdScript += $"END" + Environment.NewLine;
            sqlCmdScript += $"CLOSE {dataScriptTypeString}DataScriptsCursor" + Environment.NewLine;
            sqlCmdScript += $"DEALLOCATE {dataScriptTypeString}DataScriptsCursor" + Environment.NewLine;
            sqlCmdScript += Environment.NewLine;
            sqlCmdScript += $"EXEC sp_executesql @{dataScriptTypeString}DataCommand" + Environment.NewLine;
            sqlCmdScript += Environment.NewLine;
            sqlCmdScript += $"SET NOCOUNT OFF" + Environment.NewLine;

            return sqlCmdScript;
        }

        #endregion

        #region Write Scripts

        public static void WriteScriptToFile(string filePath, string script, bool overwriteExisting)
        {
            FileInfo scriptFileInfo = new FileInfo(filePath);
            if (!scriptFileInfo.Directory.Exists)
            {
                throw new System.Exception($"Directory does not exist: {scriptFileInfo.Directory.FullName}.");
            }
            if (scriptFileInfo.Exists && !overwriteExisting)
            {
                throw new System.Exception($"Script already exists: {scriptFileInfo.Name}. Enable overwrite existing to overwrite the existing script.");
            }
            File.WriteAllText(scriptFileInfo.FullName, script);
        }

        public static void WriteScriptToFile(string fileName, string script, string scriptsDirectory, bool overwriteExisting)
        {
            SqlDataScriptHelper.WriteScriptToFile(Path.Combine(scriptsDirectory, fileName), script, overwriteExisting);
        }

        public static void WriteScriptsToFiles(Dictionary<string, string> scripts, string scriptsDirectory, bool overwriteExisting)
        {
            if (Directory.Exists(scriptsDirectory))
            {
                foreach (string tableName in scripts.Keys)
                {
                    SqlDataScriptHelper.WriteScriptToFile(tableName, scripts[tableName], scriptsDirectory, overwriteExisting);
                }
            }
        }

        #endregion

        #region Helper Methods

        public static string DataRowValuesToString(DataRowView dataRowView, List<string> columns, bool includeSpaceBetweenFields)
        {
            string rowValuesString = string.Empty;
            rowValuesString += $"(";
            // Iterate through row fields
            for (int i = 0; i < columns.Count; i++)
            {
                rowValuesString += TypedObjectToSqlValueString(dataRowView[columns[i]]);
                if (i != (columns.Count - 1))
                {
                    rowValuesString += "," + (includeSpaceBetweenFields ? " " : string.Empty);
                }
            }
            rowValuesString += ")";

            return rowValuesString;
        }

        #endregion

        public static DataTable NewDataTableFromInsertScript(string insertScriptPath, string dataTableXml)
        {
            DataTable dataTable = DataParser.NewDataTableFromXml(dataTableXml);

            FileInfo insertScriptFileInfo = new FileInfo(insertScriptPath);
            if (insertScriptFileInfo.Exists)
            {
                string insertScript = File.ReadAllText(insertScriptFileInfo.FullName);
                // Break up insert script into parts
                Match regexMatch = Regex.Match(insertScript, @"INSERT INTO \[(?<TableSchema>[^\]\[]{1,})\].\[(?<TableName>[^\]\[]{1,})\]\s*\((?<ColumnNames>[^\)\(]{1,})\)\s*VALUES\s*(?<Rows>\((?s:.*)\))");
                if (regexMatch.Success)
                {
                    string tableSchema = string.Empty;
                    string tableName = string.Empty;
                    string[] columnNames = new string[0];
                    string rows = string.Empty;

                    Group matchSearch = regexMatch.Groups["TableSchema"];
                    if (matchSearch.Success)
                    {
                        tableSchema = matchSearch.Value;
                    }
                    matchSearch = regexMatch.Groups["TableName"];
                    if (matchSearch.Success)
                    {
                        tableName = matchSearch.Value;
                    }
                    matchSearch = regexMatch.Groups["ColumnNames"];
                    MatchCollection regexMatches;
                    if (matchSearch.Success)
                    {
                        regexMatches = Regex.Matches(matchSearch.Value, @"\[(?<ColumnName>[^\]\[]+)\]");
                        columnNames = new string[regexMatches.Count];
                        for (int i = 0; i < regexMatches.Count; i++)
                        {
                            if (regexMatches[i].Success)
                            {
                                columnNames[i] = regexMatches[i].Groups["ColumnName"].Value;
                            }
                        }
                    }
                    matchSearch = regexMatch.Groups["Rows"];
                    if (matchSearch.Success)
                    {
                        rows = matchSearch.Value;
                    }

                    regexMatches = Regex.Matches(rows, @"\((?<RowValues>.*)\),?");
                    foreach (Match match in regexMatches)
                    {
                        if (match.Success)
                        {
                            matchSearch = match.Groups["RowValues"];
                            if (matchSearch.Success)
                            {
                                DataRow newRow = dataTable.NewRow();
                                string[] rowValues = new string[columnNames.Count()];
                                int columnIndex = 0;
                                foreach (TSQLToken tSqlToken in TSQLTokenizer.ParseTokens(matchSearch.Value))
                                {
                                    if (!(tSqlToken is TSQLCharacter && tSqlToken.Text == ","))
                                    {
                                        newRow[columnNames[columnIndex]] = SqlValueStringToTypedObject(
                                            tSqlToken.Text,
                                            dataTable.Columns[columnNames[columnIndex]].DataType);
                                        columnIndex++;
                                    }
                                }
                                dataTable.Rows.Add(newRow);
                            }
                        }
                    }
                }
            }

            return dataTable;
        }

        public static string TypedObjectToSqlValueString(object value)
        {
            string stringValue = string.Empty;
            switch (value)
            {
                case DateTime v:
                    {
                        stringValue = $"'{v.ToString("yyyy-MM-ddTHH:mm:ss.fff")}'";
                        break;
                    }
                case Boolean v:
                    {
                        stringValue = v ? "1" : "0";
                        break;
                    }
                case DBNull v:
                    {
                        stringValue = "NULL";
                        break;
                    }
                case String v:
                    {
                        // Make double single-quotes single
                        v = v.Replace("''", "'");
                        // Double up on single quotes
                        stringValue = $"N'{v.Replace("'", "''")}'";
                        break;
                    }
                case int v:
                    {
                        stringValue = $"{v}";
                        break;
                    }
                case byte v:
                    {
                        stringValue = $"{v}";
                        break;
                    }
                default:
                    {
                        stringValue = $"'{value}'";
                        break;
                    }
            }

            return stringValue;
        }

        public static object SqlValueStringToTypedObject(string value, Type convertToType)
        {
            object typedValue = null;
            // Remove single quotes if necessary
            if (value[0] == '\'' && value.EndsWith("'"))
            {
                value = value.Substring(1, value.Length - 2);
            }
            // Check for null
            if (value.ToUpper() == "NULL")
            {
                typedValue = DBNull.Value;
            }
            else
            {
                switch (convertToType.ToString())
                {
                    case "System.Boolean":
                        {
                            if (value == "1")
                            {
                                typedValue = true;
                            }
                            else if (value == "0")
                            {
                                typedValue = false;

                            }
                            else
                            {
                                throw new Exception($"Cannot convert value {value}. Expecting {convertToType.ToString()}.");
                            }
                            break;
                        }
                    case "System.String":
                        {
                            typedValue = Convert.ChangeType(value, convertToType).ToString().Replace("''", "'");
                            break;
                        }
                    default:
                        {
                            typedValue = Convert.ChangeType(value, convertToType);
                            break;
                        }
                }
            }

            return typedValue;
        }
    }
}