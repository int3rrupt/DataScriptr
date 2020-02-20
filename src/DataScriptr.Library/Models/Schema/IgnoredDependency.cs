using DataScriptr.Library.Enums;

namespace DataScriptr.Library.Models.Schema
{
    public class IgnoredDependency
    {
        public string DependencyName { get; set; }
        public string ParentTableSchema { get; set; }
        public string ParentTableName { get; set; }
        public string ParentColumnName { get; set; }
        public string ChildTableSchema { get; set; }
        public string ChildTableName { get; set; }
        public string ChildColumnName { get; set; }
        public TableDependencyType TableDependencyType { get; set; }
    }
}