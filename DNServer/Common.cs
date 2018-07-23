using System;
using System.Net;

namespace DNServer
{
	public static class Common
	{
		public static IPAddress PTRName2IP(string str)
		{
			var s = str.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
			return IPAddress.Parse($@"{s[3]}.{s[2]}.{s[1]}.{s[0]}");
		}

		public static IPEndPoint String2IPEndPoint(string str)
		{
			if (str == null)
			{
				return null;
			}
			var s = str.Split(':');
			var ip = IPAddress.Parse(s[0]);
			var port = Convert.ToInt32(s[1]);
			return new IPEndPoint(ip, port);
		}
	}
}
