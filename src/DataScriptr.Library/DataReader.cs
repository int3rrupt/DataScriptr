using AutoMapper;
using DataScriptr.Library.Enums;
using DataScriptr.Library.Models;
using DataScriptr.Library.Models.Mappings;
using DataScriptr.Library.Models.Schema;
using DataScriptr.Library.Databases;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ForeignKeyConstraint = DataScriptr.Library.Models.Schema.ForeignKeyConstraint;

namespace DataScriptr.Library
{
    public class DataReader
    {
        private ISqlDatabase _database;

        public DataReader(ISqlDatabase database)
        {
            this._database = database;
        }

        #region Primary Key

        public Dictionary<string, List<string>> GetPrimaryKeyValues(string server, string database, SelectStatement selectStatement)
        {
            DataTable primaryKeyConstraintsTable = this._database.GetPrimaryKeyConstraintsFromDatabase();
            DataSet queryResultDS = this.ExecuteQueryUsingForXml(server, database, selectStatement.SelectText);
            return GetPrimaryKeyValues(selectStatement.PrimaryTableName, queryResultDS.Tables[0], primaryKeyConstraintsTable);
        }

        public Dictionary<string, List<string>> GetPrimaryKeyValues(string primaryTableSchema, string primaryTableName, DataTable dataTable, DataTable primaryKeyConstraintsTable)
        {
            Dictionary<string, List<string>> primaryKeysAndValues = new Dictionary<string, List<string>>();

            // Get primary keys
            foreach (DataRow primaryKeyConstraintRow in GetListOfPrimaryKeyConstraints(primaryTableSchema, primaryTableName, primaryKeyConstraintsTable))
            {
                string primaryKeyName = primaryKeyConstraintRow["ColumnName"].ToString();
                primaryKeysAndValues.Add(primaryKeyName, dataTable.AsEnumerable().Select(row => row[primaryKeyName].ToString()).ToList());
            }

            return primaryKeysAndValues;
        }

        public Dictionary<string, List<string>> GetPrimaryKeyValues(TableName primaryTableName, DataTable dataTable, DataTable primaryKeyConstraintsTable)
        {
            return GetPrimaryKeyValues(primaryTableName.Schema, primaryTableName.Name, dataTable, primaryKeyConstraintsTable);
        }

        public List<DataRow> GetListOfPrimaryKeyConstraints(string tableSchema, string tableName, DataTable primaryKeyConstraintsTable)
        {
            return primaryKeyConstraintsTable.AsEnumerable()
                .Where(row =>
                    (row["ConstraintType"].ToString().ToUpper() == "PRIMARY KEY") &&
                    (row["TableSchema"].ToString().ToUpper() == tableSchema.ToUpper()) &&
                    (row["TableName"].ToString().ToUpper() == tableName.ToUpper())).ToList();
        }

        public List<DataRow> GetListOfPrimaryKeyConstraints(TableName tableName, DataTable primaryKeyConstraintsTable)
        {
            return GetListOfPrimaryKeyConstraints(tableName.Schema, tableName.Name, primaryKeyConstraintsTable);
        }

        #endregion

        #region Constraints Tables

        public DataTable GetPrimaryKeyConstraintsFromDatabase()
        {
            return this._database.GetPrimaryKeyConstraintsFromDatabase();
        }

        public DataTable GetForeignKeyConstraintsTableFromDatabase(bool includeTableMaintenanceTables = false)
        {
            return this._database.GetForeignKeyConstraintsTableFromDatabase(includeTableMaintenanceTables);
        }

        public List<ForeignKeyConstraint> GetForeignKeyConstraintsFromDatabase(bool includeTableMaintenanceTables = false)
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile(new ForeignKeyConstraintProfile()));
            var mapper = config.CreateMapper();
            //Mapper.Initialize(cfg => cfg.AddProfile(new ForeignKeyConstraintProfile()));
            DataTable fkDataTable = this._database.GetForeignKeyConstraintsTableFromDatabase(includeTableMaintenanceTables);
            List<DataRow> rows = new List<DataRow>(fkDataTable.Rows.OfType<DataRow>());
            List<ForeignKeyConstraint> foreignKeyConstraints = mapper.Map<List<DataRow>, List<ForeignKeyConstraint>>(rows);

            return foreignKeyConstraints;
        }

        #endregion

        #region Soft Constraints

        public DataTable GetSoftConstraintsTableFromDatabase(string server, string database)
        {
            string constraintsQuery = $@"
                    select
                         [ForeignKeyName]
                        ,[TableSchema]
                        ,[TableName]
                        ,[ConstraintColumnName]
                        ,[ReferencedTableSchema]
                        ,[ReferencedTableName]
                        ,[ReferencedColumnName]
                        ,[IsDisabled]
                    from
                        [Scripting].[SoftDependency]";
            constraintsQuery += @"
                    order by [TableSchema], [TableName]";

            DataSet resultsDataSet = this._database.ExecuteQuery(constraintsQuery);
            DataTable dataTable;
            if (resultsDataSet.Tables.Count > 0)
            {
                dataTable = resultsDataSet.Tables[0];
            }
            else
            {
                // TODO add logging
                dataTable = new DataTable();
            }

            return dataTable;
        }

        public List<ForeignKeyConstraint> GetSoftConstraintsFromDatabase(string server, string database)
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile(new ForeignKeyConstraintProfile()));
            var mapper = config.CreateMapper();
            DataTable dataTable = GetSoftConstraintsTableFromDatabase(server, database);
            List<DataRow> rows = new List<DataRow>(dataTable.Rows.OfType<DataRow>());

            //return Mapper.Map<List<DataRow>, List<ForeignKeyConstraint>>(rows);
            return mapper.Map<List<DataRow>, List<ForeignKeyConstraint>>(rows);
        }

        #endregion

        #region Ignored Dependencies

        public IgnoredDependencyLists GetIgnoredDependenciesFromDatabase(string server, string database)
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile(new IgnoredDependencyProfile()));
            var mapper = config.CreateMapper();
            IgnoredDependencyLists ignoredDependenciesCollection = new IgnoredDependencyLists();
            string constraintsQuery = $@"
                    select
                        [DependencyName]
                       ,[ParentTableSchema]
                       ,[ParentTableName]
                       ,[ParentColumnName]
                       ,[ChildTableSchema]
                       ,[ChildTableName]
                       ,[ChildColumnName]
                       ,[TableDependencyTypeId]
                    from
                        [Scripting].[IgnoredDependency]";

            DataSet resultsDataSet = this._database.ExecuteQuery(constraintsQuery);
            if (resultsDataSet.Tables.Count > 0)
            {
                DataTable dataTable = resultsDataSet.Tables[0];
                List<DataRow> rows = new List<DataRow>(dataTable.Rows.OfType<DataRow>());
                List<IgnoredDependency> ignoredDependencies = mapper.Map<List<DataRow>, List<IgnoredDependency>>(rows);
                foreach (var group in ignoredDependencies.GroupBy(id => id.DependencyName))
                {
                    List<IgnoredDependency> ignoredDependenciesGroup = group.ToList();
                    TableDependency tableDependency = new TableDependency(ignoredDependenciesGroup);
                    switch (ignoredDependenciesGroup[0].TableDependencyType)
                    {
                        case TableDependencyType.ParentDependency:
                            {
                                ignoredDependenciesCollection.ParentDependencies.Add(tableDependency);
                                break;
                            }
                        case TableDependencyType.ChildDependency:
                            {
                                ignoredDependenciesCollection.ChildDependencies.Add(tableDependency);
                                break;
                            }
                    }
                }
            }
            else
            {
                // todo write to log
            }

            return ignoredDependenciesCollection;
        }

        #endregion

        public DataTable RetrieveTableWhereKeysInValues(string serverName, string databaseName, TableName tableName, Dictionary<string, List<string>> keysAndValues, List<string> primaryKeyColumnNameList)
        {
            DataTable dataTable = new DataTable();
            if (keysAndValues.Count > 0)
            {
                string tableQuery = SqlStatementParser.NewSelectWhereKeysInValues(tableName.FullNameDelimited, keysAndValues);
                //Write-Host "Retrieving parent table $parentTableNameDelimited"
                //Write-Host "Where $(foreignKeyConstraint.ReferencedColumnName) in ('$foreignKeyValues')" - ForegroundColor DarkGray
                // Retrieve parent table using foreign key values
                dataTable = ExecuteQueryUsingForXml(serverName, databaseName, tableQuery).Tables[tableName.FullName];
                // Set primary keys on parent DataTable
                DataParser.SetDataTablePrimaryKey(primaryKeyColumnNameList, dataTable);
                dataTable.AcceptChanges();
            }
            else
            {
                //Write-Host "Table $parentTableNameDelimited not retrieved. $childTableNameDelimited.[$(foreignKeyConstraint.ConstraintColumnName)] values are all DBNull";
            }

            return dataTable;
        }

        public DataSet ExecuteQuery(string query)
        {
            return this._database.ExecuteQuery(query);
        }

        public DataSet ExecuteQueryUsingForXml(string server, string database, string query)
        {
            DataSet resultsDataSet = new DataSet();
            // Wrap in XML
            string newQuery = $@"DECLARE @xmlData XML
                                 SET @xmlData = ({query} FOR XML AUTO, ELEMENTS, XMLSCHEMA('PowerShellDataExportXsdSchema'), ROOT('Root'))
                                 SELECT @xmlData AS XmlResult";
            // Execute select statement
            DataSet intermediateDataSet = this._database.ExecuteQuery(newQuery);
            if (intermediateDataSet.Tables.Count == 1 && intermediateDataSet.Tables[0].Rows.Count == 1)
            {
                resultsDataSet = DataParser.NewDataSetFromXml(intermediateDataSet.Tables[0].Rows[0]["XmlResult"].ToString());
            }

            return resultsDataSet;
        }
    }
}