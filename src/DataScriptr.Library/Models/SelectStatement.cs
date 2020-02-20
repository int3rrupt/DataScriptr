using DataScriptr.Library.Models.Schema;
using System.Collections.Generic;
using TSQL.Statements;

namespace DataScriptr.Library.Models
{
    public class SelectStatement
    {
        public string SelectText { get; private set; }
        public string FromText { get; private set; }
        public TableName PrimaryTableName { get; set; }
        public string PrimaryTableDatabase { get; private set; }
        //public string PrimaryTableSchema { get; private set; }
        //public string PrimaryTableName { get; private set; }
        //public string PrimaryTableFullName => (string.IsNullOrWhiteSpace(PrimaryTableDatabase) ? "" : $"{PrimaryTableDatabase}.") + $"{PrimaryTableSchema}.{PrimaryTableName}";
        //public string PrimaryTableFullNameDelimited => (string.IsNullOrWhiteSpace(PrimaryTableDatabase) ? "" : $"[{PrimaryTableDatabase}].") + $"[{PrimaryTableSchema}].[{PrimaryTableName}]";
        public TSQLSelectStatement TSQLSelectStatement { get; private set; }
        public List<string> ErrorMessages { get; private set; }

        public SelectStatement(
            string selectText,
            string fromText,
            string primaryTableDatabase,
            string primaryTableSchema,
            string primaryTableName,
            TSQLSelectStatement tSQLSelectStatement,
            List<string> errorMessages)
        {
            SelectText = selectText;
            FromText = fromText;
            PrimaryTableName = new TableName(primaryTableSchema, primaryTableName);
            PrimaryTableDatabase = primaryTableDatabase;
            //PrimaryTableSchema = primaryTableSchema;
            //PrimaryTableName = primaryTableName;
            TSQLSelectStatement = tSQLSelectStatement;
            ErrorMessages = errorMessages;
        }
    }
}