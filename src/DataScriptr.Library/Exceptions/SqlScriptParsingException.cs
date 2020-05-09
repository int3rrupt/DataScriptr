using System;

namespace DataScriptr.Library.Exceptions
{
    public class SqlScriptParsingException : Exception
    {
        public string Sql { get; private set; }
        
        public SqlScriptParsingException(string message, string sql)
            : base(message)
        {
            this.Sql = sql;
        }

        public SqlScriptParsingException(string message, string sql, Exception innerException)
            : base(message, innerException)
        {
            this.Sql = sql;
        }

        public override string ToString()
        {
            return $"{base.ToString()}{Environment.NewLine}{Environment.NewLine}Sql:{Environment.NewLine}{this.Sql}";
        }
    }
}