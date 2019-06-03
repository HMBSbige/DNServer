using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using System;
using System.Collections.Generic;
using System.IO;
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
		private readonly IEnumerable<string> banDomains = new List<string> { };

		#endregion

		#region 公有成员

		public DnsClient UpStreamDns;

		public DnsClient PureDns;

		public ClientSubnetOption UpStreamEcs;

		public ClientSubnetOption PureEcs;

		public bool BanAny;

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
			BanAny = false;
			LoadDomains(specialDomains);
			LoadDomains(ptrDomains);
			QueryReceived += OnQueryReceived;
		}

		#endregion

		#region 规则

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

		private void IsBan(DnsMessageEntryBase question)
		{
			var name = question.Name;
			var recordType = question.RecordType;
			if (BanAny && recordType == RecordType.Any)
			{
				throw new InvalidDataException(@"Query Refused: RecordType.Any");
			}

			if (banDomains.Any(domain => name.IsEqualOrSubDomainOf(DomainName.Parse(domain))))
			{
				throw new InvalidDataException($@"Query Refused: {name}");
			}
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
					var question = message.Questions[0];
					IsBan(question);

					DnsClient dnsClient;
					DnsClient dnsClientBackup;
					var options = new DnsQueryOptions
					{
						IsEDnsEnabled = true,
						IsRecursionDesired = message.IsRecursionDesired,
						IsDnsSecOk = message.IsDnsSecOk,
						IsCheckingDisabled = message.IsCheckingDisabled
					};

					var existEcs = ExistEcs(message.EDnsOptions);
					if (existEcs)
					{
						foreach (var option in message.EDnsOptions.Options)
						{
							options.EDnsOptions.Options.Add(option);
						}
					}

					Console.WriteLine($@"{e.RemoteEndpoint} Connected: {e.ProtocolType}");
					Console.WriteLine($@"DNS query: {question.Name} {question.RecordClass} {question.RecordType}");
					if (IsLocal(question.Name))
					{
						response.ReturnCode = ReturnCode.NxDomain;
						e.Response = response;
						return;
					}
					else if (IsOnList(question.Name))
					{
						dnsClient = UpStreamDns;
						dnsClientBackup = PureDns;
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
						dnsClientBackup = UpStreamDns;
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
						upstreamResponse.TransactionID = response.TransactionID;
						if (!existEcs)
						{
							upstreamResponse.EDnsOptions = response.EDnsOptions;
						}
						response = upstreamResponse;
						response.ReturnCode = ReturnCode.NoError;
						e.Response = response;
						return;
					}

					upstreamResponse = await dnsClientBackup.ResolveAsync(question.Name, question.RecordType, question.RecordClass, options);
					if (upstreamResponse != null)
					{
						upstreamResponse.TransactionID = response.TransactionID;
						if (!existEcs)
						{
							upstreamResponse.EDnsOptions = response.EDnsOptions;
						}

						response = upstreamResponse;
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
