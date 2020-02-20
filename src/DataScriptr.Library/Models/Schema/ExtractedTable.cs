using System.Collections.Generic;
using System.Data;

namespace DataScriptr.Library.Models.Schema
{
    public class ExtractedTable
    {
        public TableName TableName { get; set; }
        public List<string> PrimaryKeyColumnNames { get; set; }
        public DataTable DataTable { get; set; }
    }
}