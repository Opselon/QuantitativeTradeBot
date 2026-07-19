namespace Nexus.Core.Exceptions
{
    /// <summary>
    /// Thrown when an invalid percentage value or calculation violates domain constraints.
    /// </summary>
    public sealed class InvalidPercentageException : DomainException
    {
        public InvalidPercentageException() { }

        public InvalidPercentageException(string message) : base(message) { }

        public InvalidPercentageException(string message, Exception innerException) : base(message, innerException) { }
    }
}
