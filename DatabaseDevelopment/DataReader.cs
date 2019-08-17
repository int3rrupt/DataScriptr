using AutoMapper;
using DatabaseDevelopment.Enums;
using DatabaseDevelopment.Models;
using DatabaseDevelopment.Models.Mappings;
using DatabaseDevelopment.Models.Schema;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using ForeignKeyConstraint = DatabaseDevelopment.Models.Schema.ForeignKeyConstraint;

namespace DatabaseDevelopment
{
    public static class DataReader
    {
        #region Primary Key

        public static Dictionary<string, List<string>> GetPrimaryKeyValues(string server, string database, SelectStatement selectStatement)
        {
            DataTable primaryKeyConstraintsTable = GetPrimaryKeyConstraintsFromDatabase(server, database);
            DataSet queryResultDS = DataReader.ExecuteQueryUsingForXml(server, database, selectStatement.SelectText);
            return GetPrimaryKeyValues(selectStatement.PrimaryTableName, queryResultDS.Tables[0], primaryKeyConstraintsTable);
        }

        public static Dictionary<string, List<string>> GetPrimaryKeyValues(string primaryTableSchema, string primaryTableName, DataTable dataTable, DataTable primaryKeyConstraintsTable)
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

        public static Dictionary<string, List<string>> GetPrimaryKeyValues(TableName primaryTableName, DataTable dataTable, DataTable primaryKeyConstraintsTable)
        {
            return GetPrimaryKeyValues(primaryTableName.Schema, primaryTableName.Name, dataTable, primaryKeyConstraintsTable);
        }

        public static List<DataRow> GetListOfPrimaryKeyConstraints(string tableSchema, string tableName, DataTable primaryKeyConstraintsTable)
        {
            return primaryKeyConstraintsTable.AsEnumerable().Where(
                                                                    row =>
                                                                        (row["ConstraintType"].ToString().ToUpper() == "PRIMARY KEY") &&
                                                                        (row["TableSchema"].ToString().ToUpper() == tableSchema.ToUpper()) &&
                                                                        (row["TableName"].ToString().ToUpper() == tableName.ToUpper())
                                                                ).ToList();
        }

        public static List<DataRow> GetListOfPrimaryKeyConstraints(TableName tableName, DataTable primaryKeyConstraintsTable)
        {
            return GetListOfPrimaryKeyConstraints(tableName.Schema, tableName.Name, primaryKeyConstraintsTable);
        }

        #endregion

        #region Constraints Tables

        public static DataTable GetPrimaryKeyConstraintsFromDatabase(string server, string database)
        {
            List<string> constraintTypes = new List<string>
            {
                //if (includePrimaryKeyTypes)
                //{
                "'PRIMARY KEY'"
            //}
            //if (includeForeignKeyTypes)
            //{
            //    constraintTypes.Add("'FOREIGN KEY'");
            //}
            };
            string primaryKeysQuery = $@"
                    select
                        tc.CONSTRAINT_CATALOG as ConstraintCatalog,
                        tc.CONSTRAINT_SCHEMA as ConstraintSchema,
                        tc.CONSTRAINT_NAME as ConstraintName,
                        tc.TABLE_CATALOG as TableCatalog,
                        tc.TABLE_SCHEMA as TableSchema,
                        tc.TABLE_NAME as TableName,
                        tc.CONSTRAINT_TYPE as ConstraintType,
                        tc.IS_DEFERRABLE,
                        tc.INITIALLY_DEFERRED,
                        cu.COLUMN_NAME as ColumnName
                    from
                        INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                        inner join INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE cu
                            on tc.CONSTRAINT_NAME = cu.CONSTRAINT_NAME
                    where
                        tc.CONSTRAINT_TYPE in ({string.Join(",", constraintTypes)})";
            //if (!includeTableMaintenanceTables)
            //{
            //    primaryKeysQuery += @"
            //        and
            //            tc.TABLE_SCHEMA not like '%TableMaintenance'";
            //}
            primaryKeysQuery += @"
                    order by tc.CONSTRAINT_CATALOG, tc.CONSTRAINT_SCHEMA, tc.TABLE_CATALOG, tc.TABLE_SCHEMA, tc.TABLE_NAME";

            DataSet resultsDataSet = ExecuteQuery(server, database, primaryKeysQuery);
            DataTable primaryKeysTable;
            if (resultsDataSet.Tables.Count > 0)
            {
                primaryKeysTable = resultsDataSet.Tables[0];
            }
            else
            {
                primaryKeysTable = new DataTable();
            }

            return primaryKeysTable;
        }

        public static DataTable GetForeignKeyConstraintsTableFromDatabase(string server, string database, bool includeTableMaintenanceTables = false)
        {
            string foreignKeysQuery = $@"
                    select
                        f.name as ForeignKeyName,
                        f.parent_object_id as TableId,
                        OBJECT_SCHEMA_NAME(f.parent_object_id) as TableSchema,
                        OBJECT_NAME(f.parent_object_id) as TableName,
                        COL_NAME(fc.parent_object_id, fc.parent_column_id) AS ConstraintColumnName,
                        OBJECT_SCHEMA_NAME (f.referenced_object_id) AS ReferencedTableSchema,
                        OBJECT_NAME (f.referenced_object_id) AS ReferencedTableName,
                        COL_NAME(fc.referenced_object_id, fc.referenced_column_id) AS ReferencedColumnName,
                        is_disabled as IsDisabled,
                        delete_referential_action_desc,
                        update_referential_action_desc
                    from
                        sys.foreign_keys f
                        inner join sys.foreign_key_columns fc
                            on f.object_id = fc.constraint_object_id";
            if (!includeTableMaintenanceTables)
            {
                foreignKeysQuery += @"
                    where
                        OBJECT_SCHEMA_NAME (f.referenced_object_id) not like '%TableMaintenance'";
            }
            foreignKeysQuery += @"
                    order by TableSchema,TableName";

            DataSet resultsDataSet = ExecuteQuery(server, database, foreignKeysQuery);
            DataTable primaryKeysTable;
            if (resultsDataSet.Tables.Count > 0)
            {
                primaryKeysTable = resultsDataSet.Tables[0];
            }
            else
            {
                primaryKeysTable = new DataTable();
            }

            return primaryKeysTable;
        }

        public static List<ForeignKeyConstraint> GetForeignKeyConstraintsFromDatabase(string server, string database, bool includeTableMaintenanceTables = false)
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile(new ForeignKeyConstraintProfile()));
            var mapper = config.CreateMapper();
            //Mapper.Initialize(cfg => cfg.AddProfile(new ForeignKeyConstraintProfile()));
            DataTable fkDataTable = GetForeignKeyConstraintsTableFromDatabase(server, database, includeTableMaintenanceTables);
            List<DataRow> rows = new List<DataRow>(fkDataTable.Rows.OfType<DataRow>());
            List<ForeignKeyConstraint> foreignKeyConstraints = mapper.Map<List<DataRow>, List<ForeignKeyConstraint>>(rows);

            return foreignKeyConstraints;
        }

        #endregion

        #region Soft Constraints

        public static DataTable GetSoftConstraintsTableFromDatabase(string server, string database)
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

            DataSet resultsDataSet = ExecuteQuery(server, database, constraintsQuery);
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

        public static List<ForeignKeyConstraint> GetSoftConstraintsFromDatabase(string server, string database)
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

        public static IgnoredDependencyLists GetIgnoredDependenciesFromDatabase(string server, string database)
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile(new IgnoredDependencyProfile()));
            var mapper = config.CreateMapper();
            Mapper.Initialize(cfg => cfg.AddProfile(new IgnoredDependencyProfile()));
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

            DataSet resultsDataSet = ExecuteQuery(server, database, constraintsQuery);
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

        public static DataTable RetrieveTableWhereKeysInValues(string serverName, string databaseName, TableName tableName, Dictionary<string, List<string>> keysAndValues, List<string> primaryKeyColumnNameList)
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

        public static DataSet ExecuteQuery(string server, string database, string query)
        {
            DataSet resultsDataSet = new DataSet();
            using (SqlConnection sqlConnection = new SqlConnection($"Server = {server}; Database = {database}; Integrated Security = True"))
            {
                SqlDataAdapter sqlAdapter = new System.Data.SqlClient.SqlDataAdapter(query, sqlConnection);
                int rowsAffected = sqlAdapter.Fill(resultsDataSet);
            }

            return resultsDataSet;
        }

        public static DataSet ExecuteQueryUsingForXml(string server, string database, string query)
        {
            DataSet resultsDataSet = new DataSet();
            // Wrap in XML
            string newQuery = $@"DECLARE @xmlData XML
                                 SET @xmlData = ({query} FOR XML AUTO, ELEMENTS, XMLSCHEMA('PowerShellDataExportXsdSchema'), ROOT('Root'))
                                 SELECT @xmlData AS XmlResult";
            // Execute select statement
            DataSet intermediateDataSet = ExecuteQuery(server, database, newQuery);
            if (intermediateDataSet.Tables.Count == 1 && intermediateDataSet.Tables[0].Rows.Count == 1)
            {
                resultsDataSet = DataParser.NewDataSetFromXml(intermediateDataSet.Tables[0].Rows[0]["XmlResult"].ToString());
            }

            return resultsDataSet;
        }
    }
}