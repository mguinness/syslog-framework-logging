namespace Syslog.Framework.Logging.TransportProtocols
{
	/// <summary>
	/// Available built-in transport protocols used for sending logs to a syslog server.
	/// </summary>
	public enum TransportProtocol
	{
		/// <summary>
		/// Sends the logs using UDP datagrams.
		/// </summary>
		Udp,
		
		/// <summary>
		/// Sends the logs using local UNIX sockets.
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
		UnixSocket
	}
}