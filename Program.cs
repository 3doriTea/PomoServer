using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
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
						try
						{
							using var stream = client.GetStream();

							var buffer = new byte[1024];
							int length = stream.Read(buffer, 0, buffer.Length);
							var request = Encoding.UTF8.GetString(buffer);
							Console.WriteLine("request done");
							//Console.WriteLine(request);

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
								Console.WriteLine(BitConverter.ToString(responseBytes).Replace("-", " "));
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
								Console.WriteLine(BitConverter.ToString(responseBytes).Replace("-", " "));
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
