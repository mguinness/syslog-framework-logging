using System.Collections.Generic;
using Syslog.Framework.Logging.TransportProtocols;

namespace Syslog.Framework.Logging
{
	public class SyslogLoggerSettings
	{
		#region Fields and Methods

		/// <summary>
		/// Gets or sets the protocol used to send messages to a Syslog server.
		/// </summary>
		public TransportProtocol MessageTransportProtocol { get; set; } = TransportProtocol.Udp;
		
		/// <summary>
		/// Gets or sets the host for the Syslog server.
		/// </summary>
		/// <remarks>
		/// Used only when <see cref="MessageTransportProtocol"/> is set to <see cref="TransportProtocol.Udp"/>.
		/// </remarks>
		public string ServerHost { get; set; } = "127.0.0.1";

		/// <summary>
		/// Gets or sets the port for the Syslog server.
		/// </summary>
		/// <remarks>
		/// Used only when <see cref="MessageTransportProtocol"/> is set to <see cref="TransportProtocol.Udp"/>.
		/// </remarks>
		public int ServerPort { get; set; } = 514;

		/// <summary>
		/// Gets or sets the path to a Unix socket for logging.
		/// </summary>
		/// <remarks>
		/// Used only when <see cref="MessageTransportProtocol"/> is set to <see cref="TransportProtocol.UnixSocket"/>.
		/// </remarks>
		public string UnixSocketPath { get; set; } = "/dev/log";
		
		/// <summary>
		/// Gets or sets the facility type.
		/// </summary>
		public FacilityType FacilityType { get; set; } = FacilityType.Local0;

		/// <summary>
		/// Gets or sets the header type. Set this instead of HeaderFormat.
		/// </summary>
		public SyslogHeaderType HeaderType { get; set; } = SyslogHeaderType.Rfc3164; // Default to 3164 to be backwards compatible with v1.

		/// <summary>
		/// Structured data that is sent with every request. Only for RFC 5424.
		/// </summary>
		public IEnumerable<SyslogStructuredData> StructuredData { get; set; }

		/// <summary>
		/// Gets or sets whether to log messages using UTC or local time. Defaults to false (use local time).
		/// </summary>
		public bool UseUtc { get; set; } = false; // Default to false to be backwards compatible with v1.

		/// <summary>
		/// Gets or sets custom implementation of transport protocol.
		/// </summary>
		/// <remarks>
		/// When it is set, <see cref="MessageTransportProtocol"/> is ignored.
		/// </remarks>
		public IMessageSender CustomMessageSender { get; set; } 

		#endregion
	}

	/// <summary>
	/// Allows sending of structured data in RFC 5424.
	/// </summary>
	public class SyslogStructuredData
	{
		/// <summary>
		/// Creates an instance of SyslogStructuredData.
		/// </summary>
		public SyslogStructuredData()
		{
		}

		/// <summary>
		/// Creates an instance of SyslogStructuredData.
		/// </summary>
		/// <param name="id"></param>
		public SyslogStructuredData(string id)
		{
			Id = id;
		}

		/// <summary>
		/// Gets the ID for the structured data.
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// Gets the list of structured data elements.
		/// </summary>
		public IEnumerable<SylogStructuredDataElement> Elements { get; set; }
	}

	/// <summary>
	/// A named value for structured data.
	/// </summary>
	public class SylogStructuredDataElement
	{
		/// <summary>
		/// Creates an instance of SylogStructuredDataElement.
		/// </summary>
		public SylogStructuredDataElement()
		{
		}

		/// <summary>
		/// Creates an instance of SylogStructuredDataElement.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		public SylogStructuredDataElement(string name, string value)
		{
			Name = name;
			Value = value;
		}

		/// <summary>
		/// Gets the name of the element.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets the value of the element.
		/// </summary>
		public string Value { get; set; }
	}
}
