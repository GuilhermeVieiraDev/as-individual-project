using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace eShop.Ordering.API.Infrastructure.Telemetry;

/// <summary>
/// Log formatter that redacts sensitive information from log messages
/// </summary>
public class SensitiveDataLogFormatter : ILoggerFormatter
{
    // Common patterns for sensitive data
    private static readonly Regex _creditCardRegex = new(@"\b(?:4[0-9]{12}(?:[0-9]{3})?|5[1-5][0-9]{14}|3[47][0-9]{13}|3(?:0[0-5]|[68][0-9])[0-9]{11}|6(?:011|5[0-9]{2})[0-9]{12}|(?:2131|1800|35\d{3})\d{11})\b", RegexOptions.Compiled);
    private static readonly Regex _emailRegex = new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled);
    private static readonly Regex _ssnRegex = new(@"\b\d{3}[-]?\d{2}[-]?\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex _phoneRegex = new(@"\b(?:\+\d{1,2}\s)?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex _zipCodeRegex = new(@"\b\d{5}(?:[-\s]\d{4})?\b", RegexOptions.Compiled);

    // Key-value patterns (for structured logging)
    private static readonly Regex _keyValueRegex = new(
        @"\b(cardnumber|cardholdername|cardsecuritynumber|cvv|password|token|email|ssn|phonenumber|accountnumber)[""']?\s*[=:]\s*[""']?([^""',\}\]]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Format(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        // Redact credit card numbers
        message = _creditCardRegex.Replace(message, match =>
        {
            var card = match.Value.Replace(" ", "").Replace("-", "");
            if (card.Length >= 13)
            {
                return "XXXX-XXXX-XXXX-" + card.Substring(card.Length - 4);
            }
            return "[REDACTED-CC]";
        });

        // Redact SSNs
        message = _ssnRegex.Replace(message, "[REDACTED-SSN]");

        // Redact phone numbers
        message = _phoneRegex.Replace(message, "[REDACTED-PHONE]");

        // Partially redact emails
        message = _emailRegex.Replace(message, match =>
        {
            var parts = match.Value.Split('@');
            if (parts.Length != 2) return "[REDACTED-EMAIL]";

            string username = parts[0];
            string domain = parts[1];

            if (username.Length <= 2) return $"{username}@{domain}";
            return $"{username[0]}{'*' * (username.Length - 2)}{username[^1]}@{domain}";
        });

        // Partially redact ZIP codes
        message = _zipCodeRegex.Replace(message, match =>
        {
            var zip = match.Value.Replace(" ", "").Replace("-", "");
            if (zip.Length == 5)
                return zip[..2] + "***";
            if (zip.Length > 5)
                return zip[..2] + "***" + "-****";
            return match.Value;
        });

        // Redact key-value pairs containing sensitive information
        message = _keyValueRegex.Replace(message, match =>
        {
            string key = match.Groups[1].Value;
            return $"{key}=\"[REDACTED]\"";
        });

        return message;
    }
}

// Interface for the log formatter
public interface ILoggerFormatter
{
    string Format(string message);
}

// Logger provider that uses the formatter
public class SensitiveDataLoggerProvider : ILoggerProvider
{
    private readonly ILoggerFormatter _formatter;

    public SensitiveDataLoggerProvider(ILoggerFormatter formatter)
    {
        _formatter = formatter;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FormattedLogger(categoryName, _formatter);
    }

    public void Dispose() 
    { 
        // No resources to dispose
    }

    private class FormattedLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly ILoggerFormatter _formatter;

        public FormattedLogger(string categoryName, ILoggerFormatter formatter)
        {
            _categoryName = categoryName;
            _formatter = formatter;
        }

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            string message = formatter(state, exception);
            string formattedMessage = _formatter.Format(message);

            // Simple console logging 
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {logLevel}: {_categoryName} - {formattedMessage}");
            
            if (exception != null)
            {
                Console.WriteLine($"Exception: {exception}");
            }
        }

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new NullScope();
            public void Dispose() { }
        }
    }
}

// Extension method for easy registration
public static class SensitiveDataLogFilterExtensions
{
    public static ILoggingBuilder AddSensitiveDataFilter(this ILoggingBuilder builder)
    {
        builder.Services.AddSingleton<ILoggerFormatter, SensitiveDataLogFormatter>();
        builder.Services.AddSingleton<ILoggerProvider, SensitiveDataLoggerProvider>();
        return builder;
    }
}
