using NLog;
using System;
using System.IO;
using System.Threading;

namespace Tyranny.Networking
{
    public class AsyncTcpClient<TOpcode> where TOpcode : Enum
    {
        public Guid Id { get; set; }
        public string Host { get; private set; }
        public int Port { get; private set; }

        public event EventHandler<TcpSocketEventArgs<TOpcode>> OnConnected;
        public event EventHandler<TcpSocketEventArgs<TOpcode>> OnConnectFailed;
        public event EventHandler<TcpSocketEventArgs<TOpcode>> OnDisconnected;
        public event EventHandler<TcpPacketEventArgs<TOpcode>> OnDataReceived;

        public bool Connected => TcpClient.Connected;
        public System.Net.Sockets.TcpClient TcpClient { get; private set; }

        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly byte[] buffer = new byte[8096];
        
        private int bufferPos = 0;

        public AsyncTcpClient()
        {
            Id = Guid.NewGuid();
            TcpClient = new System.Net.Sockets.TcpClient();
        }

        public AsyncTcpClient(System.Net.Sockets.TcpClient tcpClient)
        {
            Id = Guid.NewGuid();
            TcpClient = tcpClient;
        }

        public async void Connect(String host, int port)
        {
            Host = host;
            Port = port;

            logger.Debug($"Connecting to {Host}:{Port}");
            await TcpClient.ConnectAsync(host, port);

            var args = new TcpSocketEventArgs<TOpcode> {TcpClient = this};
            if(TcpClient.Connected)
            {
                logger.Debug($"Connected to {Host}:{Port}");
                TcpClient.GetStream().ReadTimeout = 1000;
                OnConnected?.Invoke(this, args);
            }
            else
            {
                logger.Debug($"Connect failed to {Host}:{Port}");
                OnConnectFailed?.Invoke(this, args);
            }

            ReadAsync();
        }

        public void Close()
        {
            TcpClient.Close();
        }

        public async void Send(PacketWriter<TOpcode> packet)
        {
            if (TcpClient.Connected)
            {
                try
                {
                    byte[] data = packet.ToBytes();
                    await TcpClient.GetStream().WriteAsync(data, 0, data.Length);
                } 
                catch(IOException)
                {
                    logger.Error($"Error writing to socket to {Host}:{Port}");
                    TcpClient.Close();
                    OnDisconnected?.Invoke(this, new TcpSocketEventArgs<TOpcode>(this));
                }
            }
        }

        async void ReadAsync()
        {
            while (TcpClient.Connected)
            {
                try
                {
                    int read = await TcpClient.GetStream().ReadAsync(buffer, bufferPos, buffer.Length - bufferPos);
                    if (read <= 0)
                    {
                        Thread.Sleep(250);
                        continue;
                    }
                    bufferPos += read;

                    if(bufferPos < 3)
                    {
                        Thread.Sleep(250);
                        continue;
                    }

                    byte[] header = new byte[4];
                    Array.Copy(buffer, 0, header, 0, 4);
                    if (BitConverter.IsLittleEndian) Array.Reverse(header);
                    int len = BitConverter.ToInt32(header, 0);

                    if (len > 0 && bufferPos >= len)
                    {
                        byte[] data = new byte[len];
                        Array.Copy(buffer, 4, data, 0, len);

                        var packet = new PacketReader<TOpcode>(data);
                        if (OnDataReceived == null)
                            logger.Warn($"No handler found for opcode: {packet.Opcode}");
                         
                        OnDataReceived?.Invoke(this, new TcpPacketEventArgs<TOpcode>{TcpClient = this, Packet = packet});

                        int extra = bufferPos - (len + 4);
                        Array.Copy(buffer, len + 4, buffer, 0, extra);
                        bufferPos = extra;
                    }
                }
                catch(IOException)
                {
                    if (TcpClient.Connected)
                        Thread.Sleep(125);
                }
            }
            logger.Debug($"Client {Id} disconnected");
            OnDisconnected?.Invoke(this, new TcpSocketEventArgs<TOpcode>(this));
        }
    }

    public class TcpPacketEventArgs<TOpcode> : EventArgs where TOpcode : Enum
    {
        public AsyncTcpClient<TOpcode> TcpClient { get; set; }
        public PacketReader<TOpcode> Packet { get; set; }

        public TcpPacketEventArgs()
        {

        }

        public TcpPacketEventArgs(AsyncTcpClient<TOpcode> tcpClient)
        {
            TcpClient = tcpClient;
        }
    }

    public class TcpSocketEventArgs<TOpcode> : EventArgs where TOpcode : Enum
    {
        public AsyncTcpClient<TOpcode> TcpClient { get; set; }

        public TcpSocketEventArgs()
        {

        }

        public TcpSocketEventArgs(AsyncTcpClient<TOpcode> tcpClient)
        {
            TcpClient = tcpClient;
        }
    }
}
