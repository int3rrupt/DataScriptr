using System.Collections.Generic;

namespace DatabaseDevelopment.Models.Schema
{
    public class IgnoredDependencyLists
    {
        public List<TableDependency> ParentDependencies { get; set; }
        public List<TableDependency> ChildDependencies { get; set; }

        public IgnoredDependencyLists()
        {
            ParentDependencies = new List<TableDependency>();
            ChildDependencies = new List<TableDependency>();
        }
    }
}