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

					Task.Run(async () =>
					{
						try
						{
							using var stream = client.GetStream();

							using var bufferStream = new MemoryStream();
							int length = int.MaxValue;
							byte[] buffer = new byte[1024];
							while (stream.DataAvailable)
							{
								length = await stream.ReadAsync(buffer);
								await bufferStream.WriteAsync(buffer);
							}
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
								Console.Write("Method: Get");
								if (request.StartsWith("GET /count"))
								{
									Console.Write(" /count");
									SendResponseText(Encoding.UTF8.GetBytes($"{accessCount}"));
								}
								else
								{
									Console.Write(" /");
									SendResponseHTML(Encoding.UTF8.GetBytes(File.ReadAllText("./public/index.html")
										.Replace("{%%hogehogefugafuga}", $"{++accessCount}")));
								}
							}
							else if (request.StartsWith("POST"))
							{
								Console.Write("Method: POST");
								if (request.StartsWith("POST /objectcreate"))
								{
									Console.Write(" /objectcreate");
									bufferStream.Seek(0, SeekOrigin.Begin);
									var parser = await MultipartFormDataParser.ParseAsync(bufferStream);
									Console.Write("parsed");
									foreach (var file in parser.Files)
									{
										Console.Write("file reading");
										using var ms = new MemoryStream();
										file.Data.CopyTo(ms);
										bool succeed = await _resourceManager.AddFile(
											file.FileName,
											file.FileName,
											ms.ToArray()
											).WaitAsync(CancellationToken.None);

										Console.WriteLine($"upload {(succeed ? "OK" : "NG")}");

										if (succeed)
										{
										}
										else
										{

										}
									}
								}
							}
							else
							{
								Console.Write($"{request.Substring(0, 30)}...");
							}
							Console.WriteLine();
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
