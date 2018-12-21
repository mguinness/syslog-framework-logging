# Syslog.Framework.Logging
Syslog provider for [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging), the logging subsystem used by ASP.NET Core.

This package routes ASP.NET log messages through Syslog, so you can capture these in any Syslog server like [Syslog Server](https://github.com/mguinness/syslogserver).

The Syslog implementation supports the following features:

 - RFC 3164
 - RFC 5424 (v1)
 - Structure data (RFC 5424 only)
 - UTC or local time stamps
 - Custom facility types

### Instructions

Install the Syslog.Framework.Logging [NuGet package](https://www.nuget.org/packages/Syslog.Framework.Logging).

Next in appsettings.json add the following and modify where necessary:

    {
      "SyslogSettings": {
        "ServerHost": "127.0.0.1",
        "ServerPort": 514,
        "HeaderType": "Rfc5424v1",
        "FacilityType": "Local0",
        "UseUtc": true,
        "StructuredData": [ 
          {
            "Id": "mydata",
            "Elements": [
              { "Name": "tag", "Value": "MyTag" }
            ]
          }
        ]
      }
    }

Then in your web application's `Main` method, configure Syslog:

```csharp
public class Program
{
  public static void Main(string[] args)
  {
    CreateWebHostBuilder(args)
      .ConfigureLogging((ctx, logging) =>
      {
        var slConfig = ctx.Configuration.GetSection("SyslogSettings");
        if (slConfig != null)
        {
          var settings = new SyslogLoggerSettings();
          slConfig.Bind(settings);
          // Configure structured data here if desired.
          logging.AddSyslog(settings);
        }
      }
  }
}
```

If you have a console app, you can use the logging provider directly. This is what Microsoft uses to instantiate `ILogger` instances. It is possible to setup Microsoft dependency injection, but that is outside the scope of this article.

```csharp
using Microsoft.Extensions.Logging;
using Syslog.Framework.Logging;
using System;

namespace MySyslogConsoleApp
{
  class Program
  {
    public readonly static SyslogLoggerProvider mLoggingProvider;
    public readonly static ILogger mLogger;

    static Program()
    {
      var settings = new SyslogLoggerSettings();
      // Set the Syslog setting here.
      mLoggingProvider = new SyslogLoggerProvider(settings, Environment.MachineName, Microsoft.Extensions.Logging.LogLevel.Debug);
      mLogger = mLoggingProvider.CreateLogger<Program>();
    }

    static void Main(string[] args)
    {
      mLogger.LogInformation("Hello World");
      var awe = new MyAwesomeClass();
      awe.DoSomethingAmazing();
      Console.WriteLine("Hello World!");
    }
  }

  public class MyAwesomeClass
  {
    private static readonly ILogger mLogger = Program.mLoggingProvider.CreateLogger<MyAwesomeClass>();
    
    public void DoSomethingAmazing()
    {
      mLogger.LogCritical("I just did something awe inspiring!");
    }
  }
}
```
### Release notes
#### 2.1.2
Made changes to a RFC 5424 message sent by the library to fix some deviations from the RFC 5424 standard:
* If any message header has no value a NILVALUE (-) is now written as the header value instead of just skipping the header. This fixes a bug where a part of message could be interpreted as structured data and the message malformed when it started with square braces followed by a colon and the message had no structured data assigned.
* The timestamp is now written with 6 digits in a second fraction part as is mandated by the standard. Previously 7 digits have been written causing some syslog server implementations (such as rsyslog) to incorrectly display that part.
* If we fail to retrieve the process id a NILVALUE is sent instead of 0.

Aside from fixing the aforementioned bugs the changes should be transparent to any RFC 5424 compliant syslog server implementation.
