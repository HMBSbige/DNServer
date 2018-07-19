using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using DNS.Client;
using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.Utils;

namespace DNServer
{
	public class ApartRequestResolver : IRequestResolver
	{
		private readonly IPEndPoint _updns;
		private readonly IPEndPoint _puredns;

		private readonly List<string> _domains = new List<string>();
		public ApartRequestResolver(IPEndPoint updns, IPEndPoint puredns, string domainListPath)
		{
			_updns = updns;
			_puredns = puredns;
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
			foreach (var domain in _domains)
			{
				if (str.Length < domain.Length)
				{
					continue;
				}
				else if (str.Length == domain.Length)
				{
					if (str == domain)
					{
						return true;
					}
					else
					{
						continue;
					}
				}
				else
				{
					if (str[str.Length - domain.Length - 1] == '.' && str.Substring(str.Length - domain.Length) == domain)
					{
						return true;
					}
					else
					{
						continue;
					}
				}
			}
			return false;
		}

		public async Task<IResponse> Resolve(IRequest request)
		{
			IResponse res = Response.FromRequest(request);
			var question = res.Questions[0];

			var dns = IsOnList(question.Name.ToString()) ? _updns : _puredns;

			Debug.WriteLine($@"DNS query {question.Name} via {dns}");
			Console.WriteLine($@"{Environment.NewLine}DNS query {question.Name} via {dns}{Environment.NewLine}");

			using (var udp = new UdpClient())
			{
				await udp.SendAsync(request.ToArray(), request.Size, dns).WithCancellationTimeout(5000);

				var result = await udp.ReceiveAsync().WithCancellationTimeout(5000);

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

				return new ClientResponse(request, response, buffer);
			}
		}
	}
}