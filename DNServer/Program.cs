﻿using System;
using System.Net;
using System.Threading.Tasks;
using DNS.Client;
using DNS.Server;

namespace DNServer
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			StartDNServer_Async().Wait();
		}

		private static async Task StartDNServer_Async()
		{
			var dnspod = new IPEndPoint(IPAddress.Parse(@"119.29.29.29"), 53);
			var localdns = new IPEndPoint(IPAddress.Parse(@"127.0.0.1"), 5533);
			const string path = @"D:\Downloads\chndomains.txt";
			var server = new DnsServer(new ApartRequestResolver(dnspod, localdns, path));

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