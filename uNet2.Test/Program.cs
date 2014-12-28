using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using uNet2.Channel;
using uNet2.Packet;
using uNet2.Packet.Events;
using uNet2.SocketOperation;

namespace uNet2.Test
{
    public class FileTransferOperation : SocketOperationBase
    {
        public override int OperationId
        {
            get { return 1; }
        }

        public override void PacketReceived(IDataPacket packet, IChannel sender)
        {
            if (packet is FileTransferPacket)
            {
                Debug.Print((packet as FileTransferPacket).Size.ToString());
                CloseOperation();
            }
        }

        public override void PacketSent(IDataPacket packet, IChannel targetChannel)
        {
          
        }

        public override void SequenceFragmentReceived(SequenceFragmentInfo fragmentInfo)
        {
            float receivedPercentage = ((fragmentInfo.CurrentReceivedSize*100f)/fragmentInfo.ExpectedCompleteSize);
            double receiveSpeed =
                Math.Round(
                    (fragmentInfo.CurrentReceivedSize/(DateTime.Now - fragmentInfo.SessionStart).TotalSeconds)/1048576,
                    2);
            Console.WriteLine("Receiving file: {0}% @ {1} mb/s with GUID: {2}", receivedPercentage, receiveSpeed, OperationGuid);
        }

        public override void Disconnected()
        {
           
        }
    }

    public class StandardPacketProcessor : IPacketProcessor
    {
        private Dictionary<int, Type> _packetTable = new Dictionary<int, Type>
        {
            {1, typeof (PingPacket)},
            {2, typeof(FileTransferInitiatePacket)}
        };

        public byte[] ProcessRawData(byte[] rawData)
        {
            return rawData;
        }

        public IDataPacket ParsePacket(Stream data)
        {
            var br = new BinaryReader(data);
            var packet = (IDataPacket)Activator.CreateInstance(_packetTable[br.ReadInt32()]);
            return packet;
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
            bw.Write(File);
        }

        public void DeserializeFrom(Stream stream)
        {
            var br = new BinaryReader(stream);
            Size = br.ReadInt32();
            File = br.ReadBytes(Size);
        }
    }

    public class PingPacket : IDataPacket
    {
        public int PacketId { get { return 1; } }
        public void SerializeTo(Stream stream)
        {
            stream.Write(BitConverter.GetBytes(PacketId), 0, sizeof (int));
            stream.Write(BitConverter.GetBytes(1000), 0, sizeof (int));
        }

        public void DeserializeFrom(Stream stream)
        {

        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Example4();
        }

        // Simple example on how multiple channels can be handled
        public static void Example1()
        {
            var srv = new UNetServer();

            // Register global events across all channels
            srv.OnPeerConnected +=
                (sender, e) => Console.WriteLine("A peer has connected to channel: {0} with GUID: {1}", e.Channel.Name,
                    e.Peer.Identity.Guid);
            srv.OnChannelCreated +=
                (sender, e) => Console.WriteLine("A channel was created with name: {0}", e.Channel.Name);

            // Create a main channel for theserver
            var mainChannel = srv.CreateChannel<TcpServerChannel>();

            // Register events on main channel exclusively
            mainChannel.OnPeerConnected +=
                (sender, e) =>
                    Console.WriteLine("A peer has connected to the mainchannel with GUID: {0}", e.Peer.Identity.Guid);
            mainChannel.OnPacketReceived +=
                (sender, e) =>
                    Console.WriteLine("A packet was received on the mainchannel from peer with GUID: {0}",
                        e.Peer.Identity.Guid);

            // Initialize the server and start listening on the main channel
            srv.Initialize(mainChannel);

            // Create and add a second channel to the server
            srv.CreateAndAddChannel<TcpServerChannel>(ch => ch.Name = "MySecondChannel");
            Console.ReadLine();
        }

        // Simple example on how you can transfer a large file and track the progress
        public static void Example2()
        {
            var srv = new UNetServer();
            var mainChannel = srv.CreateChannel<TcpServerChannel>();
            mainChannel.OnSequenceFragmentReceived += (sender, e) =>
            {
                float receivedPercentage = ((e.CurrentReceivedSize * 100f) / e.ExpectedCompleteSize);
                double receiveSpeed =
                    Math.Round((e.CurrentReceivedSize / (DateTime.Now - e.SessionStart).TotalSeconds) / 1048576, 2);
                Console.WriteLine("Receiving file: {0}% @ {1} mb/s", receivedPercentage, receiveSpeed);
            };
            srv.Initialize(mainChannel);

            Console.ReadLine();
        }

        private static int count = 0;
        private static List<FileTransferOperation> _operations = new List<FileTransferOperation>();
        public static void Example3()
        {
            var srv = new UNetServer();
            var mainChannel = srv.CreateChannel<TcpServerChannel>();
            var secondChannel = srv.CreateChannel<TcpServerChannel>();
            

            mainChannel.OnPeerConnected += (sender, e) =>
            {
                _operations.Add(mainChannel.RegisterOperation<FileTransferOperation>(e.Peer.Identity.Guid));
                srv.AddPeerToChannel(secondChannel, p => p.Identity.Guid == e.Peer.Identity.Guid);
                count++;
                Console.WriteLine("Main channel connect");
            };
            secondChannel.OnPeerConnected += (sender, e) =>
            {
                _operations.Add(secondChannel.RegisterOperation<FileTransferOperation>(e.Peer.Identity.Guid));
                count++;
                Console.WriteLine("second channel connect");
            };
            secondChannel.OnPacketReceived += (sender, e) =>
            {
                Console.WriteLine("Received pingpacket from peer: {0} @ channel {1}", e.Peer.Identity.Guid, e.Channel.Id);
            };

            srv.Initialize(mainChannel);
            srv.AddChannel(secondChannel);

            while (count != 2)
                Thread.Sleep(100);


            _operations[1].SendPacket(new FileTransferInitiatePacket());
            Console.ReadLine();
        }

        public static void Example4()
        {
            var srv = new UNetServer();
            var mainChannel = srv.CreateChannel<TcpServerChannel>(new StandardPacketProcessor());
            var secondChannel = srv.CreateChannel<TcpServerChannel>(new StandardPacketProcessor());

            mainChannel.OnPeerConnected += (sender, e) => srv.AddPeerToChannel(secondChannel, p => p.Identity.Equals(e.Peer.Identity));

            secondChannel.OnPeerConnected += (sender, e) =>
            {
                secondChannel.OnPacketReceived += (sender2, e2) =>
                {
                    if (e2.Packet.PacketId == 1)
                        Console.WriteLine("Received ping packet from {0} @ second channel", e2.Peer.Identity.Guid);
                };
                Console.WriteLine("Peer has connected to second channel");
                secondChannel.Send(new PingPacket(), e.Peer.Identity.Guid);
            };


            srv.Initialize(mainChannel);
            srv.AddChannel(secondChannel);

            Console.ReadLine();
        }
    }
}
