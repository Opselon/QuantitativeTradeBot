using System;

namespace Nexus.Core.Exceptions
{
    /// <summary>
    /// Thrown when a position operation, modification, or state transition is illegal or invalid.
    /// </summary>
    public sealed class InvalidPositionException : DomainException
    {
        public InvalidPositionException() { }

        public InvalidPositionException(string message) : base(message) { }

        public InvalidPositionException(string message, Exception innerException) : base(message, innerException) { }
    }
}
