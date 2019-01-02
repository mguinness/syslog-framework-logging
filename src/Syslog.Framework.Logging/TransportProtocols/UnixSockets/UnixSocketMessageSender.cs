using System.Net.Sockets;

namespace Syslog.Framework.Logging.TransportProtocols.UnixSockets
{
	/// <summary>
	/// Sends message to a syslog server using local UNIX sockets.
	/// </summary>
	/// <remarks>
	/// Note that depending on syslog server UNIX sockets can expect different message format than other protocols.
	/// This is the case on rsyslog by default. If you want to use UNIX socket logging using this library you should
	/// set UseSpecialParser="off" either for default /dev/log socket or add new socket with this special parser disabled.
	/// Example configuration for /dev/log in rsyslog.conf:
	/// <example>
	///    module(load="imuxsock" SysSock.UseSpecialParser="off")
	/// </example>
	/// </remarks>
	public class UnixSocketMessageSender : IMessageSender
	{
		private readonly string _socketPath;

		public UnixSocketMessageSender(string socketPath)
		{
			_socketPath = socketPath;
		}

		public void SendMessageToServer(byte[] messageData)
		{
			using(var socket = new Socket(AddressFamily.Unix, SocketType.Dgram, ProtocolType.IP))
			{
				var endpoint = new UnixDomainSocketEndPoint(_socketPath);
				socket.Connect(endpoint);

				socket.Send(messageData);
			}
		}
	}
}