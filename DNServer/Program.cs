using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DNS.Client;
using DNS.Server;
using CommandLine;

namespace DNServer
{
	internal class Program
	{
		private class Options
		{
			[Option('u', "updns", HelpText = "Up DNS Server", Default = @"119.29.29.29:53")]
			public string UpDNS { get; set; }

			[Option('p', "puredns", HelpText = "Pure DNS Server", Default = @"223.113.97.99:53")]
			public string PureDNS { get; set; }

			[Option('l', "list", HelpText = "Domains list file path", Default = @"chndomains.txt")]
			public string Path { get; set; }
		}

		static void Main(string[] args)
		{
			Parser.Default.ParseArguments<Options>(args)
			.WithParsed(opts => RunOptionsAndReturnExitCode(opts))
			.WithNotParsed((errs) => HandleParseError(errs));

		}

		private static int RunOptionsAndReturnExitCode(Options options)
		{
			IPEndPoint updns;
			IPEndPoint puredns;
			try
			{
				updns = String2IPEndPoint(options.UpDNS);
				Console.WriteLine($@"UpDNS:{updns}");
				puredns = String2IPEndPoint(options.PureDNS);
				Console.WriteLine($@"PureDNS:{puredns}");
			}
			catch
			{
				Console.WriteLine(@"DNS Server ERROR!");
				return 1;
			}
			var path = options.Path;
			StartDNServer_Async(updns, puredns, path).Wait();
			return 0;
		}

		private static void HandleParseError(IEnumerable<Error> errs)
		{

		}

		private static IPEndPoint String2IPEndPoint(string str)
		{
			var s = str.Split(':');
			var ip = IPAddress.Parse(s[0]);
			var port = Convert.ToInt32(s[1]);
			return new IPEndPoint(ip, port);
		}
		private static async Task StartDNServer_Async(IPEndPoint updns, IPEndPoint puredns, string path)
		{
			var server = new DnsServer(new ApartRequestResolver(updns, puredns, path));

			server.Requested += (request) => Console.WriteLine($@"Requested: {request}");
			server.Responded += (request, response) => Console.WriteLine($@"Responded: {request} => {response}");
			server.Listening += () => Console.WriteLine(@"Listening");
			server.Errored += (e) =>
			{
				Console.WriteLine($@"Errored: {e}");
				if (e is ResponseException responseError)
				{
					Console.WriteLine(responseError.Response);
				}
			};

			await server.Listen(53);
		}
	}
}