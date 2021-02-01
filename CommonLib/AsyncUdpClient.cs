using System;
using System.Net;
using System.Net.Sockets;
using NLog;

namespace Tyranny.Networking
{
    public class AsyncUdpClient<TOpcode> where TOpcode : Enum
    {
        public Guid Id { get; set; }
        public int Port { get; private set; }

        public event EventHandler<AsyncUdpSocketEventArgs<TOpcode>> OnConnected;
        public event EventHandler<AsyncUdpSocketEventArgs<TOpcode>> OnDisconnected;
        public event EventHandler<AsyncUdpPacketEventArgs<TOpcode>> OnDataReceived;
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
            OnConnected?.Invoke(this, new AsyncUdpSocketEventArgs<TOpcode> {Client = this});
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

        public void LeaveMulticastGroup(string host)
        {
            UdpClient.DropMulticastGroup(IPAddress.Parse(host));
        }
        
        public async void Send(PacketWriter<TOpcode> packet, string host)
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
                    var packet = new PacketReader<TOpcode>(result.Buffer);
                    
                    if (OnDataReceived == null)
                        logger.Warn($"No handler found for opcode: {packet.Opcode}");

                    OnDataReceived?.Invoke(this, new AsyncUdpPacketEventArgs<TOpcode>
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
            OnDisconnected?.Invoke(this, new AsyncUdpSocketEventArgs<TOpcode>{Client = this});
        }
    }
    
    public class AsyncUdpPacketEventArgs<TOpcode> : EventArgs where TOpcode : Enum
    {
        public AsyncUdpClient<TOpcode> Client { get; set; }
        public PacketReader<TOpcode> Packet { get; set; }
        public IPEndPoint Sender { get; set; }
    }
    
    public class AsyncUdpSocketEventArgs<TOpcode> : EventArgs where TOpcode : Enum
    {
        public AsyncUdpClient<TOpcode> Client { get; set; }
    }
}