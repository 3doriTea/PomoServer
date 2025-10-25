using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PomoServer
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var listener = new TcpListener(IPAddress.Parse("192.168.3.159"), 80);
			listener.Start();
			Console.WriteLine($"Listening on {listener.LocalEndpoint}");
	
			using var client = listener.AcceptTcpClient();
			using var stream = client.GetStream();
	
			var buffer = new byte[1024];
			int length = stream.Read(buffer, 0, buffer.Length);
			var request = Encoding.UTF8.GetString(buffer);
			Console.WriteLine(request);
	
			if (request.StartsWith("GET"))
			{
				var response = string.Join("\r\n",
				[
					"HTTP/1.1 200 OK",
					"Content-Type: text/plain",
					"Content-Length: 11",
					"",
					"Hello World"
				]);
	
				byte[] responseBytes = Encoding.UTF8.GetBytes(response);
				stream.Write(responseBytes, 0, responseBytes.Length);
			}
		}
	}
}
