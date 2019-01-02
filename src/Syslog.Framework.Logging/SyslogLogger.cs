using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Syslog.Framework.Logging.TransportProtocols;
using Syslog.Framework.Logging.TransportProtocols.Udp;

namespace Syslog.Framework.Logging
{
	public abstract class SyslogLogger : ILogger
	{
		private readonly string _name;
		private readonly string _host;
		private readonly LogLevel _lvl;
		private readonly SyslogLoggerSettings _settings;
		private readonly int? _processId;
		private readonly IMessageSender _messageSender;

		[Obsolete("Remains for backward compatibility. Will be removed in future. Use the other overload.")]
		public SyslogLogger(string name, SyslogLoggerSettings settings, string host, LogLevel lvl)
			: this(name, settings, host, lvl, new UdpMessageSender(settings.ServerHost, settings.ServerPort))
		{
		}
		
		public SyslogLogger(string name, SyslogLoggerSettings settings, string host, LogLevel lvl, IMessageSender messageSender)
		{
			_name = name;
			_settings = settings;
			_host = host;
			_lvl = lvl;
			_messageSender = messageSender;
			_processId = GetProcID();
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
			var now = _settings.UseUtc ? DateTime.UtcNow : DateTime.Now;
			var msg = FormatMessage(priority, now, _host, _name, _processId, eventId.Id, message);
			var raw = Encoding.ASCII.GetBytes(msg);

			try
			{
				_messageSender.SendMessageToServer(raw);
			}
			catch (Exception ex)
			{
				// Do not rethrow exception. Crashing an application just because logging has failed due to a transient unavailability of syslog server
				// does not look like a good practice.
				Console.Error.WriteLine("Logging failed." + ex);
			}
		}

		[Obsolete("Left for backward compatibility only. Will be removed in future. Override the other method overload")]
		protected virtual string FormatMessage(int priority, DateTime now, string host, string name, int procid, int msgid, string message)
		{
			throw new NotImplementedException($"You have to provide implementation for a {nameof(FormatMessage)} method.");
		}

		protected virtual string FormatMessage(int priority, DateTime now, string host, string name, int? procid, int msgid, string message)
		{
#pragma warning disable 618
			return FormatMessage(priority, now, host, name, procid ?? 0, msgid, message);
#pragma warning restore 618
		}

		private int? GetProcID()
		{
			try
			{
				// Attempt to get the process ID. This might not work on all platforms.
				return Process.GetCurrentProcess().Id;
			}
			catch
			{
				return null;
			}
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
		[Obsolete("Remains for backward compatibility. Will be removed in future. Use the other overload.")]
		public Syslog3164Logger(string name, SyslogLoggerSettings settings, string host, LogLevel lvl)
			: base(name, settings, host, lvl)
		{
		}
		
		public Syslog3164Logger(string name, SyslogLoggerSettings settings, string host, LogLevel lvl, IMessageSender messageSender)
			: base(name, settings, host, lvl, messageSender)
		{
		}

		protected override string FormatMessage(int priority, DateTime now, string host, string name, int? procid, int msgid, string message)
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
		private const string NilValue = "-";
		private readonly string _structuredData;
		
		[Obsolete("Remains for backward compatibility. Will be removed in future. Use the other overload.")]
		public Syslog5424v1Logger(string name, SyslogLoggerSettings settings, string host, LogLevel lvl)
			: base(name, settings, host, lvl)
		{
		}

		public Syslog5424v1Logger(string name, SyslogLoggerSettings settings, string host, LogLevel lvl, IMessageSender messageSender)
			: base(name, settings, host, lvl, messageSender)
		{
			_structuredData = FormatStructuredData(settings);
		}

		private string FormatStructuredData(SyslogLoggerSettings settings)
		{
			if (settings.StructuredData == null)
                return null;

			if (!settings.StructuredData.Any())
                return null;
			
			var sb = new StringBuilder();

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

		protected override string FormatMessage(int priority, DateTime now, string host, string name, int? procid, int msgid, string message)
		{
			var formattedTimestamp = FormatTimestamp(now);
			return $"<{priority}>1 {formattedTimestamp} {host ?? NilValue} {name ?? NilValue} {procid?.ToString() ?? NilValue} {msgid} {_structuredData ?? NilValue} {message}";
		}

		/// <summary>
		/// Formats the date as required by RFC 5424.
		/// </summary>
		private string FormatTimestamp(DateTime time)
		{
			return time.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK", CultureInfo.InvariantCulture);
		}
	}
}
