namespace Syslog.Framework.Logging.TransportProtocols
{
	/// <summary>
	/// An interface used for sending serialized messages to a Syslog server.
	/// </summary>
	public interface IMessageSender
	{
		void SendMessageToServer(byte[] messageData);
	}
}