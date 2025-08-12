using System;
using System.Collections.Generic;
using CCXT.Collector.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace CCXT.Collector.Service
{
    /// <summary>
    /// Command types for structured logging
    /// </summary>
    public enum LogCommandType
    {
        /// <summary>
        /// Quote/Market data related
        /// </summary>
        Quote,
        
        /// <summary>
        /// Order/Trading related
        /// </summary>
        Order,
        
        /// <summary>
        /// Exception/Error diagnostic
        /// </summary>
        Exception,
        
        /// <summary>
        /// Custom/General purpose
        /// </summary>
        Custom,
        
        /// <summary>
        /// Debug information
        /// </summary>
        Debug,
        
        /// <summary>
        /// Performance metrics
        /// </summary>
        Performance,
        
        /// <summary>
        /// Connection/Network related
        /// </summary>
        Connection
    }

    /// <summary>
    /// Delegate for logger events carrying lightweight structured fields.
    /// </summary>
    /// <param name="sender">Originating object.</param>
    /// <param name="e">Event payload with command/exchange/message.</param>
    public delegate void LogEventHandler(object sender, CCEventArgs e);

    /// <summary>
    /// Event args for collector logging conveying command type, exchange name and message.
    /// </summary>
    public class CCEventArgs : EventArgs
    {
        /// <summary>
        /// Command type indicating log category (PascalCase per .NET conventions)
        /// </summary>
        public LogCommandType CommandType { get; set; }
        
        /// <summary>
        /// Legacy command string for backward compatibility
        /// </summary>
        [Obsolete("Use CommandType enum instead")]
        public string Command { get; set; }

        /// <summary>
        /// Exchange name associated with the log entry (PascalCase per .NET conventions)
        /// </summary>
        public string Exchange { get; set; }

        /// <summary>
        /// Human-readable message text (PascalCase per .NET conventions)
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Structured data for context (channel, symbol, etc.)
        /// </summary>
        public Dictionary<string, object> Properties { get; set; }
        
        /// <summary>
        /// Log level for integration with ILogger
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Information;
        
        /// <summary>
        /// Optional exception for error logging
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Timestamp of the log event
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Modern logger implementation with Microsoft.Extensions.Logging integration and backward compatibility
    /// </summary>
    public class CCLogger
    {
        private readonly string _exchangeName;
        private readonly ILogger<CCLogger> _logger;
        private readonly ILoggerFactory _loggerFactory;
        
        // Legacy event support with thread-safe invocation
        public static event LogEventHandler LogEvent;

        /// <summary>
        /// Initializes a logger bound to a specific exchange name using ILoggerFactory
        /// </summary>
        /// <param name="exchange">Exchange identifier</param>
        /// <param name="loggerFactory">Optional logger factory for structured logging</param>
        public CCLogger(string exchange, ILoggerFactory loggerFactory = null)
        {
            _exchangeName = exchange ?? throw new ArgumentNullException(nameof(exchange));
            _loggerFactory = loggerFactory;
            
            if (_loggerFactory != null)
            {
                _logger = _loggerFactory.CreateLogger<CCLogger>();
            }
        }

        /// <summary>
        /// Thread-safe write method with structured logging support
        /// </summary>
        /// <param name="commandType">Type of log command</param>
        /// <param name="message">Log message</param>
        /// <param name="sender">Optional sender object</param>
        /// <param name="properties">Optional structured properties</param>
        /// <param name="exception">Optional exception for error logs</param>
        /// <param name="logLevel">Log level (defaults to Information)</param>
        public void Write(
            LogCommandType commandType, 
            string message, 
            object sender = null,
            Dictionary<string, object> properties = null,
            Exception exception = null,
            LogLevel logLevel = LogLevel.Information)
        {
            // Create event args
            var args = new CCEventArgs
            {
                CommandType = commandType,
                Command = GetLegacyCommand(commandType), // For backward compatibility
                Exchange = _exchangeName,
                Message = message,
                Properties = properties ?? new Dictionary<string, object>(),
                Exception = exception,
                LogLevel = logLevel,
                Timestamp = DateTimeOffset.UtcNow
            };

            // Use Microsoft.Extensions.Logging if available
            if (_logger != null)
            {
                // Build scope properties
                var scopeProperties = new Dictionary<string, object>
                {
                    ["Exchange"] = _exchangeName,
                    ["CommandType"] = commandType.ToString(),
                    ["Sender"] = sender?.GetType().Name ?? "Unknown"
                };
                
                // Add custom properties if provided
                if (properties != null)
                {
                    foreach (var prop in properties)
                    {
                        scopeProperties[prop.Key] = prop.Value;
                    }
                }

                using (var scope = _logger.BeginScope(scopeProperties))
                {
                    // Log with appropriate level
                    switch (logLevel)
                    {
                        case LogLevel.Trace:
                            _logger.LogTrace(exception, message);
                            break;
                        case LogLevel.Debug:
                            _logger.LogDebug(exception, message);
                            break;
                        case LogLevel.Information:
                            _logger.LogInformation(exception, message);
                            break;
                        case LogLevel.Warning:
                            _logger.LogWarning(exception, message);
                            break;
                        case LogLevel.Error:
                            _logger.LogError(exception, message);
                            break;
                        case LogLevel.Critical:
                            _logger.LogCritical(exception, message);
                            break;
                    }
                }
            }

            // Thread-safe event invocation for legacy support
            var handler = LogEvent;
            handler?.Invoke(sender ?? this, args);
        }

        /// <summary>
        /// Writes a quote-related log message with structured data
        /// </summary>
        public void WriteQuote(string message, string symbol = null, decimal? price = null, decimal? volume = null)
        {
            var properties = new Dictionary<string, object>();
            if (symbol != null) properties["Symbol"] = symbol;
            if (price.HasValue) properties["Price"] = price.Value;
            if (volume.HasValue) properties["Volume"] = volume.Value;
            
            Write(LogCommandType.Quote, message, properties: properties);
        }

        /// <summary>
        /// Writes an order-related log message with structured data
        /// </summary>
        public void WriteOrder(string message, string orderId = null, string symbol = null, string side = null, decimal? quantity = null)
        {
            var properties = new Dictionary<string, object>();
            if (orderId != null) properties["OrderId"] = orderId;
            if (symbol != null) properties["Symbol"] = symbol;
            if (side != null) properties["Side"] = side;
            if (quantity.HasValue) properties["Quantity"] = quantity.Value;
            
            Write(LogCommandType.Order, message, properties: properties);
        }

        /// <summary>
        /// Writes an exception log with full stack trace
        /// </summary>
        public void WriteException(string message, Exception exception)
        {
            Write(LogCommandType.Exception, message, exception: exception, logLevel: LogLevel.Error);
        }

        /// <summary>
        /// Writes a debug message
        /// </summary>
        public void WriteDebug(string message, Dictionary<string, object> properties = null)
        {
            Write(LogCommandType.Debug, message, properties: properties, logLevel: LogLevel.Debug);
        }

        /// <summary>
        /// Writes performance metrics
        /// </summary>
        public void WritePerformance(string operation, double elapsedMs, Dictionary<string, object> additionalMetrics = null)
        {
            var properties = new Dictionary<string, object>
            {
                ["Operation"] = operation,
                ["ElapsedMs"] = elapsedMs
            };
            
            if (additionalMetrics != null)
            {
                foreach (var metric in additionalMetrics)
                {
                    properties[metric.Key] = metric.Value;
                }
            }
            
            Write(LogCommandType.Performance, $"{operation} completed in {elapsedMs:F2}ms", properties: properties);
        }

        /// <summary>
        /// Writes connection status changes
        /// </summary>
        public void WriteConnection(string message, bool isConnected, string endpoint = null)
        {
            var properties = new Dictionary<string, object>
            {
                ["IsConnected"] = isConnected,
            };
            
            if (endpoint != null) properties["Endpoint"] = endpoint;
            
            var logLevel = isConnected ? LogLevel.Information : LogLevel.Warning;
            Write(LogCommandType.Connection, message, properties: properties, logLevel: logLevel);
        }

        #region Legacy Support Methods

        /// <summary>
        /// Legacy quote logging (WQ) - use WriteQuote instead
        /// </summary>
        [Obsolete("Use WriteQuote for structured logging")]
        public void WriteQ(object sender, string message)
        {
            Write(LogCommandType.Quote, message, sender);
        }

        /// <summary>
        /// Legacy order logging (WO) - use WriteOrder instead
        /// </summary>
        [Obsolete("Use WriteOrder for structured logging")]
        public void WriteO(object sender, string message)
        {
            Write(LogCommandType.Order, message, sender);
        }

        /// <summary>
        /// Legacy exception logging (WX) - use WriteException instead
        /// </summary>
        [Obsolete("Use WriteException for structured logging")]
        public void WriteX(object sender, string message)
        {
            Write(LogCommandType.Exception, message, sender, logLevel: LogLevel.Error);
        }

        /// <summary>
        /// Legacy custom logging (WC) - use Write instead
        /// </summary>
        [Obsolete("Use Write for structured logging")]
        public void WriteC(object sender, string message)
        {
            Write(LogCommandType.Custom, message, sender);
        }

        #endregion

        /// <summary>
        /// Maps command type to legacy command string
        /// </summary>
        private static string GetLegacyCommand(LogCommandType commandType)
        {
            return commandType switch
            {
                LogCommandType.Quote => "WQ",
                LogCommandType.Order => "WO",
                LogCommandType.Exception => "WX",
                LogCommandType.Custom => "WC",
                LogCommandType.Debug => "WD",
                LogCommandType.Performance => "WP",
                LogCommandType.Connection => "WN",
                _ => "WC"
            };
        }

        /// <summary>
        /// Creates a scoped logger for a specific operation
        /// </summary>
        public IDisposable BeginScope(string operation, Dictionary<string, object> properties = null)
        {
            if (_logger == null)
                return new NoOpDisposable();

            var scopeProperties = new Dictionary<string, object>
            {
                ["Operation"] = operation,
                ["Exchange"] = _exchangeName
            };

            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    scopeProperties[prop.Key] = prop.Value;
                }
            }

            return _logger.BeginScope(scopeProperties);
        }

        /// <summary>
        /// No-op disposable for when logger is not configured
        /// </summary>
        private class NoOpDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }

    /// <summary>
    /// Extension methods for convenient logging
    /// </summary>
    public static class CCLoggerExtensions
    {
        /// <summary>
        /// Logs an error from a WebSocket client
        /// </summary>
        public static void RaiseError(this IWebSocketClient client, string message)
        {
            var logger = new CCLogger(client.ExchangeName);
            logger.WriteException(message, null);
        }

        /// <summary>
        /// Logs performance metrics for an operation
        /// </summary>
        public static IDisposable MeasurePerformance(this CCLogger logger, string operation)
        {
            return new PerformanceMeasure(logger, operation);
        }

        private class PerformanceMeasure : IDisposable
        {
            private readonly CCLogger _logger;
            private readonly string _operation;
            private readonly DateTime _startTime;

            public PerformanceMeasure(CCLogger logger, string operation)
            {
                _logger = logger;
                _operation = operation;
                _startTime = DateTime.UtcNow;
            }

            public void Dispose()
            {
                var elapsed = (DateTime.UtcNow - _startTime).TotalMilliseconds;
                _logger.WritePerformance(_operation, elapsed);
            }
        }
    }
}