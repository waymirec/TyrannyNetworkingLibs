using NLog;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Tyranny.Networking
{
    public class AsyncTcpClient
    {
        public String Host { get; private set; }
        public int Port { get; private set; }

        public event EventHandler<NetworkAsyncEventArgs> OnConnected;
        public event EventHandler<NetworkAsyncEventArgs> OnConnectFailed;
        public event EventHandler<NetworkAsyncEventArgs> OnDisconnected;
        public event EventHandler<NetworkAsyncEventArgs> OnDataReceived;

        private Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private System.Net.Sockets.TcpClient client;

        private byte[] buffer = new byte[8096];
        private int bufferPos = 0;

        public AsyncTcpClient()
        {
            client = new System.Net.Sockets.TcpClient();
            client.ReceiveTimeout = 1000;

        }

        public async void Connect(String host, int port)
        {
            Host = host;
            Port = port;

            logger.Debug($"Connecting to {Host}:{Port}");
            await client.ConnectAsync(host, port);

            NetworkAsyncEventArgs args = new NetworkAsyncEventArgs();
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

            while(client.Connected)
            {
                Read();
            }
        }

        public void Send(PacketWriter packet)
        {
            if (client.Connected)
            {
                try
                {
                    byte[] data = packet.ToBytes();
                    client.GetStream().Write(data, 0, data.Length);
                } 
                catch(IOException)
                {
                    logger.Error($"Error writing to socket to {Host}:{Port}");
                    client.Close();
                    OnDisconnected?.Invoke(this, new NetworkAsyncEventArgs(this));
                }
            }
        }

        private void Read()
        {
            while (client.Connected)
            {
                try
                {
                    int read = client.GetStream().Read(buffer, bufferPos, buffer.Length - bufferPos);
                    if (read == 0)
                    {
                        Thread.Sleep(250);
                        continue;
                    }

                    bufferPos += read;

                    byte[] header = new byte[4];
                    Array.Copy(buffer, 0, header, 0, 4);
                    if (BitConverter.IsLittleEndian) Array.Reverse(header);
                    int len = BitConverter.ToInt32(header);

                    if (bufferPos < len)
                    {
                        Read();
                    }

                    byte[] data = new byte[len];
                    Array.Copy(buffer, 4, data, 0, len);

                    NetworkAsyncEventArgs args = new NetworkAsyncEventArgs();
                    args.Client = this;
                    args.Packet = new PacketReader(data);
                    OnDataReceived?.Invoke(this, args);

                    int extra = bufferPos - (len + 4);
                    Array.Copy(buffer, len + 4, buffer, 0, extra);
                    bufferPos = extra;
                }
                catch(IOException)
                {
                    logger.Error($"Error reading from socket to {Host}:{Port}");
                    client.Close();
                    OnDisconnected?.Invoke(this, new NetworkAsyncEventArgs(this));
                }
            }

            OnDisconnected?.Invoke(this, new NetworkAsyncEventArgs(this));
        }
    }

    public class NetworkAsyncEventArgs : EventArgs
    {
        public AsyncTcpClient Client { get; set; }
        public PacketReader Packet { get; set; }

        public NetworkAsyncEventArgs()
        {

        }

        public NetworkAsyncEventArgs(AsyncTcpClient client)
        {
            Client = client;
        }
    }
}
