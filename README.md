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
