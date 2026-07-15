using System;

namespace Nexus.Core.Exceptions
{
    /// <summary>
    /// Thrown when a risk check or parameter exceeds permitted thresholds or violates safety protocols.
    /// </summary>
    public sealed class InvalidRiskException : DomainException
    {
        public InvalidRiskException() { }

        public InvalidRiskException(string message) : base(message) { }

        public InvalidRiskException(string message, Exception innerException) : base(message, innerException) { }
    }
}
