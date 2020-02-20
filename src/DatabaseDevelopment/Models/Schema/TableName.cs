namespace DatabaseDevelopment.Models.Schema
{
    public struct TableName
    {
        public string Schema { get; }
        public string Name { get; }
        public string FullName => $"{Schema}.{Name}";
        public string FullNameDelimited => $"[{Schema}].[{Name}]";

        public TableName(string schema, string name)
        {
            Schema = schema.ToUpper();
            Name = name.ToUpper();
        }
    }
}