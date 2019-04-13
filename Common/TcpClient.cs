using NLog;
using System;
using System.IO;
using System.Net.Sockets;

namespace Tyranny.Networking
{
    public class TcpClient
    {
        public String Host { get; private set; }
        public int Port { get; private set; }
        public bool Connected { get { return client.Connected; } }

        private Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private System.Net.Sockets.TcpClient client;

        private byte[] buffer = new byte[8096];
        private int bufferPos = 0;

        public TcpClient()
        {
            client = new System.Net.Sockets.TcpClient();
            client.ReceiveTimeout = 1000;

        }

        public bool Connect(String host, int port)
        {
            Host = host;
            Port = port;

            try
            {
                logger.Debug($"Connecting to {Host}:{Port}");
                client.Connect(host, port);

                if (client.Connected)
                {
                    logger.Debug($"Connected to {Host}:{Port}");
                    return true;
                }
                else
                {
                    logger.Debug($"Connect failed to {Host}:{Port}");
                    return false;
                }
            }
            catch(SocketException)
            {
                logger.Debug($"Error connecting to {Host}:{Port}");
                return false;
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
                }
            }
        }

        public bool Read(out PacketReader packet)
        {
            int read = client.GetStream().Read(buffer, bufferPos, buffer.Length - bufferPos);
            if (read == 0)
            {
                packet = null;
                return false;
            }

            bufferPos += read;

            byte[] header = new byte[4];
            Array.Copy(buffer, 0, header, 0, 4);
            if (BitConverter.IsLittleEndian) Array.Reverse(header);
            int len = BitConverter.ToInt32(header, 0);

            if (bufferPos < len)
            {
                return Read(out packet);
            }

            byte[] data = new byte[len];
            Array.Copy(buffer, 4, data, 0, len);

            int extra = bufferPos - (len + 4);
            Array.Copy(buffer, len + 4, buffer, 0, extra);
            bufferPos = extra;

            packet = new PacketReader(data);
            return true;
        }
    }
}
