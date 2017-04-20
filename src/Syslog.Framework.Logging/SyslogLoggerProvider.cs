using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Syslog.Framework.Logging
{
    public class SyslogLoggerProvider : ILoggerProvider
    {
        private SyslogLoggerSettings _settings;
        private string _hostName;
        private LogLevel _logLevel;
        private IDictionary<string, ILogger> _loggers;

        public SyslogLoggerProvider(SyslogLoggerSettings settings, string hostName, LogLevel logLevel)
        {
            _settings = settings;
            _hostName = hostName;
            _logLevel = logLevel;
            _loggers = new Dictionary<string, ILogger>();
        }

        public ILogger CreateLogger(string name)
        {
            if (!_loggers.ContainsKey(name))
                _loggers[name] = new SyslogLogger(name, _settings, _hostName, _logLevel);

            return _loggers[name];
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
