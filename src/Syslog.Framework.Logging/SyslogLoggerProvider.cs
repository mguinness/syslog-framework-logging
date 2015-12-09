using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Syslog.Framework.Logging
{
    public class SyslogLoggerProvider : ILoggerProvider
    {
        private UdpClient _udp;
        private string _hostName;
        private LogLevel _logLevel;
        private IDictionary<string, ILogger> _loggers;

        public SyslogLoggerProvider(string serverHost, int serverPort, string hostName, LogLevel logLevel)
        {
            _udp = new UdpClient(serverHost, serverPort);
            _hostName = hostName;
            _logLevel = logLevel;
            _loggers = new Dictionary<string, ILogger>();
        }

        public ILogger CreateLogger(string name)
        {
            if (!_loggers.ContainsKey(name))
                _loggers[name] = new SyslogLogger(name, _udp, _hostName, _logLevel);

            return _loggers[name];
        }

        public void Dispose()
        {
            _loggers.Clear();
#if DNX451
            _udp.Close();
#else
            _udp.Dispose();
#endif
        }
    }
}
