namespace Nexus.Core.Exceptions
{
    /// <summary>
    /// Base exception class for all business domain-specific exceptions within the Nexus Trading Engine.
    /// </summary>
    public class DomainException : Exception
    {
        public DomainException() { }

        public DomainException(string message) : base(message) { }

        public DomainException(string message, Exception innerException) : base(message, innerException) { }
    }
}
