using DNS.Client;
using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using DNS.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DNServer
{
	public class ApartRequestResolver : IRequestResolver
	{
		public int Timeout = 1000;

		private readonly IPEndPoint[] _updns;
		private readonly IPEndPoint[] _puredns;

		private readonly HashSet<string> _domains = new HashSet<string>();

		public ApartRequestResolver(IPEndPoint[] updns, string domainListPath) :
		this(updns, null, domainListPath)
		{ }

		public ApartRequestResolver(IPEndPoint[] updns, IPEndPoint[] puredns, string domainListPath)
		{
			_updns = updns;
			_puredns = puredns;
			if (_puredns != null)
			{
				try
				{
					LoadDomainsList(domainListPath);
				}
				catch
				{
					Console.WriteLine($@"Load ""{domainListPath}"" fail!");
					//throw new Exception($@"Load ""{domainListPath}"" fail!");
				}
			}
		}

		public void LoadDomainsList(string path)
		{
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
							_domains.Add(domain);
						}
					}
				}
				Debug.WriteLine($@"Load ""{path}"" Success!");
				Console.WriteLine($@"Load ""{path}"" Success!");
			}
			else
			{
				Debug.WriteLine($@"No exist ""{path}""!");
				Console.WriteLine($@"No exist ""{path}""!");
				throw new Exception($@"No exist ""{path}""!");
			}
		}

		private bool IsOnList(string str)
		{
			if (str.EndsWith(@"in-addr.arpa"))
			{
				return true;
			}
			if (str.EndsWith(@".lan"))
			{
				str = str.RemoveLastString(@".lan");
			}
			else if (str.EndsWith(@".local"))
			{
				str = str.RemoveLastString(@".local");
			}

			if (_domains.TryGetValue(str, out _))
			{
				return true;
			}

			var parts = str.Split('.');
			foreach (var part in parts)
			{
				str = str.RemoveStartString(part);
				str = str.RemoveStartString(@".");
				if (string.IsNullOrWhiteSpace(str))
				{
					return false;
				}

				if (_domains.TryGetValue(str, out _))
				{
					return true;
				}
			}
			return false;
		}

		private static bool OutputLog(ICollection<IResourceRecord> records, Domain name, IPEndPoint dns)
		{
			if (records.Count == 0)
			{
				Debug.WriteLine($@"DNS query {name} no answer via {dns}");
				Console.WriteLine($@"DNS query {name} no answer via {dns}");
				return true;
			}
			else
			{
				foreach (var record in records)
				{
					string outstr;
					if (record.Type == RecordType.A || record.Type == RecordType.AAAA)
					{
						var iprecord = (IPAddressResourceRecord)record;
						outstr = $@"DNS query {name} answer {iprecord.IPAddress} via {dns}";
					}
					else if (record.Type == RecordType.CNAME)
					{
						var cnamerecord = (CanonicalNameResourceRecord)record;
						outstr = $@"DNS query {name} answer {cnamerecord.CanonicalDomainName} via {dns}";
					}
					else if (record.Type == RecordType.PTR)
					{
						var ptrrecord = (PointerResourceRecord)record;
						outstr = $@"DNS query {Common.PTRName2IP(name.ToString())} answer {ptrrecord.PointerDomainName} via {dns}";
					}
					else
					{
						outstr = $@"DNS query {name} {record.Type} via {dns}";
					}
					Debug.WriteLine(outstr);
					Console.WriteLine(outstr);
				}
				return false;
			}
		}

		public async Task<IResponse> Resolve(IRequest request)
		{
			IResponse res = Response.FromRequest(request);
			var question = res.Questions[0];
			IPEndPoint[] dnsS;
			if (_puredns != null)
			{
				dnsS = IsOnList(question.Name.ToString()) ? _updns : _puredns;
			}
			else
			{
				dnsS = _updns;
			}

			ClientResponse clientResponse = null;
			foreach (var dns in dnsS)
			{
				using (var udp = new UdpClient())
				{
					UdpReceiveResult result;
					try
					{
						await udp.SendAsync(request.ToArray(), request.Size, dns).WithCancellationTimeout(Timeout);
						result = await udp.ReceiveAsync().WithCancellationTimeout(Timeout);
					}
					catch
					{
						Console.WriteLine($@"DNS query timeout via {dns}");
						continue;
					}
					if (!result.RemoteEndPoint.Equals(dns))
					{
						throw new IOException(@"Remote endpoint mismatch");
					}

					var buffer = result.Buffer;
					var response = Response.FromArray(buffer);

					if (response.Truncated)
					{
						return await new NullRequestResolver().Resolve(request);
					}
					clientResponse = new ClientResponse(request, response, buffer);

					var records = clientResponse.AnswerRecords;
					var noAnswer = OutputLog(records, question.Name, dns);
					if (!noAnswer)
					{
						return clientResponse;
					}
				}
			}

			if (clientResponse == null)
			{
				Console.WriteLine(@"All DNS Servers response timeout!");
				throw new IOException(@"All DNS Servers response timeout!");
			}
			return clientResponse;
		}
	}
}