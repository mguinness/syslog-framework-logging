# Syslog.Framework.Logging
Syslog provider for [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging), the logging subsystem used by ASP.NET Core.

This package routes ASP.NET log messages through Syslog, so you can capture these in any Syslog server like [Syslog Server](https://github.com/mguinness/syslogserver).

The Syslog implementation supports the following features:

 - RFC 3164
 - RFC 5424 (v1)
 - Structured data (RFC 5424 only)
 - UTC or local time stamps
 - UDP and Unix sockets as message transport protocols,
 - Can be extended with custom message transport procotol,
 - Custom facility types

### Instructions

Install the Syslog.Framework.Logging [NuGet package](https://www.nuget.org/packages/Syslog.Framework.Logging).

Next in appsettings.json add the following and modify where necessary:

    {
      "SyslogSettings": {
        "MessageTransportProtocol": "Udp",
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
### Message transport protocols
A message transport protocol defines means by which the message is delivered to a syslog server.
This library currently has the following built-in transport protocols:
#### UDP
A fast but unreliable protocol. When using UDP be aware that messages can be lost or they can be delivered out of order. Allows sending of messages directly to a remote server.

To use this transport protocol configure your syslog server to accept UDP messages and change your config to include the following lines (change the server host and port according to your server configuration):
```
"SyslogSettings": {
    "MessageTransportProtocol": "Udp",
    "ServerHost": "127.0.0.1",
    "ServerPort": 514
}
```
#### Unix domain sockets
A fast and reliable protocol for sending messages to a local syslog server on Unix systems. Does NOT support sending of messages to a remote server (but note that most syslog server implementations can be configured to forward the messages to a centralized server, also with advanced features such as queueing the messages if the server is unavailable). 

Note that by default most syslog servers (at least rsyslog is) are configured to expect special message format for default `/dev/log` socket. This message format is inflexible and is not supported by this library. If you want to use this transport protocol you have to configure the server to listen to additional socket in standard format or reconfigure the default socket. 

In rsyslog you can reconfigure the /dev/log socket to use the same format as for messages sent by Udp or Tcp by replacing the following entry in `/etc/rsyslog.conf`:

`module(load="imuxsock") # provides support for local system logging`

with

`module(load="imuxsock" SysSock.UseSpecialParser="off") # provides support for local system logging`

Note that during testing we did not encounter any side effects when disabling special message format for `/dev/log` but there is a possibility that logs coming from some applications using that format can be malformed so depending on your requirements the usage of an additional socket in different path can be better solution.

To use this transport protocol change your config to include the following lines (change the socket path if it is different than /dev/log):
```
"SyslogSettings": {
    "MessageTransportProtocol": "UnixSocket",
    "UnixSocketPath": "/dev/log"
}
```
#### Adding custom protocol
If built-in protocols aren't enough you can add support for any protocol by implementing just 1 simple interface - `IMessageSender`.

For example, to use Tcp protocol you could write this simple class (note - this is just an example designed for simplicity, NOT a production ready implementation):
```
public class TcpMessageSender : IMessageSender
{
   public void SendMessageToServer(byte[] messageData)
   {
      using (var tcp = new TcpClient())
      {
         tcp.Connect("127.0.0.1", 514);
				
         using (var stream = tcp.GetStream())
         {
            stream.Write(messageData, 0, messageData.Length);
            stream.WriteByte((byte)'\n');
         }
      }
   }
}
```
And then tell the library to use this protocol implementation by assigning an object of this class to a `CustomMessageSender` property of `SyslogLoggerSettings`:
```
    CreateWebHostBuilder(args)
    .ConfigureLogging((ctx, logging) =>
    {
        var slConfig = ctx.Configuration.GetSection("SyslogSettings");
        
        logging.AddSyslog(slConfig, x =>
        {
            x.CustomMessageSender = new TcpMessageSender();
        });
    }
```
If you have written a generic implementation of protocol not currently supported by the library and you think it can be useful to more people you are encouraged to contribute the implementation to the project.

### Release notes
#### 2.1.2
Made changes to a RFC 5424 message sent by the library to fix some deviations from the RFC 5424 standard:
* If any message header has no value a NILVALUE (-) is now written as the header value instead of just skipping the header. This fixes a bug where a part of message could be interpreted as structured data and the message malformed when it started with square braces followed by a colon and the message had no structured data assigned.
* The timestamp is now written with 6 digits in a second fraction part as is mandated by the standard. Previously 7 digits have been written causing some syslog server implementations (such as rsyslog) to incorrectly display that part.
* If we fail to retrieve the process id a NILVALUE is sent instead of 0.

Aside from fixing the aforementioned bugs the changes should be transparent to any RFC 5424 compliant syslog server implementation.

#### 2.2.0
* Support for sending messages by UNIX domain sockets.
* Support for custom transport procotol implementations.
