using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Nexus.Tests.EndToEnd.Fixture
{
    public class TestOutputLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _outputHelper;

        public TestOutputLoggerProvider(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestOutputLogger(categoryName, _outputHelper);
        }

        public void Dispose() { }
    }

    public class TestOutputLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly ITestOutputHelper _outputHelper;

        public TestOutputLogger(string categoryName, ITestOutputHelper outputHelper)
        {
            _categoryName = categoryName;
            _outputHelper = outputHelper;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            // Simple scope formatting if needed, but not strictly required for test output
            return null;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            try
            {
                var message = formatter(state, exception);
                var formatted = $"[{DateTime.UtcNow:HH:mm:ss.fff}] [{logLevel}] [{_categoryName}] {message}";
                if (exception != null)
                {
                    formatted += Environment.NewLine + exception.ToString();
                }
                _outputHelper.WriteLine(formatted);
            }
            catch
            {
                // Ignore writing errors if test helper is already disposed (e.g. async race conditions at test finish)
            }
        }
    }
}
