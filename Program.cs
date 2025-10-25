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

			Console.CancelKeyPress += (sender, e) =>
			{
				listener.Stop();
			};

			Console.WriteLine($"Listening on {listener.LocalEndpoint}");

			int accessCount = 0;

			while (true)
			{
				try
				{
					var client = listener.AcceptTcpClient();

					Task.Run(() =>
					{
						using var stream = client.GetStream();

						var buffer = new byte[1024];
						int length = stream.Read(buffer, 0, buffer.Length);
						var request = Encoding.UTF8.GetString(buffer);
						Console.WriteLine("request done");
						//Console.WriteLine(request);

						void SendResponse(byte[] contentBytes)
						{
							var response = string.Join("\r\n",
								[
									"HTTP/1.1 200 OK",
									"Content-Type: text/plain",
									$"Content-Length: {contentBytes.Length}",
									"",
									// コンテンツは byte配列直で結合
								]);

							byte[] headerBytes = Encoding.UTF8.GetBytes(response);
							byte[] responseBytes = new byte[headerBytes.Length + contentBytes.Length];
							stream.Write(responseBytes, 0, responseBytes.Length);
						}

						if (request.StartsWith("GET"))
						{
							//SendResponse(Encoding.UTF8.GetBytes($"Hello World! Access Count:{++accessCount}"));
							{
								var response = string.Join("\r\n",
								[
									"HTTP/1.1 200 OK",
									"Content-Type: text/plain",
									$"Content-Length: 12",
									"",
									"Hello World!"
								]);
								var responseBytes = Encoding.UTF8.GetBytes(response);
								stream.Write(responseBytes, 0, responseBytes.Length);
							}
						}

						client.Close();
						client.Dispose();
					});
				}
				catch (SocketException e)
				{
					Console.WriteLine(e);
					break;
				}
			}
		}
	}
}
