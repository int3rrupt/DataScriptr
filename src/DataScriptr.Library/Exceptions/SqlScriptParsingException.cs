using System;

namespace DataScriptr.Library.Exceptions
{
    public class SqlScriptParsingException : Exception
    {
        public SqlScriptParsingException(string message)
            : base(message)
        {
        }
    }
}