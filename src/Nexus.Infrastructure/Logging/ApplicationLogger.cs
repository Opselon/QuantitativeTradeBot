using System;
using Microsoft.Extensions.Logging;
using Nexus.Application.Ports;

namespace Nexus.Infrastructure.Logging
{
    /// <summary>
    /// Adapter implementation of <see cref="IApplicationLogger"/> wrapping the standard
    /// Microsoft.Extensions.Logging framework. This allows easy integration with Serilog,
    /// NLog, Console or dynamic UI loggers.
    /// </summary>
    public class ApplicationLogger : IApplicationLogger
    {
        private readonly ILogger<ApplicationLogger> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationLogger"/> class.
        /// </summary>
        /// <param name="logger">The underlying .NET logger factory instance.</param>
        public ApplicationLogger(ILogger<ApplicationLogger> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public void LogInformation(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
        }

        /// <inheritdoc />
        public void LogWarning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }

        /// <inheritdoc />
        public void LogError(Exception? exception, string message, params object[] args)
        {
            _logger.LogError(exception, message, args);
        }

        /// <inheritdoc />
        public void LogDebug(string message, params object[] args)
        {
            _logger.LogDebug(message, args);
        }
    }
}
