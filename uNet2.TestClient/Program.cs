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
                SendPacket(new FileTransferPacket() {Size = 5});
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

    public class FileTransferInitiatePacket : IDataPacket
    {
        public int PacketId { get { return 2; } }
        public void SerializeTo(Stream stream)
        {
            var bw = new BinaryWriter(stream);
            bw.Write(PacketId);
            var buff =
                File.ReadAllBytes(
                    @"C:\BcelEditor.jar");

            bw.Write(buff);
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
            Data = File.ReadAllBytes(@"G:\Steam\SteamUI.dll");
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
            bw.Write(Size);
        }

        public void DeserializeFrom(Stream stream)
        {
            var br = new BinaryReader(stream);
            Size = br.ReadInt32();
        }
    }

    public class StandardPacketProcessor : IPacketProcessor
    {
        private Dictionary<int, Type> _packetTable = new Dictionary<int, Type>
        {
            {1, typeof (FileTransferPacket)},
            {2, typeof(FileTransferInitiatePacket)}
        };

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
            client.Connect("127.0.0.1", 1000);

            Console.ReadLine();
        }

        public static void Example2()
        {
            Thread.Sleep(2000);

            var client = new UNetClient(new StandardPacketProcessor());
            client.Connect("127.0.0.1", 1000);

            var sequenceContext = SequenceContext.CreateFromPacket(new TestPacket(), 20000);
            client.SendSequence(sequenceContext);
            Console.ReadLine();
        }
    }
}
