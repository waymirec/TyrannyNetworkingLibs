using NLog;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Timers;

namespace Tyranny.Networking
{
    public class AsyncTcpClient
    {
        public Guid Id { get; set; }
        public string Host { get; private set; }
        public int Port { get; private set; }

        public event EventHandler<SocketEventArgs> OnConnected;
        public event EventHandler<SocketEventArgs> OnConnectFailed;
        public event EventHandler<SocketEventArgs> OnDisconnected;
        public event EventHandler<PacketEventArgs> OnDataReceived;

        public bool Connected => TcpClient.Connected;
        public System.Net.Sockets.TcpClient TcpClient { get; private set; }

        private readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly byte[] buffer = new byte[8096];
        
        private int bufferPos = 0;
        private System.Timers.Timer heartbeatTimer;

        public AsyncTcpClient()
        {
            Id = Guid.NewGuid();
            TcpClient = new System.Net.Sockets.TcpClient();
            Initialize();
        }

        public AsyncTcpClient(System.Net.Sockets.TcpClient tcpClient)
        {
            Id = Guid.NewGuid();
            TcpClient = tcpClient;
            Initialize();

        }

        ~AsyncTcpClient()
        {
            StopHeartbeat();
        }

        public async void Connect(String host, int port)
        {
            Host = host;
            Port = port;

            logger.Debug($"Connecting to {Host}:{Port}");
            await TcpClient.ConnectAsync(host, port);

            SocketEventArgs args = new SocketEventArgs();
            args.TcpClient = this;

            if(TcpClient.Connected)
            {
                logger.Debug($"Connected to {Host}:{Port}");
                ((NetworkStream)TcpClient.GetStream()).ReadTimeout = 1000;
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

        public async void Send(PacketWriter packet)
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
                    OnDisconnected?.Invoke(this, new SocketEventArgs(this));
                }
            }
        }

        public async void ReadAsync()
        {
            while (TcpClient.Connected)
            {
                NetworkStream stream = TcpClient.GetStream();
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

                        PacketEventArgs args = new PacketEventArgs();
                        args.TcpClient = this;
                        args.Packet = new PacketReader(data);
                        if (args.Packet.Opcode != TyrannyOpcode.NoOp)
                        {
                            if (OnDataReceived == null)
                                logger.Warn($"No handler found for opcode: {args.Packet.Opcode}");

                            OnDataReceived?.Invoke(this, args);
                        }

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
            OnDisconnected?.Invoke(this, new SocketEventArgs(this));
        }

        private void Initialize()
        {
            heartbeatTimer = new System.Timers.Timer(5000);
            heartbeatTimer.Elapsed += OnHeartbeatTimer;
            heartbeatTimer.Enabled = true;
        }

        private void StopHeartbeat()
        {
            heartbeatTimer.Elapsed -= OnHeartbeatTimer;
            heartbeatTimer.Enabled = false;
        }

        private void OnHeartbeatTimer(object source, ElapsedEventArgs e)
        {
            if (TcpClient.Connected)
            {
                try
                {
                    PacketWriter noop = new PacketWriter(TyrannyOpcode.NoOp);
                    noop.Write((byte)0);
                    Send(noop);
                }
                catch (Exception exception)
                {
                    logger.Debug(exception, "Exception sending heartbeat");
                }
            }
        }
    }

    public class PacketEventArgs : EventArgs
    {
        public AsyncTcpClient TcpClient { get; set; }
        public PacketReader Packet { get; set; }

        public PacketEventArgs()
        {

        }

        public PacketEventArgs(AsyncTcpClient tcpClient)
        {
            TcpClient = tcpClient;
        }
    }

    public class SocketEventArgs : EventArgs
    {
        public AsyncTcpClient TcpClient { get; set; }

        public SocketEventArgs()
        {

        }

        public SocketEventArgs(AsyncTcpClient tcpClient)
        {
            TcpClient = tcpClient;
        }
    }
}
