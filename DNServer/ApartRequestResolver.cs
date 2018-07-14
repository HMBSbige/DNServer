using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using DNS.Client.RequestResolver;
using DNS.Protocol;

namespace DNServer
{
	public class ApartRequestResolver : IRequestResolver
	{
		private readonly IPEndPoint _updns;
		private readonly IPEndPoint _puredns;

		public ApartRequestResolver(IPEndPoint updns, IPEndPoint puredns)
		{
			_updns = updns;
			_puredns = puredns;
		}

		public async Task<IResponse> Resolve(IRequest request)
		{
			IResponse response = null;

			foreach (var question in request.Questions)
			{
				var udp = new UdpClient();
				if (question.Name.ToString() == @"www.google.com")
				{
					await udp.SendAsync(request.ToArray(), request.Size, _puredns);
				}
				else
				{
					await udp.SendAsync(request.ToArray(), request.Size, _updns);
				}

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