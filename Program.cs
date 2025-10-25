using HttpMultipartParser;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PomoServer
{
	internal class Program
	{
		private readonly static World.ResourceManager _resourceManager = new("../../db/", "data.dat", "header.dat");

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
						try
						{
							using var stream = client.GetStream();
							Console.WriteLine("client.GetStream :)");
							using var bufferStream = new MemoryStream();
							Console.WriteLine("created buff stream :|");
							stream.CopyTo(bufferStream, 1024);
							Console.WriteLine("copy stream ok :3");

							var request = Encoding.UTF8.GetString(bufferStream.ToArray());
							Console.WriteLine($"request done from l{client.Client.LocalEndPoint} r{client.Client.RemoteEndPoint}");

							void SendResponseHTML(byte[] contentBytes)
							{
								var response = string.Join("\r\n",
									[
										"HTTP/1.1 200 OK",
										//"Content-Type: text/plain",
										"Content-Type: text/html; charset=UTF-8",
										$"Content-Length: {contentBytes.Length}",
										"Cache-Control: no-cache",
										"Connection: keep-alive",
									// コンテンツは byte配列直で結合
								]);
								response += "\r\n\r\n";


								byte[] headerBytes = Encoding.UTF8.GetBytes(response);
								byte[] responseBytes = new byte[headerBytes.Length + contentBytes.Length];
								Array.Copy(headerBytes, responseBytes, headerBytes.Length);
								Array.Copy(contentBytes, 0, responseBytes, headerBytes.Length, contentBytes.Length);
								stream.Write(responseBytes, 0, responseBytes.Length);
							}

							void SendResponseText(byte[] contentBytes)
							{
								var response = string.Join("\r\n",
									[
										"HTTP/1.1 200 OK",
										"Content-Type: text/plain",
										$"Content-Length: {contentBytes.Length}",
										"Cache-Control: no-cache",
										"Connection: keep-alive",
									// コンテンツは byte配列直で結合
								]);
								response += "\r\n\r\n";


								byte[] headerBytes = Encoding.UTF8.GetBytes(response);
								byte[] responseBytes = new byte[headerBytes.Length + contentBytes.Length];
								Array.Copy(headerBytes, responseBytes, headerBytes.Length);
								Array.Copy(contentBytes, 0, responseBytes, headerBytes.Length, contentBytes.Length);
								stream.Write(responseBytes, 0, responseBytes.Length);
							}

							if (request.StartsWith("GET"))
							{
								if (request.StartsWith("GET /count"))
								{
									SendResponseText(Encoding.UTF8.GetBytes($"{accessCount}"));
								}
								else
								{
									SendResponseHTML(Encoding.UTF8.GetBytes(File.ReadAllText("./public/index.html")
										.Replace("{%%hogehogefugafuga}", $"{++accessCount}")));
								}
							}
							//else if (request.StartsWith("POST"))
							//{
							//	if (request.StartsWith("POST /objectcreate"))
							//	{
							//		var parser = await MultipartFormDataParser.ParseAsync(stream);
							//		foreach (var file in parser.Files)
							//		{
							//			using var ms = new MemoryStream();
							//			file.Data.CopyTo(ms);
							//			bool succeed = await _resourceManager.AddFile(
							//				file.FileName,
							//				file.FileName,
							//				ms.ToArray()
							//				).WaitAsync(CancellationToken.None);
							//			//if (succeed == false)
							//			//{
											
							//			//}
							//		}
							//	}
							//}
						}
						catch (Exception ex)
						{
							Console.WriteLine(ex);
						}
						finally
						{
							client.Close();
							client.Dispose();
						}
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
