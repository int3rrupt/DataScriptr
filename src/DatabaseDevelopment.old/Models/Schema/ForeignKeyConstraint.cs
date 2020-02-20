namespace DatabaseDevelopment.Models.Schema
{
    public class ForeignKeyConstraint
    {
        public string ForeignKeyName { get; set; }
        public string TableSchema { get; set; }
        public string TableName { get; set; }
        public string ConstraintColumnName { get; set; }
        public string ReferencedTableSchema { get; set; }
        public string ReferencedTableName { get; set; }
        public string ReferencedColumnName { get; set; }
        public bool IsDisabled { get; set; }
    }
}