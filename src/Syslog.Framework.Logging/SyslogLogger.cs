using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Text;

namespace Syslog.Framework.Logging
{
    public class SyslogLogger : ILogger
    {
        private readonly string _name;
        private readonly string _host;
        private readonly LogLevel _lvl;
        private readonly UdpClient _udp;

        enum FacilityType
        {
            Kern, User, Mail, Daemon, Auth, Syslog, LPR, News, UUCP, Cron, AuthPriv, FTP,
            NTP, Audit, Audit2, CRON2, Local0, Local1, Local2, Local3, Local4, Local5, Local6, Local7
        };

        enum SeverityType { Emergency, Alert, Critical, Error, Warning, Notice, Informational, Debug };

        public SyslogLogger(string name, UdpClient udp, string host, LogLevel lvl)
        {
            _name = name;
            _udp = udp;
            _host = host;
            _lvl = lvl;
        }

        public IDisposable BeginScopeImpl(object state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None && logLevel >= _lvl;
        }

        public void Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            SeverityType severity = SeverityType.Debug;

            if (IsEnabled(logLevel))
            {
                switch (logLevel)
                {
                    case LogLevel.Information:
                        severity = SeverityType.Informational;
                        break;
                    case LogLevel.Warning:
                        severity = SeverityType.Warning;
                        break;
                    case LogLevel.Error:
                        severity = SeverityType.Error;
                        break;
                    case LogLevel.Critical:
                        severity = SeverityType.Critical;
                        break;
                }
            }
            else
                return;

            string message;

            if (formatter == null)
                message = LogFormatter.Formatter(state, exception);
            else
                message = formatter(state, exception);

            if (!String.IsNullOrEmpty(message))
            {
                int priority = ((int)FacilityType.Local0 * 8) + (int)severity; // (Facility * 8) + Severity = Priority
                string msg = String.Format("<{0}>{1:MMM dd HH:mm:ss} {2} {3}", priority, DateTime.Now, _host, _name + ": " + message); // RFC 3164 header format

                byte[] raw = Encoding.ASCII.GetBytes(msg);
                _udp.SendAsync(raw, Math.Min(raw.Length, 1024));
            }
        }
    }
}
