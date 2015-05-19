using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using uNet2.Channel;
using uNet2.Packet;
using uNet2.SocketOperation;

namespace uNet2.TestClient
{
    public class FileTransferOperation : SocketOperationBase
    {
        public override int OperationId
        {
            get { return 1; }
        }

        public override void PacketReceived(IDataPacket packet, IChannel sender)
        {
            if (packet is FileTransferInitiatePacket)
            {
                SendSequence(
                    SequenceContext.CreateFromPacket(
                        new FileTransferPacket {File = File.ReadAllBytes(Assembly.GetExecutingAssembly().Location)},
                        1024));
                CloseOperation();
            }
        }

        public override void PacketSent(IDataPacket packet, IChannel targetChannel)
        {

        }

        public override void SequenceFragmentReceived(SequenceFragmentInfo fragmentInfo)
        {
            
        }

        public override void Disconnected()
        {

        }

    }

    public class PingPacket : IDataPacket
    {
        public int PacketId { get { return 1; } }
        public void SerializeTo(Stream stream)
        {
            stream.Write(BitConverter.GetBytes(PacketId), 0, sizeof(int));
        }

        public void DeserializeFrom(Stream stream)
        {

        }
    }

    public class FileTransferInitiatePacket : IDataPacket
    {
        public int PacketId { get { return 2; } }
        public void SerializeTo(Stream stream)
        {
            var bw = new BinaryWriter(stream);
            bw.Write(PacketId);
        }

        public void DeserializeFrom(Stream stream)
        {
        }
    }

    public class TestPacket : IDataPacket
    {
        public int PacketId { get { return 69; } }
        public byte[] Data { get; set; }
        public void SerializeTo(Stream stream)
        {
            var bw = new BinaryWriter(stream);
            bw.Write(PacketId);
            Data = File.ReadAllBytes(@"C:\Users\mattias\Documents\Visual Studio 2013\Projects\ScireNET\ScireNET.Server.GUI\bin\Debug\ScireNET.Server.GUI.exe");
            bw.Write(Data);
        }

        public void DeserializeFrom(Stream stream)
        {
 
        }
    }

    public class FileTransferPacket : IDataPacket
    {
        public int PacketId { get { return 1; } }
        public int Size { get; set; }
        public byte[] File { get; set; }
        public void SerializeTo(Stream stream)
        {
            var bw = new BinaryWriter(stream);
            bw.Write(PacketId);
            bw.Write(File.Length);
            bw.Write(File);
        }

        public void DeserializeFrom(Stream stream)
        {
            var br = new BinaryReader(stream);
            Size = br.ReadInt32();
            File = br.ReadBytes(Size);
        }
    }

    public class StandardPacketProcessor : IPacketProcessor
    {
        private Dictionary<int, Type> _packetTable = new Dictionary<int, Type>
        {
            {1, typeof (PingPacket)},
            {2, typeof(FileTransferInitiatePacket)}
        };

        public Dictionary<int, Type> PacketTable
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public byte[] ProcessRawData(byte[] rawData)
        {
            return rawData;
        }

        public IDataPacket ParsePacket(Stream data)
        {
            var br = new BinaryReader(data);
            var id = br.ReadInt32();
            var packet = (IDataPacket)Activator.CreateInstance(_packetTable[id]);
            return packet;
        }
    }
    
    class Program
    {
        static void Main(string[] args)
        {
            Example2();
        }

        public static void Example1()
        {
            Thread.Sleep(2000);

            var client = new UNetClient(new StandardPacketProcessor());
            client.EnsurePacketIntegrity = true;
            client.PacketIntegrityHash = Security.PacketIntegrityHash.Sha256;
            client.Connect("127.0.0.1", 1000);

            Console.ReadLine();
        }

        public static void Example2()
        {
            Thread.Sleep(2000);

            var client = new UNetClient(new StandardPacketProcessor());
            client.EnsurePacketIntegrity = true;
            client.PacketIntegrityHash = Security.PacketIntegrityHash.Sha256;
            client.Connect("127.0.0.1", 1000);

            Console.ReadLine();
        }


        public static void Example3()
        {
            Thread.Sleep(2000);

            var client = new UNetClient(new StandardPacketProcessor());
            client.RegisterOperation<FileTransferOperation>();
            client.Connect("127.0.0.1", 1000);
            Console.ReadLine();
        }

        public static void Example4()
        {
            Thread.Sleep(2000);

            var client = new UNetClient(new StandardPacketProcessor());

            client.OnPacketReceived += (sender, e) =>
            {
                if (e.Packet.PacketId == 1)
                {
                    Console.WriteLine("Received pingpacket to client @ channel {0}", e.Channel.Id);
                    client.Send(new PingPacket(), e.Channel);
                }
            };
            client.Connect("127.0.0.1", 1000);


            Console.ReadLine();
        }

        public static void Example5()
        {
            var client = new UNetClient(new StandardPacketProcessor());
            Thread.Sleep(2000);
            client.Connect("127.0.0.1", 1000);
            client.OnPacketReceived += (o, e) =>
            {
                Console.WriteLine(e.Packet);
            };

        }
    }
}
