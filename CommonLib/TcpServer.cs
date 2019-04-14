using NLog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Tyranny.Networking
{
    public class TcpServer
    {
        public event EventHandler<SocketEventArgs> OnClientConnected;

        public string LocalAddress { get; private set; }
        public int Port { get; private set; }

        public bool Running { get; private set; }

        protected Dictionary<string, AsyncTcpClient> clients = new Dictionary<string, AsyncTcpClient>();

        private Logger logger = LogManager.GetCurrentClassLogger();
        private TcpListener listener;

        public TcpServer(string localAddress, int port)
        {
            LocalAddress = localAddress;
            Port = port;
        }

        public async void Start()
        {
            listener = new TcpListener(IPAddress.Parse(LocalAddress), Port);
            listener.Start();
            Running = true;
            logger.Info($"Server listening at {LocalAddress}:{Port}");

            await Task.Run(() =>
            {
                while (Running)
                {
                    if (!listener.Pending())
                    {
                        Thread.Sleep(250);
                        continue;
                    }

                    System.Net.Sockets.TcpClient client = listener.AcceptTcpClient();
                    AsyncTcpClient asyncTcpClient = new AsyncTcpClient(client);
                    clients[asyncTcpClient.Id] = asyncTcpClient;
                    OnClientConnected?.Invoke(this, new SocketEventArgs(asyncTcpClient));
                }
            });
        }

        public async void Stop()
        {
            Running = false;
            await Task.Run(() =>
            {
                try
                {
                    foreach (var item in clients)
                    {
                        item.Value.Close();
                    }
                }
                catch (Exception exception)
                {
                    logger.Info(exception, "Encountered exception while stopping server.");
                }

            });
        }
    }
}
