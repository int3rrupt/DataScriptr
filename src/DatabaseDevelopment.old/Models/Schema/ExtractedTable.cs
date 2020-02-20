using System.Collections.Generic;
using System.Data;

namespace DatabaseDevelopment.Models.Schema
{
    public class ExtractedTable
    {
        public TableName TableName { get; set; }
        public List<string> PrimaryKeyColumnNames { get; set; }
        public DataTable DataTable { get; set; }
    }
}