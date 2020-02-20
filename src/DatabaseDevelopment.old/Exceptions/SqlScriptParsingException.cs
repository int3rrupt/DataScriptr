using System;

namespace DatabaseDevelopment.Exceptions
{
    public class SqlScriptParsingException : Exception
    {
        public SqlScriptParsingException(string message)
            : base(message)
        {
        }
    }
}