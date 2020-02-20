using System;

namespace DataScriptr.Models.Exceptions
{
    public class ValueSetException : Exception
    {
        public ValueSetException(string message)
            : base(message)
        {
        }
    }
}