using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Syslog.Framework.Logging
{
	public abstract class SyslogLogger : ILogger
	{
		private readonly string _name;
		private readonly string _host;
		private readonly LogLevel _lvl;
		private readonly SyslogLoggerSettings _settings;

		public SyslogLogger(string name, SyslogLoggerSettings settings, string host, LogLevel lvl)
		{
			_name = name;
			_settings = settings;
			_host = host;
			_lvl = lvl;
		}

		public IDisposable BeginScope<TState>(TState state)
		{
			return null;
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return logLevel != LogLevel.None && logLevel >= _lvl;
		}

		public virtual void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			if (formatter == null)
				throw new ArgumentNullException(nameof(formatter));

			if (!IsEnabled(logLevel))
                return;

			string message = formatter(state, exception);

			if (String.IsNullOrEmpty(message))
                return;

			// Defined in RFC 5424, section 6.2.1, and RFC 3164, section 4.1.1.
			// If a different value is needed, then this code should probably move into the specific loggers.
			var severity = MapToSeverityType(logLevel);
			var priority = ((int)_settings.FacilityType * 8) + (int)severity;
			var procid = GetProcID();
			var now = _settings.UseUtc ? DateTime.UtcNow : DateTime.Now;
			var msg = FormatMessage(priority, now, _host, _name, procid, eventId.Id, message);
			var raw = Encoding.ASCII.GetBytes(msg);

			using (var udp = new UdpClient())
			{
				udp.Send(raw, raw.Length, _settings.ServerHost, _settings.ServerPort);
			}
		}

		protected abstract string FormatMessage(int priority, DateTime now, string host, string name, int procid, int msgid, string message);

		private int? _procID;
		private int GetProcID()
		{
			if (_procID == null)
			{
				try
				{
					// Attempt to get the process ID. This might not work on all platforms.
					_procID = Process.GetCurrentProcess().Id;
				}
				catch
				{
					// If we can't get it, just default to 0.
					_procID = 0;
				}
			}

			return _procID.Value;
		}

		internal virtual SeverityType MapToSeverityType(LogLevel logLevel)
		{
			switch (logLevel)
			{
				case LogLevel.Information:
					return SeverityType.Informational;
				case LogLevel.Warning:
					return SeverityType.Warning;
				case LogLevel.Error:
					return SeverityType.Error;
				case LogLevel.Critical:
					return SeverityType.Critical;
				default:
					return SeverityType.Debug;
			}
		}
	}

	/// <summary>
	/// Based on RFC 3164: https://tools.ietf.org/html/rfc3164
	/// </summary>
	public class Syslog3164Logger : SyslogLogger
	{
		public Syslog3164Logger(string name, SyslogLoggerSettings settings, string host, LogLevel lvl)
			: base(name, settings, host, lvl)
		{
		}

		protected override string FormatMessage(int priority, DateTime now, string host, string name, int procid, int msgid, string message)
		{
            var tag = name.Replace(".", String.Empty).Replace("_", String.Empty); // Alphanumeric
            tag = tag.Substring(0, Math.Min(32, tag.Length)); // Max length is 32 according to spec
            return $"<{priority}>{now:MMM dd HH:mm:ss} {host} {tag} {message}";
		}
	}

	/// <summary>
	/// Based on RFC 5424: https://tools.ietf.org/html/rfc5424
	/// </summary>
	public class Syslog5424v1Logger : SyslogLogger
	{
		private readonly string _structuredData;

		public Syslog5424v1Logger(string name, SyslogLoggerSettings settings, string host, LogLevel lvl)
			: base(name, settings, host, lvl)
		{
			_structuredData = FormatStructuredData(settings);
		}

		private string FormatStructuredData(SyslogLoggerSettings settings)
		{
			if (settings.StructuredData == null)
                return null;

			if (settings.StructuredData.Count() == 0)
                return null;
			
			var sb = new StringBuilder();
			sb.Append(" "); // Need to add a space to separate what came before it.

			foreach (var data in settings.StructuredData)
			{
				if (!IsValidPrintAscii(data.Id, '=', ' ', ']', '"'))
					throw new InvalidOperationException($"ID for structured data {data.Id} is not valid. US Ascii 33-126 only, except '=', ' ', ']', '\"'");

				sb.Append($"[{data.Id}");

				if (data.Elements != null)
				{
					foreach (var element in data.Elements)
					{
						if (!IsValidPrintAscii(element.Name, '=', ' ', ']', '"'))
							throw new InvalidOperationException($"Element {element.Name} in structured data {data.Id} is not valid. US Ascii 33-126 only, except '=', ' ', ']', '\"'");

						// According to spec, need to escape these characters.
						var val = element.Value
							.Replace("\\", "\\\\")
							.Replace("\"", "\\\"")
							.Replace("]", "\\]");
						sb.Append($" {element.Name}=\"{val}\"");
					}
				}

                sb.Append("]");
			}

			return sb.ToString();
		}

		/// <summary>
		/// Based on spec, section 6.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		private bool IsValidPrintAscii(string name, params char[] invalid)
		{
			if (String.IsNullOrEmpty(name))
                return false;

			foreach (var ch in name)
			{
				if (ch < 33)
                    return false;
				if (ch > 126)
                    return false;
				if (invalid.Contains(ch))
                    return false;
			}

			return true;
		}

		protected override string FormatMessage(int priority, DateTime now, string host, string name, int procid, int msgid, string message)
		{
            var data = _structuredData ?? String.Empty;
            return $"<{priority}>1 {now:o} {host} {name} {procid} {msgid}{data} {message}";
		}
	}
}
