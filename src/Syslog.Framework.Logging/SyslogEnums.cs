namespace Syslog.Framework.Logging
{
	/// <summary>
	/// Different header formats that are available.
	/// </summary>
	public enum SyslogHeaderType
	{
		Rfc3164,
		Rfc5424v1
	}

	/// <summary>
	/// Available facility types. Same values for both RFC 3164 & 5424.
	/// </summary>
	public enum FacilityType
	{
		Kern = 0,
		User = 1,
		Mail = 2,
		Daemon = 3,
		Auth = 4,
		Syslog = 5,
		LPR = 6,
		News = 7,
		UUCP = 8,
		Cron = 9,
		AuthPriv = 10,
		FTP = 11,
		NTP = 12,
		Audit = 13,
		Audit2 = 14,
		CRON2 = 15,
		Local0 = 16,
		Local1 = 17,
		Local2 = 18,
		Local3 = 19,
		Local4 = 20,
		Local5 = 21,
		Local6 = 22,
		Local7 = 23
	}

	internal enum SeverityType { Emergency, Alert, Critical, Error, Warning, Notice, Informational, Debug };
}
