using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Text;
using NReco.Logging.File;

namespace Nixill.Streaming.JoltBot;

public static class Log
{
  public static readonly ILoggerFactory Factory = LoggerFactory.Create(builder => builder.AddFile("logs/{0:yyyy}-{0:MM}-{0:dd}.log",
      (FileLoggerOptions opts) =>
      {
        opts.Append = true;
        opts.FormatLogFileName = name => string.Format(name, DateTime.UtcNow);
        opts.MinLevel = LogLevel.Trace;
      }));
}
