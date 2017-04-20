# Syslog.Framework.Logging
Syslog provider for [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging), the logging subsystem used by ASP.NET Core.

This package routes ASP.NET log messages through Syslog, so you can capture these in any Syslog server like [Syslog Server](https://github.com/mguinness/syslogserver).

### Instructions

Install the Syslog.Framework.Logging [NuGet package](https://www.nuget.org/packages/Syslog.Framework.Logging).

Next in appsettings.json add the following and modify where necessary:

    {
      "SyslogSettings": {
        "ServerHost": "127.0.0.1",
        "ServerPort": 514
      }
    }

Then in your application's `Configure` method in Startup, configure Syslog:

```csharp
public class Startup
{
  public Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
  {
    if (env.IsDevelopment())
    {
      var slConfig = Configuration.GetSection("SyslogSettings");
      if (slConfig != null)
        loggerFactory.AddSyslog(slConfig, Configuration.GetValue<string>("COMPUTERNAME", "localhost"));
    }
  }
}  
```