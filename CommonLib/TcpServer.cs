using NLog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Tyranny.Networking
{
    public class TcpServer<TOpcode> where TOpcode : Enum
    {
        public event EventHandler<TcpSocketEventArgs<TOpcode>> OnClientConnected;

        public string LocalAddress { get; private set; }
        public int Port { get; private set; }

        public bool Running { get; private set; }

        protected Dictionary<Guid, AsyncTcpClient<TOpcode>> clients = new Dictionary<Guid, AsyncTcpClient<TOpcode>>();

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

                    TcpClient client = listener.AcceptTcpClient();
                    AsyncTcpClient<TOpcode> asyncTcpClient = new AsyncTcpClient<TOpcode>(client);
                    clients[asyncTcpClient.Id] = asyncTcpClient;
                    OnClientConnected?.Invoke(this, new TcpSocketEventArgs<TOpcode>(asyncTcpClient));
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
