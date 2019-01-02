using System.Net.Sockets;

namespace Syslog.Framework.Logging.TransportProtocols.Udp
{
	/// <summary>
	/// Sends message to a syslog server using UDP datagrams.
	/// </summary>
	public class UdpMessageSender : IMessageSender
	{
		private readonly string _serverHost;
		private readonly int _serverPort;

		public UdpMessageSender(string serverHost, int serverPort)
		{
			_serverHost = serverHost;
			_serverPort = serverPort;
		}

		public void SendMessageToServer(byte[] messageData)
		{
			using (var udp = new UdpClient())
			{
				udp.Send(messageData, messageData.Length, _serverHost, _serverPort);
			}
		}
	}
}