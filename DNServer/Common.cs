using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace DNServer
{
	public static class Common
	{
		public static IPAddress PTRName2IP(string str)
		{
			var s = str.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
			return IPAddress.Parse($@"{s[3]}.{s[2]}.{s[1]}.{s[0]}");
		}

		private static readonly Regex Ipv4Pattern = new Regex("^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\\.){1}(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\\.){2}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");

		public static bool IsIPv4Address(string input)
		{
			return Ipv4Pattern.IsMatch(input);
		}

		public static bool IsPort(int port)
		{
			if (port >= IPEndPoint.MinPort && port <= IPEndPoint.MaxPort)
			{
				return true;
			}

			return false;
		}

		public static IPEndPoint ToIPEndPoint(string str, int defaultport)
		{
			if (string.IsNullOrWhiteSpace(str) || !IsPort(defaultport))
			{
				return null;
			}

			var s = str.Split(':');
			if (s.Length == 1 || s.Length == 2)
			{
				if (!IsIPv4Address(s[0]))
				{
					return null;
				}

				var ip = IPAddress.Parse(s[0]);
				if (s.Length == 2)
				{
					var port = Convert.ToInt32(s[1]);
					if (IsPort(port))
					{
						return new IPEndPoint(ip, port);
					}
				}
				else
				{
					return new IPEndPoint(ip, defaultport);
				}
			}

			return null;
		}

		public static IEnumerable<IPEndPoint> ToIPEndPoints(string str, int defaultport, char[] separator)
		{
			if (string.IsNullOrWhiteSpace(str))
			{
				return null;
			}
			var s = str.Split(separator, StringSplitOptions.RemoveEmptyEntries);
			var res = s.Select(ipEndPointsStr => ToIPEndPoint(ipEndPointsStr, defaultport)).Where(ipend => ipend != null)
					.ToList();
			return res.ToArray();
		}

		public static string FromIPEndPoints(IEnumerable<IPEndPoint> ipEndPoints, char separator = ',')
		{
			return string.Join(separator, ipEndPoints);
		}

		public static string RemoveLastString(this string str, string value)
		{
			var index = str.LastIndexOf(value, StringComparison.Ordinal);
			if (index != -1)
			{
				return str.Substring(0, index);
			}

			return str;
		}

		public static string RemoveStartString(this string str, string value)
		{
			var index = str.IndexOf(value, StringComparison.Ordinal);
			if (index == 0)
			{
				return str.Substring(value.Length);
			}

			return str;
		}
	}
}
