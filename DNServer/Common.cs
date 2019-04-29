using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace DNServer
{
	public static class Common
	{
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

		public static IPAddress ToIpAddress(string str)
		{
			if (string.IsNullOrWhiteSpace(str))
			{
				return null;
			}

			return IPAddress.TryParse(str, out var ip) ? ip : null;
		}

		public static IEnumerable<IPAddress> ToIpAddresses(string str, char[] separator)
		{
			if (string.IsNullOrWhiteSpace(str))
			{
				return null;
			}
			var s = str.Split(separator, StringSplitOptions.RemoveEmptyEntries);
			var res = new List<IPAddress>();
			foreach (var ipStr in s)
			{
				if (IPAddress.TryParse(ipStr, out var ip))
				{
					res.Add(ip);
				}
			}
			return res;
		}

		public static string FromIpAddresses(IEnumerable<IPAddress> ips, char separator = ',')
		{
			return string.Join(separator, ips);
		}

		public static IEnumerable<string> ReadLines(string path)
		{
			var list = new List<string>();
			if (File.Exists(path))
			{
				using (var sr = new StreamReader(path, Encoding.UTF8))
				{
					string line;
					while ((line = sr.ReadLine()) != null)
					{
						var domain = line;
						if (!string.IsNullOrWhiteSpace(domain))
						{
							list.Add(domain);
						}
					}
				}
			}
			return list;
		}
	}
}
