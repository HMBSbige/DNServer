using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DNServer
{
	public class ConditionalForwardingDnsServer : DnsServer
	{
		#region 私有成员

		private const int DefaultDnsPort = 53;

		private readonly HashSet<DomainName> domains = new HashSet<DomainName>();

		private readonly IEnumerable<string> specialDomains = new List<string> { @"lan", @"local", @"localdomain" };
		private readonly IEnumerable<string> ptrDomains = new List<string> { @"in-addr.arpa" };

		#endregion

		#region 共有成员

		public DnsClient UpStreamDns;

		public DnsClient PureDns;

		public ClientSubnetOption UpStreamEcs;

		public ClientSubnetOption PureEcs;

		#endregion

		#region 构造函数

		public ConditionalForwardingDnsServer(int udpListenerCount, int tcpListenerCount) :
			this(IPAddress.Any, udpListenerCount, tcpListenerCount)
		{ }

		public ConditionalForwardingDnsServer(IPAddress bindAddress, int udpListenerCount, int tcpListenerCount, int port = DefaultDnsPort) :
			this(new IPEndPoint(bindAddress, port), udpListenerCount, tcpListenerCount)
		{ }

		public ConditionalForwardingDnsServer(IPEndPoint bindEndPoint, int udpListenerCount, int tcpListenerCount) :
				base(bindEndPoint, udpListenerCount, tcpListenerCount)
		{
			UpStreamDns = DnsClient.Default;
			PureDns = DnsClient.Default;
			UpStreamEcs = null;
			PureEcs = null;
			LoadDomains(specialDomains);
			LoadDomains(ptrDomains);
			QueryReceived += OnQueryReceived;
		}

		#endregion

		#region 白名单列表

		public void LoadDomains(IEnumerable<string> list)
		{
			foreach (var domain in list)
			{
				if (DomainName.TryParse(domain, out var name))
				{
					domains.Add(name);
				}
			}
		}

		private bool IsOnList(DomainName name)
		{
			foreach (var domain in domains)
			{
				if (name.IsEqualOrSubDomainOf(domain))
				{
					return true;
				}
			}

			return false;
		}

		private bool IsLocal(DomainName name)
		{
			return specialDomains.Any(domain => name.IsEqualOrSubDomainOf(DomainName.Parse(domain)));
		}

		#endregion

		private static bool ExistEcs(OptRecord eDnsOptions)
		{
			return eDnsOptions?.Options != null && eDnsOptions.Options.OfType<ClientSubnetOption>().Any();
		}

		private async Task OnQueryReceived(object sender, QueryReceivedEventArgs e)
		{
			if (e.Query is DnsMessage message)
			{
				var response = message.CreateResponseInstance();

				if (message.Questions.Count == 1)
				{
					DnsClient dnsClient;
					var options = new DnsQueryOptions
					{
						IsEDnsEnabled = true,
						IsRecursionDesired = true
					};

					var existEcs = ExistEcs(message.EDnsOptions);
					if (existEcs)
					{
						foreach (var option in message.EDnsOptions.Options)
						{
							options.EDnsOptions.Options.Add(option);
						}
					}

					var question = message.Questions[0];
					Console.WriteLine($@"DNS query: {question.Name}");
					if (IsLocal(question.Name))
					{
						response.ReturnCode = ReturnCode.NxDomain;
						e.Response = response;
						return;
					}
					else if (IsOnList(question.Name))
					{
						dnsClient = UpStreamDns;
						if (!existEcs)
						{
							if (UpStreamEcs != null)
							{
								options.EDnsOptions.Options.Add(UpStreamEcs);
							}
							else //if (question.RecordType != RecordType.Ptr)
							{
								options.EDnsOptions.Options.Add(new ClientSubnetOption(32, e.RemoteEndpoint.Address));
							}
						}
					}
					else
					{
						dnsClient = PureDns;
						if (!existEcs)
						{
							if (PureEcs != null)
							{
								options.EDnsOptions.Options.Add(PureEcs);
							}
							else //if (question.RecordType != RecordType.Ptr)
							{
								options.EDnsOptions.Options.Add(new ClientSubnetOption(32, e.RemoteEndpoint.Address));
							}
						}
					}

					// send query to upstream server
					var upstreamResponse = await dnsClient.ResolveAsync(question.Name, question.RecordType, question.RecordClass, options);

					// if got an answer, copy it to the message sent to the client
					if (upstreamResponse != null)
					{
						foreach (var record in upstreamResponse.AnswerRecords)
						{
							response.AnswerRecords.Add(record);
						}

						foreach (var record in upstreamResponse.AdditionalRecords)
						{
							response.AdditionalRecords.Add(record);
						}

						//if (existEcs)
						{
							response.EDnsOptions = upstreamResponse.EDnsOptions;
						}

						response.ReturnCode = ReturnCode.NoError;
						e.Response = response;
					}
				}
				else
				{
					response.ReturnCode = ReturnCode.FormatError;
					e.Response = response;
				}
			}
		}
	}
}
