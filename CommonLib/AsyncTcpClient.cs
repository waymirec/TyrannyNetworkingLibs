using NLog;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Tyranny.Networking
{
    public class AsyncTcpClient
    {
        public string Id => Guid.NewGuid().ToString().Replace("-", "");
        public string Host { get; private set; }
        public int Port { get; private set; }

        public event EventHandler<NetworkEventArgs> OnConnected;
        public event EventHandler<NetworkEventArgs> OnConnectFailed;
        public event EventHandler<NetworkEventArgs> OnDisconnected;
        public event EventHandler<NetworkEventArgs> OnDataReceived;

        public bool Connected {
            get
            {
                return client.Connected;
            }
        }

        private Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private System.Net.Sockets.TcpClient client;

        private byte[] buffer = new byte[8096];
        private int bufferPos = 0;

        public AsyncTcpClient()
        {
            client = new System.Net.Sockets.TcpClient();

        }

        public AsyncTcpClient(System.Net.Sockets.TcpClient client)
        {
            this.client = client;
        }

        public async void Connect(String host, int port)
        {
            Host = host;
            Port = port;

            logger.Debug($"Connecting to {Host}:{Port}");
            await client.ConnectAsync(host, port);

            NetworkEventArgs args = new NetworkEventArgs();
            args.Client = this;

            if(client.Connected)
            {
                logger.Debug($"Connected to {Host}:{Port}");
                ((NetworkStream)client.GetStream()).ReadTimeout = 1000;
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
            client.Close();
        }

        public async void Send(PacketWriter packet)
        {
            if (client.Connected)
            {
                try
                {
                    byte[] data = packet.ToBytes();
                    await client.GetStream().WriteAsync(data, 0, data.Length);
                } 
                catch(IOException)
                {
                    logger.Error($"Error writing to socket to {Host}:{Port}");
                    client.Close();
                    OnDisconnected?.Invoke(this, new NetworkEventArgs(this));
                }
            }
        }

        public async void ReadAsync()
        {
            while (client.Connected)
            {
                NetworkStream stream = client.GetStream();
                if (!stream.CanRead)// || !stream.DataAvailable)
                {
                    Thread.Sleep(250);
                    continue;
                }
                try
                {
                    int read = await client.GetStream().ReadAsync(buffer, bufferPos, buffer.Length - bufferPos);
                    //int read = client.GetStream().Read(buffer, bufferPos, buffer.Length - bufferPos);
                    if (read == 0)
                    {
                        Thread.Sleep(250);
                        continue;
                    }

                    bufferPos += read;

                    byte[] header = new byte[4];
                    Array.Copy(buffer, 0, header, 0, 4);
                    if (BitConverter.IsLittleEndian) Array.Reverse(header);
                    int len = BitConverter.ToInt32(header, 0);

                    if (bufferPos >= len)
                    {
                        byte[] data = new byte[len];
                        Array.Copy(buffer, 4, data, 0, len);

                        NetworkEventArgs args = new NetworkEventArgs();
                        args.Client = this;
                        args.Packet = new PacketReader(data);
                        OnDataReceived?.Invoke(this, args);

                        int extra = bufferPos - (len + 4);
                        Array.Copy(buffer, len + 4, buffer, 0, extra);
                        bufferPos = extra;
                    }
                }
                catch(IOException ex)
                {
                    if (!client.Connected)
                    {
                        logger.Warn($"Socket Exception: {ex.ToString()}");
                        break;
                    }
                    Thread.Sleep(125);
                }
            }
            logger.Info("Stopping read, client disconnected.");
            OnDisconnected?.Invoke(this, new NetworkEventArgs(this));
        }
    }

    public class NetworkEventArgs : EventArgs
    {
        public AsyncTcpClient Client { get; set; }
        public PacketReader Packet { get; set; }

        public NetworkEventArgs()
        {

        }

        public NetworkEventArgs(AsyncTcpClient client)
        {
            Client = client;
        }
    }
}
