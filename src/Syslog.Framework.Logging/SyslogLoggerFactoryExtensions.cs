using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Syslog.Framework.Logging
{
    public static class SyslogLoggerFactoryExtensions
    {
        public static void AddSyslog(this ILoggerFactory loggerFactory, IConfiguration configuration, string hostName = "localhost", LogLevel logLevel = LogLevel.Verbose)
        {
            var settings = configuration.Get<SyslogLoggerSettings>();
            loggerFactory.AddProvider(new SyslogLoggerProvider(settings.ServerHost, settings.ServerPort, hostName, logLevel));
        }
    }
}
