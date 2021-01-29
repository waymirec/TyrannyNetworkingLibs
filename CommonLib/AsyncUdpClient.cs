using System;
using System.Net;
using System.Net.Sockets;
using NLog;

namespace Tyranny.Networking
{
    public class AsyncUdpClient
    {
        public Guid Id { get; set; }
        public int Port { get; private set; }

        public event EventHandler<AsyncUdpSocketEventArgs> OnConnected;
        public event EventHandler<AsyncUdpSocketEventArgs> OnDisconnected;
        public event EventHandler<AsyncUdpPacketEventArgs> OnDataReceived;
        public UdpClient UdpClient { get; private set; }

        private readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private bool running = false;
        
        public AsyncUdpClient(int port)
        {
            Id = Guid.NewGuid();
            Port = port;
            UdpClient = new UdpClient(port);
        }

        public void Start()
        {
            running = true;
            ReceiveAsync();
            OnConnected?.Invoke(this, new AsyncUdpSocketEventArgs {Client = this});
        }

        public void Stop()
        {
            running = false;
            UdpClient.Close();
        }
        
        public void JoinMulticastGroup(string host)
        {
            UdpClient.JoinMulticastGroup(IPAddress.Parse(host));
        }
        
        public async void Send(PacketWriter packet, string host)
        {
            var data = packet.ToBytes();
            var bytesSent = await UdpClient.SendAsync(data, data.Length, host, Port);
            if (bytesSent != data.Length)
            { 
                logger.Warn($"byte-count mismatch: {bytesSent}/{data.Length}");
            }
        }
        
        async void ReceiveAsync()
        {
            while (running)
            {
                try
                {
                    var result = await UdpClient.ReceiveAsync();
                    var packet = new PacketReader(result.Buffer);
                    if (packet.Opcode == TyrannyOpcode.NoOp) return;
                    
                    if (OnDataReceived == null)
                        logger.Warn($"No handler found for opcode: {packet.Opcode}");

                    OnDataReceived?.Invoke(this, new AsyncUdpPacketEventArgs
                    {
                        Client = this, 
                        Packet = packet,
                        Sender = result.RemoteEndPoint
                    });
                }
                catch (SocketException e)
                {
                    running = false;
                    logger.Debug(e);
                }
            }
            logger.Debug($"Client {Id} disconnected");
            OnDisconnected?.Invoke(this, new AsyncUdpSocketEventArgs{Client = this});
        }
    }
    
    public class AsyncUdpPacketEventArgs : EventArgs
    {
        public AsyncUdpClient Client { get; set; }
        public PacketReader Packet { get; set; }
        public IPEndPoint Sender { get; set; }
    }
    
    public class AsyncUdpSocketEventArgs : EventArgs
    {
        public AsyncUdpClient Client { get; set; }
    }
}