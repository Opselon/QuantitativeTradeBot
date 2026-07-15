using System;

namespace Nexus.Application.Ports
{
    /// <summary>
    /// Port interface defining the core logging capabilities across the platform.
    /// Protects high-level components from direct dependency on concrete logging libraries.
    /// </summary>
    public interface IApplicationLogger
    {
        /// <summary>
        /// Logs a message at the Information level.
        /// </summary>
        /// <param name="message">The structured log message template.</param>
        /// <param name="args">Message formatting arguments.</param>
        void LogInformation(string message, params object[] args);

        /// <summary>
        /// Logs a message at the Warning level.
        /// </summary>
        /// <param name="message">The structured log message template.</param>
        /// <param name="args">Message formatting arguments.</param>
        void LogWarning(string message, params object[] args);

        /// <summary>
        /// Logs a message and exception at the Error level.
        /// </summary>
        /// <param name="exception">The exception being caught or reported.</param>
        /// <param name="message">The structured log message template.</param>
        /// <param name="args">Message formatting arguments.</param>
        void LogError(Exception? exception, string message, params object[] args);

        /// <summary>
        /// Logs a message at the Debug level.
        /// </summary>
        /// <param name="message">The structured log message template.</param>
        /// <param name="args">Message formatting arguments.</param>
        void LogDebug(string message, params object[] args);
    }
}
