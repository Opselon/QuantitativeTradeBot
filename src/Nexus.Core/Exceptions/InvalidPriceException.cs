namespace Nexus.Core.Exceptions
{
    /// <summary>
    /// Thrown when an invalid price or pricing operation violates domain constraints.
    /// </summary>
    public sealed class InvalidPriceException : DomainException
    {
        public InvalidPriceException() { }

        public InvalidPriceException(string message) : base(message) { }

        public InvalidPriceException(string message, Exception innerException) : base(message, innerException) { }
    }
}
