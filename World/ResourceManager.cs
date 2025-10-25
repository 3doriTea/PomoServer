using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PomoServer.World.ResourceManager.Header;

namespace PomoServer.World
{
	internal class ResourceManager(string dir, string dataFileName, string headerFileName)
	{
		// リソースのヘッダ
		public class Header
		{
			// 1データのヘッダ
			public class DataHeader(string fileName, string nickName, int byteOffset, int fileSize)
			{
				public string FileName { get; private set; } = fileName;  // ファイル名
				public string NickName { get; private set; } = nickName;  // ファイルへの簡易的な名前
				public long Offset { get; private set; } = byteOffset;     // データが始まるバイト数
				public long Size { get; private set; } = fileSize;     // ファイルのバイト数
			}

			public long Size { get; internal set; } = 0;  // ヘッダーのサイズ
			public List<DataHeader> DataHeaders { get; internal set; } = [];
		}

		private string _dataFileName = Path.Join(dir, dataFileName);
		private string _headerFileName = Path.Join(dir, headerFileName);

		public Header? ResourceHeader { get; private set; } = null;

		/// <summary>
		/// ヘッダを読み込むだけ
		/// </summary>
		/// <returns></returns>
		public Task Load()
		{
			return Task.Run(() =>
			{
				ResourceHeader = new Header();

				if (Directory.Exists(dir) == false)
				{
					Directory.CreateDirectory(dir);
				}

				using var fs = new FileStream(_headerFileName, FileMode.OpenOrCreate, FileAccess.Read);

				ResourceHeader.Size = fs.Length;

				if (ResourceHeader.Size <= 0)
				{
					return;  // ヘッダ情報これ以上ないなら回帰
				}

				// ファイルデータのヘッダを取得
				var buffer = new byte[ResourceHeader.Size];
				fs.Read(buffer, 0, (int)ResourceHeader.Size);

				using var ms = new MemoryStream(buffer);
				using var br = new BinaryReader(ms);

				while (br.BaseStream.Position < br.BaseStream.Length)
				{
					string fileName = br.ReadString();
					string nickName = br.ReadString();
					int offset = br.ReadInt32();
					int fileSize = br.ReadInt32();
					ResourceHeader.DataHeaders.Add(new DataHeader(fileName, nickName, offset, fileSize));
				}
			});
		}

		/// <summary>
		/// ファイルをニックネームから取得する
		/// </summary>
		/// <param name="nickName"></param>
		/// <returns></returns>
		/// <exception cref="NullReferenceException"></exception>
		public async Task<byte[]?> GetFileNick(string nickName)
		{
			if (ResourceHeader is null)
			{
				// リソースのヘッダが読み込まれていなかったら再読み込み
				await Load().WaitAsync(CancellationToken.None);

				if (ResourceHeader is null)
				{
					throw new NullReferenceException("ResourceHeader is null");
				}
			}

			var dataHeader = ResourceHeader.DataHeaders.Find(data => data.NickName == nickName);
			if (dataHeader is null)
			{
				return null;  // ファイルが見つからなかった！しらんニックネーム
			}

			using var fs = new FileStream(_dataFileName, FileMode.Open, FileAccess.Read);
			fs.Seek(dataHeader.Offset, SeekOrigin.Begin);

			var buffer = new byte[dataHeader.Size];
			int readLength = await fs.ReadAsync(buffer, CancellationToken.None);

			return buffer;
		}

		/// <summary>
		/// ファイルニックネームがユニークであるかチェック
		/// </summary>
		/// <param name="nickName">チェックするニックネーム</param>
		/// <returns>ユニークである true / false</returns>
		/// <exception cref="NullReferenceException">ありえない</exception>
		public async Task<bool> CheckUnique(string nickName)
		{
			if (ResourceHeader is null)
			{
				// リソースのヘッダが読み込まれていなかったら再読み込み
				await Load().WaitAsync(CancellationToken.None);

				if (ResourceHeader is null)
				{
					throw new NullReferenceException("ResourceHeader is null");
				}
			}

			foreach (var header in ResourceHeader.DataHeaders)
			{
				if (header.NickName == nickName)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// ファイルデータを追加する
		/// </summary>
		/// <param name="fileName">ファイル名</param>
		/// <param name="nickName">ファイルニックネーム(ユニーク)</param>
		/// <param name="buffer">ファイルデータ</param>
		/// <returns>追加に成功 true / false</returns>
		public Task<bool> AddFile(string fileName, string nickName, byte[] buffer)
		{
			return Task.Run(async () =>
			{
				// もしニックネームがユニークじゃないならダメーーー
				if (await CheckUnique(nickName) == false)
				{
					return false;
				}

				long offset;  // データのオフセット
				{
					using var fs = new FileStream(_dataFileName, FileMode.OpenOrCreate, FileAccess.Write);
					using var bw = new BinaryWriter(fs);

					offset = fs.Seek(0, SeekOrigin.End);
					fs.Write(buffer);
				}

				{
					using var fs = new FileStream(_headerFileName, FileMode.Open, FileAccess.Write);
					using var bw = new BinaryWriter(fs);

					fs.Seek(0, SeekOrigin.End);
					bw.Write(fileName);
					bw.Write(nickName);
					bw.Write(offset);
					bw.Write(buffer);
				}

				// 追加したら再読み込み
				await Load().WaitAsync(CancellationToken.None);

				return true;
			});
		}
	}
}
