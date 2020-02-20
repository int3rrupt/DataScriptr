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
    public class DataParser
    {
        #region Foreign Key

        public static List<DataRow> GetListOfForeignKeyConstraints(string tableSchema, string tableName, DataTable foreignKeyConstraintsTable, bool recurse, List<DataRow> processedForeignKeyConstraints = null)
        {
            List<DataRow> foreignKeysConstraints = new List<DataRow>();
            // Get filtered list
            List<DataRow> foreignKeyConstraintsFiltered = foreignKeyConstraintsTable.AsEnumerable().Where(
                                                            row =>
                                                                (row["TableSchema"].ToString().ToUpper() == tableSchema.ToUpper()) &&
                                                                (row["TableName"].ToString().ToUpper() == tableName.ToUpper())
                                                            ).ToList();
            // Iterate through keys
            foreach (DataRow foreignKeyConstraintRow in foreignKeyConstraintsFiltered)
            {
                // Check if current foreign key already processed
                if (!(processedForeignKeyConstraints?.Contains(foreignKeyConstraintRow) ?? false))
                {
                    foreignKeysConstraints.Add(foreignKeyConstraintRow);
                    // Process all parent foreign keys recursively
                    if (recurse)
                    {
                        if (processedForeignKeyConstraints == null)
                        {
                            processedForeignKeyConstraints = new List<DataRow>();
                        }
                        processedForeignKeyConstraints.Add(foreignKeyConstraintRow);
                        List<DataRow> result = GetListOfForeignKeyConstraints(foreignKeyConstraintRow["ReferencedTableSchema"].ToString(), foreignKeyConstraintRow["ReferencedTableName"].ToString(), foreignKeyConstraintsTable, recurse, processedForeignKeyConstraints);
                        if (result.Count > 0)
                        {
                            foreignKeysConstraints.AddRange(result);
                        }
                    }
                }
            }

            return foreignKeysConstraints;
        }

        public static List<TableDependency> GetParentDependenciesList(string tableSchema, string tableName, List<ForeignKeyConstraint> foreignKeyConstraints, bool recurse, List<TableDependency> processedDependencies = null)
        {
            List<TableDependency> tableDependencies = new List<TableDependency>();
            // Get filtered list
            List<ForeignKeyConstraint> foreignKeyConstraintsFiltered = foreignKeyConstraints.Where(
                fkc =>
                    fkc.TableSchema.ToUpper() == tableSchema.ToUpper() &&
                    fkc.TableName.ToUpper() == tableName.ToUpper()).ToList();
            foreach (IGrouping<string, ForeignKeyConstraint> foreignKeyGroup in foreignKeyConstraintsFiltered.GroupBy(fkc => fkc.ForeignKeyName))
            {
                TableDependency tableDependency = new TableDependency(foreignKeyGroup.ToList());
                // Check if current foreign key already processed
                if (!(processedDependencies?.Contains(tableDependency) ?? false))
                {
                    tableDependencies.Add(tableDependency);
                    // Process all dependencies recursively
                    if (recurse)
                    {
                        if (processedDependencies == null)
                        {
                            processedDependencies = new List<TableDependency>();
                        }
                        processedDependencies.Add(tableDependency);
                        List<TableDependency> result = GetParentDependenciesList(tableDependency.ParentTableName.Schema, tableDependency.ParentTableName.Name, foreignKeyConstraints, recurse, processedDependencies);
                        if (result.Count > 0)
                        {
                            tableDependencies.AddRange(result);
                        }
                    }
                }
            }
            //// Iterate through keys
            //foreach (ForeignKeyConstraint foreignKeyConstraint in foreignKeyConstraintsFiltered)
            //{
            //    // Check if current foreign key already processed
            //    if (!(processedForeignKeyConstraints?.Contains(foreignKeyConstraint) ?? false))
            //    {
            //        tableDependencies.Add(new TableDependency(foreignKeyConstraint));
            //        // Process all parent foreign keys recursively
            //        if (recurse)
            //        {
            //            if (processedForeignKeyConstraints == null)
            //            {
            //                processedForeignKeyConstraints = new List<ForeignKeyConstraint>();
            //            }
            //            processedForeignKeyConstraints.Add(foreignKeyConstraint);
            //            List<TableDependency> result = GetParentDependenciesList(foreignKeyConstraint.ReferencedTableSchema, foreignKeyConstraint.ReferencedTableName, foreignKeyConstraints, recurse, processedForeignKeyConstraints);
            //            if (result.Count > 0)
            //            {
            //                tableDependencies.AddRange(result);
            //            }
            //        }
            //    }
            //}

            return tableDependencies;
        }

        public static List<TableDependency> GetParentDependenciesList(TableName tableName, List<ForeignKeyConstraint> foreignKeyConstraints, bool recurse, List<TableDependency> processedDependencies = null)
        {
            return GetParentDependenciesList(tableName.Schema, tableName.Name, foreignKeyConstraints, recurse, processedDependencies);
        }
        //ORIGINAL WORKING
        //public static List<TableDependency> GetParentDependenciesList(string tableSchema, string tableName, List<ForeignKeyConstraint> foreignKeyConstraints, bool recurse, List<ForeignKeyConstraint> processedForeignKeyConstraints = null)
        //{
        //    List<TableDependency> tableDependencies = new List<TableDependency>();
        //    // Get filtered list
        //    List<ForeignKeyConstraint> foreignKeyConstraintsFiltered = foreignKeyConstraints.Where(
        //        fkc =>
        //            fkc.TableSchema.ToUpper() == tableSchema.ToUpper() &&
        //            fkc.TableName.ToUpper() == tableName.ToUpper()).ToList();
        //    // Iterate through keys
        //    foreach (ForeignKeyConstraint foreignKeyConstraint in foreignKeyConstraintsFiltered)
        //    {
        //        // Check if current foreign key already processed
        //        if (!(processedForeignKeyConstraints?.Contains(foreignKeyConstraint) ?? false))
        //        {
        //            tableDependencies.Add(new TableDependency(foreignKeyConstraint));
        //            // Process all parent foreign keys recursively
        //            if (recurse)
        //            {
        //                if (processedForeignKeyConstraints == null)
        //                {
        //                    processedForeignKeyConstraints = new List<ForeignKeyConstraint>();
        //                }
        //                processedForeignKeyConstraints.Add(foreignKeyConstraint);
        //                List<TableDependency> result = GetParentDependenciesList(foreignKeyConstraint.ReferencedTableSchema, foreignKeyConstraint.ReferencedTableName, foreignKeyConstraints, recurse, processedForeignKeyConstraints);
        //                if (result.Count > 0)
        //                {
        //                    tableDependencies.AddRange(result);
        //                }
        //            }
        //        }
        //    }

        //    return tableDependencies;
        //}

        public static List<TableDependency> GetChildDependenciesList(
            string tableSchema,
            string tableName,
            List<ForeignKeyConstraint> foreignKeyConstraints,
            bool recurse,
            List<TableDependency> processedDependencies = null)
        {
            List<TableDependency> tableDependencies = new List<TableDependency>();
            // Get filtered list
            List<ForeignKeyConstraint> foreignKeyConstraintsFiltered = foreignKeyConstraints.Where(
                fkc =>
                    fkc.ReferencedTableSchema.ToUpper() == tableSchema.ToUpper() &&
                    fkc.ReferencedTableName.ToUpper() == tableName.ToUpper()).ToList();
            foreach (IGrouping<string, ForeignKeyConstraint> foreignKeyGroup in foreignKeyConstraintsFiltered.GroupBy(fkc => fkc.ForeignKeyName))
            {
                TableDependency tableDependency = new TableDependency(foreignKeyGroup.ToList());
                // Check if current foreign key already processed
                if (!(processedDependencies?.Contains(tableDependency) ?? false))
                {
                    tableDependencies.Add(tableDependency);
                    // Process all dependencies recursively
                    if (recurse)
                    {
                        if (processedDependencies == null)
                        {
                            processedDependencies = new List<TableDependency>();
                        }
                        processedDependencies.Add(tableDependency);
                        List<TableDependency> result = GetChildDependenciesList(tableDependency.ChildTableName.Schema, tableDependency.ChildTableName.Name, foreignKeyConstraints, recurse, processedDependencies);
                        if (result.Count > 0)
                        {
                            tableDependencies.AddRange(result);
                        }
                    }
                }
            }

            return tableDependencies;
        }

        public static List<TableDependency> GetChildDependenciesList(
            TableName tableName,
            List<ForeignKeyConstraint> foreignKeyConstraints,
            bool recurse,
            List<TableDependency> processedDependencies = null)
        {
            return GetChildDependenciesList(tableName.Schema, tableName.Name, foreignKeyConstraints, recurse, processedDependencies);
        }

        #endregion

        #region Dependencies

        public static void RemoveUnwantedDependencies(List<TableDependency> originalList, List<TableDependency> excludeList)
        {
            foreach (TableDependency tableDependency in excludeList)
            {
                originalList.Remove(tableDependency);
            }
        }

        #endregion

        public static List<string> GetColumnValues(DataTable dataTable, string columnName, bool excludeNullValues, bool distinct)
        {
            List<string> values = dataTable.AsEnumerable().Select(row => row[columnName].ToString()).ToList();
            // Remove null values
            if (excludeNullValues)
            {
                values = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
            }
            if (distinct)
            {
                values = values.Distinct().ToList();
            }

            return values;
        }

        public static Dictionary<string, List<string>> GetColumnValues(DataTable dataTable, List<string> columnNames, bool excludeNullValues, bool distinct)
        {
            Dictionary<string, List<string>> valuesCollection = new Dictionary<string, List<string>>();
            foreach (string columnName in columnNames)
            {
                List<string> values = dataTable.AsEnumerable().Select(row => row[columnName].ToString()).ToList();
                // Remove null values
                if (excludeNullValues)
                {
                    values = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
                }
                if (distinct)
                {
                    values = values.Distinct().ToList();
                }
                valuesCollection.Add(columnName, values);
            }

            return valuesCollection;
        }

        public static Dictionary<string, List<string>> GetColumnValues(
            DataTable dataTable,
            ColumnDependencyCollection columnDependencies,
            TableDependencyType sourceColumnDependencyType,
            TableDependencyType outputColumnDependencyType,
            bool excludeNullValues,
            bool distinct)
        {
            Dictionary<string, List<string>> valuesCollection = new Dictionary<string, List<string>>();
            foreach (ColumnDependency columnDependency in columnDependencies)
            {
                List<string> values = dataTable.AsEnumerable().Select(row => row[columnDependency.GetColumnNameByDependency(sourceColumnDependencyType)].ToString()).ToList();
                // Remove null values
                if (excludeNullValues)
                {
                    values = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
                }
                if (distinct)
                {
                    values = values.Distinct().ToList();
                }
                if (values.Count > 0)
                {
                    valuesCollection.Add(columnDependency.GetColumnNameByDependency(outputColumnDependencyType), values);
                }
            }

            return valuesCollection;
        }

        public static Dictionary<string, List<string>> GetColumnValues(
            DataTable dataTable,
            List<ColumnDependency> columnDependencies,
            TableDependencyType sourceColumnDependencyType,
            TableDependencyType outputColumnDependencyType,
            bool excludeNullValues,
            bool distinct)
        {
            Dictionary<string, List<string>> valuesCollection = new Dictionary<string, List<string>>();
            foreach (ColumnDependency columnDependency in columnDependencies)
            {
                List<string> values = dataTable.AsEnumerable().Select(row => row[columnDependency.GetColumnNameByDependency(sourceColumnDependencyType)].ToString()).ToList();
                // Remove null values
                if (excludeNullValues)
                {
                    values = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToList();
                }
                if (distinct)
                {
                    values = values.Distinct().ToList();
                }
                if (values.Count > 0)
                {
                    valuesCollection.Add(columnDependency.GetColumnNameByDependency(outputColumnDependencyType), values);
                }
            }

            return valuesCollection;
        }

        public static void SetDataTablePrimaryKey(List<string> primaryKeyColumnNameList, DataTable dataTable)
        {
            List<DataColumn> primaryKeyColumnList = new List<DataColumn>();
            foreach (string primaryKeyColumnName in primaryKeyColumnNameList)
            {
                primaryKeyColumnList.Add(dataTable.Columns[primaryKeyColumnName]);
            }
            dataTable.PrimaryKey = primaryKeyColumnList.ToArray();
        }

        public static DataSet NewDataSetFromXml(string dataSetxml)
        {
            DataSet newDataSet = new DataSet();
            using (MemoryStream memoryStream = NewMemoryStreamFromXml(dataSetxml))
            {
                newDataSet.ReadXml(memoryStream);
            }

            return newDataSet;
        }

        public static DataTable NewDataTableFromXml(string dataTableXml)
        {
            DataTable dataTable = new DataTable();
            using (MemoryStream memoryStream = NewMemoryStreamFromXml(dataTableXml))
            {
                dataTable.ReadXml(memoryStream);
            }

            return dataTable;
        }

        public static MemoryStream NewMemoryStreamFromXml(string xml)
        {
            MemoryStream memoryStream = new MemoryStream();
            StreamWriter streamWriter = new StreamWriter(memoryStream);
            streamWriter.Write(xml);
            streamWriter.Flush();
            memoryStream.Position = 0;

            return memoryStream;
        }

        public static string GetXmlSchemaFromDataTable(DataTable dataTable)
        {
            string xml = string.Empty;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                dataTable.WriteXmlSchema(memoryStream, false);
                memoryStream.Position = 0;
                using (StreamReader streamReader = new StreamReader(memoryStream))
                {
                    xml = streamReader.ReadToEnd();
                }
            }
            xml = $"<Root>{xml}</Root>";

            return xml;
        }
    }
}