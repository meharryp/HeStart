using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace HeStart
{
	class SQuery
	{
		public static UdpClient QueryWorker = new UdpClient();
        public static int IPAmnt = 0;
        public static byte[] Regions = new byte[9]{ 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0xFF };
        public static int DropCount = 0;
        public static bool DroppedLast = false;

		public static void AddToBytes(List<byte> bytes, string str, bool nullTerminated=true) {
			foreach (byte b in Encoding.ASCII.GetBytes(str))
			{
				bytes.Add(b);
			}

			if (nullTerminated)
			{
				bytes.Add(0x00);
			}
		}

		static byte[] CreateData(string ip, byte region, string payload) {
			List<byte> data = new List<byte>();

			data.Add(0x31);
			data.Add(region);

			AddToBytes(data, ip);
			AddToBytes(data, payload);

			byte[] dgram = data.ToArray<byte>();

			return dgram;
		}

		/*public static async Task<UdpReceiveResult> SendToServer(UdpClient client, byte[] data) {
			client.Send(data, data.Length);
			UdpReceiveResult res = await client.ReceiveAsync();
			return res;
		}*/

		async static void Query(byte region, UdpClient client, string ip="0.0.0.0:0") {
			byte[] data = CreateData(ip, region, @"\dedicated\1\appid\4000");
			byte[] buffer = new byte[6];

			try
			{
				buffer = await SendToServerExAsync(data, client);
				ReadA2S_INFO(data);
			}
			catch (Exception e)
			{
				Console.WriteLine("AS2_INFO failed");
			}

			MemoryStream Stream = new MemoryStream(buffer);
			BinaryReader Reader = new BinaryReader(Stream);

			Reader.ReadBytes(6);

			string lastIP = "0.0.0.0:0";

			List<string> IPs = new List<string>();

			for (int i=6; i < Reader.BaseStream.Length; i+=6) {
				IPAmnt++;
				lastIP = "";

				lastIP += Reader.ReadByte() + ".";
				lastIP += Reader.ReadByte() + ".";
				lastIP += Reader.ReadByte() + ".";
				lastIP += Reader.ReadByte() + ":";

				byte p1 = Reader.ReadByte();
				byte p2 = Reader.ReadByte();

				lastIP += BitConverter.ToUInt16(new byte[2] { p2, p1 }, 0); // steam responds with litle endian ports

				if (lastIP == "31.220.45.227:27015") {
					throw new Exception("Look ma, I'm on TV!");
				}

				Console.WriteLine(lastIP);
				IPs.Add(lastIP);
			}

			Thread q = new Thread(async () => await A2SQuery(IPs));
			q.Start();

			if (lastIP != "0.0.0.0:0") {
				Query(region, client, lastIP);
			}
		}

        public static async Task<byte[]> SendToServerExAsync(byte[] data, UdpClient ServerQuery, IPEndPoint ip = null)
        {
            await ServerQuery.SendAsync(data, data.Length, ip);
            Task<UdpReceiveResult> serverTask = ServerQuery.ReceiveAsync();
            var AsyncResult = await Task.WhenAny(serverTask, Task.Delay(7500));

            if (AsyncResult == serverTask)
            {
                try
                {
                    IPEndPoint remoteEP = null;
                    var resultTask = (Task<UdpReceiveResult>)AsyncResult;
                    byte[] receivedData = resultTask.Result.Buffer;
                    return receivedData;
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            else
            {
                throw new TimeoutException("Socket timed out");
            }
        }

        public static async Task<List<A2SInfoData>> A2SQuery(List<string> IPs) {
            List<A2SInfoData> DataList = new List<A2SInfoData>();

			foreach (string host in IPs)
			{
				if (host == "0.0.0.0:0")
				{
                    break;
				}

				string[] hostport = host.Split(':');
				UdpClient ServerQuery = new UdpClient();
				ServerQuery.Client.Connect(hostport[0], int.Parse(hostport[1]));
				ServerQuery.Client.ReceiveTimeout = 1;

				List<byte> payload = new List<byte>();
				payload.Add(0xFF);
				payload.Add(0xFF);
				payload.Add(0xFF);
				payload.Add(0xFF);
				payload.Add(0x54);

				AddToBytes(payload, "Source Engine Query");

				byte[] dgram = payload.ToArray<byte>();

				try
				{
					byte[] data = await SendToServerExAsync(dgram, ServerQuery);
					A2SInfoData res = ReadA2S_INFO(data, host);
                    DataList.Add(res);
				} catch (Exception e) {
					Console.WriteLine("AS2_INFO failed");
                    DropCount++;
                    DroppedLast = true;
				}
			}

            return DataList;
		}

		public static string ReadSourceString(BinaryReader Reader) {
			List<char> chars = new List<char>();
			bool cont = true;

			while (cont)
			{
				char c = Reader.ReadChar();
				if (c != '\0')
				{
					chars.Add(c);
				} else {
					cont = false;
				}
			}

			return new string(chars.ToArray<char>());
		}

		static A2SInfoData ReadA2S_INFO(byte[] data, string ip="0.0.0.0:0") {
			MemoryStream Stream = new MemoryStream(data);
			BinaryReader Reader = new BinaryReader(Stream);

			Reader.ReadBytes(4);
			byte header = Reader.ReadByte();
			byte protocol = Reader.ReadByte();
			string hostname = ReadSourceString(Reader);
			string map = ReadSourceString(Reader);
			string folder = ReadSourceString(Reader);
			string game = ReadSourceString(Reader);
			short id = Reader.ReadInt16();
			byte players = Reader.ReadByte();
			byte maxplayers = Reader.ReadByte();
			byte bots = Reader.ReadByte();
			char type = Convert.ToChar(Reader.ReadByte());
			char os = Convert.ToChar(Reader.ReadByte());
			bool password = Convert.ToBoolean(Reader.ReadByte());
			bool vac = Convert.ToBoolean(Reader.ReadByte());
			string version = ReadSourceString(Reader);

            return new A2SInfoData(header, protocol, hostname, map, folder, game, id, players, maxplayers, bots, type, os, password, vac, version, ip);
		}
	}
}
