using System;
using Syslog.Framework.Logging.TransportProtocols.Udp;
using Syslog.Framework.Logging.TransportProtocols.UnixSockets;

namespace Syslog.Framework.Logging.TransportProtocols
{
	internal static class MessageSenderFactory
	{
		public static IMessageSender CreateFromSettings(SyslogLoggerSettings settings)
		{
			switch (settings.MessageTransportProtocol)
			{
				case TransportProtocol.Udp:
					return new UdpMessageSender(settings.ServerHost, settings.ServerPort);
				case TransportProtocol.UnixSocket:
					return new UnixSocketMessageSender(settings.UnixSocketPath);
				default:
					throw new InvalidOperationException($"{nameof(TransportProtocol)} '{settings.MessageTransportProtocol}' is not recognized.");
			}
		}
	}
}