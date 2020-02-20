using DataScriptr.Library.Enums;

namespace DataScriptr.Library.Models.Schema
{
    public struct ColumnDependency
    {
        public string ParentColumnName { get; }
        public string ChildColumnName { get; }

        public ColumnDependency(string parentColumnName, string childColumnName)
        {
            ParentColumnName = parentColumnName.ToUpper();
            ChildColumnName = childColumnName.ToUpper();
        }

        public string GetColumnNameByDependency(TableDependencyType tableDependencyType)
        {
            switch (tableDependencyType)
            {
                case TableDependencyType.ParentDependency:
                    return ParentColumnName;
                case TableDependencyType.ChildDependency:
                    return ChildColumnName;
                default:
                    throw new System.Exception("TableDependencyType not mapped to a ColumnDependency Name.");
            }
        }
    }
}