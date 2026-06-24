using Microsoft.Extensions.Logging;

namespace resultsComparer;

public static class Logger
{
    public static ILoggerFactory ConfigureLogger(LogLevel logLevel)
    {
#if DEBUG
        logLevel = logLevel > LogLevel.Debug ? LogLevel.Debug : logLevel;
#endif

        return LoggerFactory.Create(builder =>
        {
            builder
                .AddSimpleConsole(static c => c.IncludeScopes = true)
#if DEBUG
                .AddDebug()
#endif
                .SetMinimumLevel(logLevel);
        });
    }
}