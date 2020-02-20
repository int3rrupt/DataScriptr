using DatabaseDevelopment.Enums;
using System.Collections.Generic;

namespace DatabaseDevelopment.Models.Schema
{
    public struct TableDependency
    {
        public TableName ParentTableName { get; }
        public string ParentColumnName { get; }
        public TableName ChildTableName { get; }
        public string ChildColumnName { get; }
        public ColumnDependencyCollection ColumnDependencies { get; }

        public TableDependency(List<ForeignKeyConstraint> foreignKeyGrouping)
        {
            if (foreignKeyGrouping.Count < 1)
            {
                throw new System.Exception("Unable to create TableDependency from empty ForeignKey Grouping list parameter.");
            }
            ColumnDependencies = new ColumnDependencyCollection();
            string foreignKeyName = foreignKeyGrouping[0].ForeignKeyName;
            string tableSchema = foreignKeyGrouping[0].TableSchema;
            string tableName = foreignKeyGrouping[0].TableName;
            string referencedTableSchema = foreignKeyGrouping[0].ReferencedTableSchema;
            string referencedTableName = foreignKeyGrouping[0].ReferencedTableName;
            foreach (ForeignKeyConstraint foreignKeyConstraint in foreignKeyGrouping)
            {
                if (foreignKeyName != foreignKeyConstraint.ForeignKeyName)
                {
                    throw new System.Exception($"Cannot create TableDependency from unrelated foreign keys. Foreign Key Name {foreignKeyConstraint.ForeignKeyName} does not match {foreignKeyName}");
                }
                if (tableSchema != foreignKeyConstraint.TableSchema)
                {
                    throw new System.Exception($"Cannot create TableDependency from unrelated foreign keys. Foreign Key Name {foreignKeyConstraint.TableSchema} does not match {tableSchema}");
                }
                if (tableName != foreignKeyConstraint.TableName)
                {
                    throw new System.Exception($"Cannot create TableDependency from unrelated foreign keys. Foreign Key Name {foreignKeyConstraint.TableName} does not match {tableName}");
                }
                if (referencedTableSchema != foreignKeyConstraint.ReferencedTableSchema)
                {
                    throw new System.Exception($"Cannot create TableDependency from unrelated foreign keys. Foreign Key Name {foreignKeyConstraint.ReferencedTableSchema} does not match {referencedTableSchema}");
                }
                if (referencedTableName != foreignKeyConstraint.ReferencedTableName)
                {
                    throw new System.Exception($"Cannot create TableDependency from unrelated foreign keys. Foreign Key Name {foreignKeyConstraint.ReferencedTableName} does not match {referencedTableName}");
                }
                ColumnDependencies.Add(new ColumnDependency(foreignKeyConstraint.ReferencedColumnName, foreignKeyConstraint.ConstraintColumnName));
            }
            ParentTableName = new TableName(referencedTableSchema, referencedTableName);
            ChildTableName = new TableName(tableSchema, tableName);
            ParentColumnName = null;
            ChildColumnName = null;
        }

        public TableDependency(List<IgnoredDependency> ignoredDependencyGrouping)
        {
            if (ignoredDependencyGrouping.Count < 1)
            {
                throw new System.Exception("Unable to create TableDependency from empty IgnoredDependency Grouping list parameter.");
            }
            ColumnDependencies = new ColumnDependencyCollection();
            string dependencyName = ignoredDependencyGrouping[0].DependencyName;
            string parentTableSchema = ignoredDependencyGrouping[0].ParentTableSchema;
            string parentTableName = ignoredDependencyGrouping[0].ParentTableName;
            string childTableSchema = ignoredDependencyGrouping[0].ChildTableSchema;
            string childTableName = ignoredDependencyGrouping[0].ChildTableName;
            TableDependencyType tableDependencyType = ignoredDependencyGrouping[0].TableDependencyType;
            foreach (IgnoredDependency ignoredDependency in ignoredDependencyGrouping)
            {
                if (dependencyName != ignoredDependency.DependencyName)
                {
                    throw new System.Exception($"Cannot create TableDependency from unrelated ignored dependencies. Dependency Name {ignoredDependency.DependencyName} does not match {dependencyName}");
                }
                if (parentTableSchema != ignoredDependency.ParentTableSchema)
                {
                    throw new System.Exception($"Cannot create TableDependency from unrelated ignored dependencies. Dependency Name {ignoredDependency.ParentTableSchema} does not match {parentTableSchema}");
                }
                if (parentTableName != ignoredDependency.ParentTableName)
                {
                    throw new System.Exception($"Cannot create TableDependency from unrelated ignored dependencies. Dependency Name {ignoredDependency.ParentTableName} does not match {parentTableName}");
                }
                if (childTableSchema != ignoredDependency.ChildTableSchema)
                {
                    throw new System.Exception($"Cannot create TableDependency from unrelated ignored dependencies. Dependency Name {ignoredDependency.ChildTableSchema} does not match {childTableSchema}");
                }
                if (childTableName != ignoredDependency.ChildTableName)
                {
                    throw new System.Exception($"Cannot create TableDependency from unrelated ignored dependencies. Dependency Name {ignoredDependency.ChildTableName} does not match {childTableName}");
                }
                if (tableDependencyType != ignoredDependency.TableDependencyType)
                {
                    throw new System.Exception($"Cannot create TableDependency from unrelated ignored dependencies. Dependency Name {ignoredDependency.TableDependencyType} does not match {tableDependencyType}");
                }
                ColumnDependencies.Add(new ColumnDependency(ignoredDependency.ParentColumnName, ignoredDependency.ChildColumnName));
            }
            ParentTableName = new TableName(parentTableSchema, parentTableName);
            ChildTableName = new TableName(childTableSchema, childTableName);
            ParentColumnName = null;
            ChildColumnName = null;
        }

        public TableDependency(TableName parentTableName, string parentColumnName, TableName childTableName, string childColumnName)
        {
            ParentTableName = parentTableName;
            ParentColumnName = parentColumnName.ToUpper();
            ChildTableName = childTableName;
            ChildColumnName = childColumnName.ToUpper();
            ColumnDependencies = null;
        }
    }
}