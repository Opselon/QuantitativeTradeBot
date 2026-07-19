namespace Nexus.Core.Exceptions
{
    /// <summary>
    /// Thrown when an invalid volume value or volume constraint violation occurs.
    /// </summary>
    public sealed class InvalidVolumeException : DomainException
    {
        public InvalidVolumeException() { }

        public InvalidVolumeException(string message) : base(message) { }

        public InvalidVolumeException(string message, Exception innerException) : base(message, innerException) { }
    }
}
