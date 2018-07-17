using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DNS.Client.RequestResolver;
using DNS.Protocol;

namespace DNServer
{
	public class ApartRequestResolver : IRequestResolver
	{
		private readonly IPEndPoint _updns;
		private readonly IPEndPoint _puredns;

		private readonly List<Regex> _domains = new List<Regex>();
		public ApartRequestResolver(IPEndPoint updns, IPEndPoint puredns,string domainListPath)
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
							var pattern = $@"^(.*\.)?{domain.Replace(@".", @"\.")}$";
							var reg = new Regex(pattern);
							_domains.Add(reg);
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

		public async Task<IResponse> Resolve(IRequest request)
		{
			IResponse response = null;

			foreach (var question in request.Questions)
			{
				var udp = new UdpClient();
				var dns = _puredns;

				foreach (var domain in _domains)
				{
					if (domain.IsMatch(question.Name.ToString()))
					{
						dns = _updns;
						break;
					}
				}

				Debug.WriteLine($@"DNS query {question.Name} via {dns}");
				Console.WriteLine($@"{Environment.NewLine}DNS query {question.Name} via {dns}{Environment.NewLine}");
				await udp.SendAsync(request.ToArray(), request.Size, dns);

				var result = await udp.ReceiveAsync();
				var buffer = result.Buffer;
				var res = Response.FromArray(buffer);
				response = res;
				if (response.AnswerRecords.Count > 0)
				{
					break;
				}
			}

			return response;
		}
	}
}