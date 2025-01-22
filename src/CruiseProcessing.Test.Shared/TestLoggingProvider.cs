using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace CruiseProcessing.Test
{
    public class TestLoggingProvider : ILoggerProvider
    {
        public TestLoggingProvider(ITestOutputHelper output, LogLevel minLogLevel)
        {
            Output = output ?? throw new ArgumentNullException(nameof(output));
            MinLogLevel = minLogLevel;
        }

        public ITestOutputHelper Output { get; set; }

        public LogLevel MinLogLevel { get; set; } = LogLevel.Debug;

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(Output, MinLogLevel, categoryName);
        }

        public void Dispose()
        {
            // nothing to dispose
        }
    }
}
