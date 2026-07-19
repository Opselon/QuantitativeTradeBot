using Microsoft.Extensions.Logging;

namespace Nexus.Application.Observability
{
    public static class LoggingExtensions
    {
        public static IDisposable? BeginWorkflowScope(this ILogger logger, WorkflowContext context)
        {
            var scopeData = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(context.CorrelationId)) scopeData["CorrelationId"] = context.CorrelationId;
            if (!string.IsNullOrEmpty(context.OperationId)) scopeData["OperationId"] = context.OperationId;
            if (!string.IsNullOrEmpty(context.Workflow)) scopeData["Workflow"] = context.Workflow;
            if (!string.IsNullOrEmpty(context.StrategyId)) scopeData["StrategyId"] = context.StrategyId;
            if (!string.IsNullOrEmpty(context.Symbol)) scopeData["Symbol"] = context.Symbol;
            if (!string.IsNullOrEmpty(context.AccountId)) scopeData["AccountId"] = context.AccountId;
            if (!string.IsNullOrEmpty(context.OrderId)) scopeData["OrderId"] = context.OrderId;
            if (!string.IsNullOrEmpty(context.PositionId)) scopeData["PositionId"] = context.PositionId;
            if (!string.IsNullOrEmpty(context.Gateway)) scopeData["Gateway"] = context.Gateway;
            if (!string.IsNullOrEmpty(context.Subsystem)) scopeData["Subsystem"] = context.Subsystem;

            return logger.BeginScope(scopeData);
        }

        public static void LogStructured(
            this ILogger logger,
            LogLevel logLevel,
            EventId eventId,
            string messageTemplate,
            params object?[] args)
        {
            if (args == null || args.Length == 0)
            {
                logger.Log(logLevel, eventId, messageTemplate);
                return;
            }

            var sanitizedArgs = new object?[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is string s)
                {
                    sanitizedArgs[i] = LogSanitizer.Sanitize(s);
                }
                else
                {
                    sanitizedArgs[i] = args[i];
                }
            }

            logger.Log(logLevel, eventId, messageTemplate, sanitizedArgs);
        }

        public static void LogStructuredError(
            this ILogger logger,
            Exception exception,
            EventId eventId,
            string messageTemplate,
            params object?[] args)
        {
            var sanitizedArgs = new object?[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is string s)
                {
                    sanitizedArgs[i] = LogSanitizer.Sanitize(s);
                }
                else
                {
                    sanitizedArgs[i] = args[i];
                }
            }

            logger.LogError(eventId, exception, messageTemplate, sanitizedArgs);
        }
    }
}
