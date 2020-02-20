using System;

namespace Jarvis.Models.Exceptions
{
    public class ValueSetException : Exception
    {
        public ValueSetException(string message)
            : base(message)
        {
        }
    }
}