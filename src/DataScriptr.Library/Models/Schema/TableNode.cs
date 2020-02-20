using System.Collections.Generic;
using System.Linq;

namespace DataScriptr.Library.Models.Schema
{
    public class TableDependency2
    {
        public TableNode Table { get; set; }
        //public ColumnDependencyCollection ColumnDependencies { get; set; }
        public List<ColumnDependency> ColumnDependencies { get; set; }

        public TableDependency2(TableNode tableNode, List<ForeignKeyConstraint> foreignKeyGrouping)
        {
            if (foreignKeyGrouping.Count < 1)
            {
                throw new System.Exception("Unable to create TableNode from empty ForeignKey Grouping list parameter.");
            }
            ColumnDependencies = new List<ColumnDependency>();
            string foreignKeyName = foreignKeyGrouping[0].ForeignKeyName;
            string tableSchema = foreignKeyGrouping[0].TableSchema;
            string tableName = foreignKeyGrouping[0].TableName;
            string referencedTableSchema = foreignKeyGrouping[0].ReferencedTableSchema;
            string referencedTableName = foreignKeyGrouping[0].ReferencedTableName;
            foreach (ForeignKeyConstraint foreignKeyConstraint in foreignKeyGrouping)
            {
                if (foreignKeyName != foreignKeyConstraint.ForeignKeyName)
                {
                    throw new System.Exception($"Cannot create TableNode from unrelated foreign keys. Foreign Key Name {foreignKeyConstraint.ForeignKeyName} does not match {foreignKeyName}");
                }
                if (tableSchema != foreignKeyConstraint.TableSchema)
                {
                    throw new System.Exception($"Cannot create TableNode from unrelated foreign keys. Foreign Key Name {foreignKeyConstraint.TableSchema} does not match {tableSchema}");
                }
                if (tableName != foreignKeyConstraint.TableName)
                {
                    throw new System.Exception($"Cannot create TableNode from unrelated foreign keys. Foreign Key Name {foreignKeyConstraint.TableName} does not match {tableName}");
                }
                if (referencedTableSchema != foreignKeyConstraint.ReferencedTableSchema)
                {
                    throw new System.Exception($"Cannot create TableNode from unrelated foreign keys. Foreign Key Name {foreignKeyConstraint.ReferencedTableSchema} does not match {referencedTableSchema}");
                }
                if (referencedTableName != foreignKeyConstraint.ReferencedTableName)
                {
                    throw new System.Exception($"Cannot create TableNode from unrelated foreign keys. Foreign Key Name {foreignKeyConstraint.ReferencedTableName} does not match {referencedTableName}");
                }
                ColumnDependencies.Add(new ColumnDependency(foreignKeyConstraint.ReferencedColumnName, foreignKeyConstraint.ConstraintColumnName));
            }
            Table = tableNode;
        }
    }

    public class ColumnNode
    {
        public string Name { get; set; }
        public TableNode Table { get; set; }
        public ColumnNode ReferencedColumn { get; set; }
    }

    public class TableNode
    {
        public TableName Name { get; set; }
        //public List<ColumnNode> Columns { get; set; }
        public Dictionary<string, List<TableDependency2>> ParentDependencies { get; set; }
        public Dictionary<string, List<TableDependency2>> ChildDependencies { get; set; }

        //public List<TableNode> ParentNodes { get; set; }
        //public List<TableNode> ChildNodes { get; }
        //public string ParentColumnName { get; }
        //public TableName ChildTableName { get; }
        //public string ChildColumnName { get; }
        //public ColumnDependencyCollection ColumnDependencies { get; }
    }

    public class DependencyBuilder
    {
        Dictionary<string, TableNode> dependencyIndex = new Dictionary<string, TableNode>();
        List<TableNode> dependencies = new List<TableNode>();

        public Dictionary<string, TableNode> BuildDependency(List<ForeignKeyConstraint> foreignKeyConstraints)
        {
            foreach (IGrouping<string, ForeignKeyConstraint> foreignKeyGroupBy in foreignKeyConstraints.GroupBy(fkc => fkc.ForeignKeyName))
            {
                List<ForeignKeyConstraint> foreignKeyGroup = foreignKeyGroupBy.ToList();
                TableNode childTable;
                TableNode parentTable;

                if (dependencyIndex.ContainsKey($"{foreignKeyGroup[0].TableSchema}.{foreignKeyGroup[0].TableName}".ToUpper()))
                {
                    childTable = dependencyIndex[$"{foreignKeyGroup[0].TableSchema}.{foreignKeyGroup[0].TableName}".ToUpper()];
                }
                else
                {
                    childTable = new TableNode();
                    childTable.Name = new TableName(foreignKeyGroup[0].TableSchema, foreignKeyGroup[0].TableName);
                    childTable.ChildDependencies = new Dictionary<string, List<TableDependency2>>();
                    childTable.ParentDependencies = new Dictionary<string, List<TableDependency2>>();
                }
                if (dependencyIndex.ContainsKey($"{foreignKeyGroup[0].ReferencedTableSchema}.{foreignKeyGroup[0].ReferencedTableName}".ToUpper()))
                {
                    parentTable = dependencyIndex[$"{foreignKeyGroup[0].ReferencedTableSchema}.{foreignKeyGroup[0].ReferencedTableName}".ToUpper()];
                }
                else
                {
                    parentTable = new TableNode();
                    parentTable.Name = new TableName(foreignKeyGroup[0].ReferencedTableSchema, foreignKeyGroup[0].ReferencedTableName);
                    parentTable.ChildDependencies = new Dictionary<string, List<TableDependency2>>();
                    parentTable.ParentDependencies = new Dictionary<string, List<TableDependency2>>();
                }

                TableDependency2 tableDependency = new TableDependency2(parentTable, foreignKeyGroup);
                if (childTable.ParentDependencies.ContainsKey(parentTable.Name.FullName))
                {
                    childTable.ParentDependencies[parentTable.Name.FullName].Add(tableDependency);
                }
                else
                {
                    List<TableDependency2> newList = new List<TableDependency2>();
                    newList.Add(tableDependency);
                    childTable.ParentDependencies.Add(parentTable.Name.FullName, newList);
                }
                tableDependency = new TableDependency2(childTable, foreignKeyGroup);
                if (parentTable.ChildDependencies.ContainsKey(childTable.Name.FullName))
                {
                    parentTable.ChildDependencies[childTable.Name.FullName].Add(tableDependency);
                }
                else
                {
                    List<TableDependency2> newList = new List<TableDependency2>();
                    newList.Add(tableDependency);
                    parentTable.ChildDependencies.Add(childTable.Name.FullName, newList);
                }

                if (!dependencyIndex.ContainsKey(childTable.Name.FullName))
                {
                    dependencies.Add(childTable);
                    int index = dependencies.IndexOf(childTable, dependencies.Count - 1);
                    dependencyIndex.Add(childTable.Name.FullName, childTable);
                }
                if (!dependencyIndex.ContainsKey(parentTable.Name.FullName))
                {
                    dependencies.Add(parentTable);
                    int index = dependencies.IndexOf(parentTable, dependencies.Count - 1);
                    dependencyIndex.Add(parentTable.Name.FullName, parentTable);
                }
                //TableDependency tableDependency = new TableDependency(foreignKeyGroup.ToList());
                //// Check if current foreign key already processed
                //if (!(processedDependencies?.Contains(tableDependency) ?? false))
                //{
                //    tableDependencies.Add(tableDependency);
                //    // Process all dependencies recursively
                //    if (recurse)
                //    {
                //        if (processedDependencies == null)
                //        {
                //            processedDependencies = new List<TableDependency>();
                //        }
                //        processedDependencies.Add(tableDependency);
                //        List<TableDependency> result = GetParentDependenciesList(tableDependency.ParentTableName.Schema, tableDependency.ParentTableName.Name, foreignKeyConstraints, recurse, processedDependencies);
                //        if (result.Count > 0)
                //        {
                //            tableDependencies.AddRange(result);
                //        }
                //    }
                //}
            }
            return dependencyIndex;
        }
    }
}