using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DataScriptr.Library.Databases
{
    public class SqlDatabase : ISqlDatabase
    {
        private readonly string _connectionString;
        public SqlDatabase(string connectionString)
        {
            this._connectionString = connectionString;
        }

        public DataSet ExecuteQuery(string query)
        {
            DataSet resultsDataSet = new DataSet();
            using (SqlConnection sqlConnection = new SqlConnection(this._connectionString))
            {
                using (SqlDataAdapter sqlAdapter = new SqlDataAdapter(query, sqlConnection))
                {
                    int rowsAffected = sqlAdapter.Fill(resultsDataSet);
                }
            }

            return resultsDataSet;
        }

        public DataTable GetPrimaryKeyConstraintsFromDatabase()
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

            DataSet resultsDataSet = this.ExecuteQuery(primaryKeysQuery);
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

        public DataTable GetForeignKeyConstraintsTableFromDatabase(bool includeTableMaintenanceTables = false)
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

            DataSet resultsDataSet = this.ExecuteQuery(foreignKeysQuery);
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
    }
}