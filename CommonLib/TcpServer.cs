using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tyranny.Networking
{
    public class TcpServer
    {
        public event EventHandler<ClientEventArgs> OnClientConnected;

        public string LocalAddress { get; private set; }
        public int Port { get; private set; }

        protected bool Running { get; private set; }

        private Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private TcpListener listener;
        private Dictionary<string, AsyncTcpClient> clients = new Dictionary<string, AsyncTcpClient>();

        public TcpServer(string localAddress, int port)
        {
            LocalAddress = localAddress;
            Port = port;
        }

        public void Start()
        {
            listener = new TcpListener(IPAddress.Parse(LocalAddress), Port);
            listener.Start();
            Running = true;
            logger.Info($"Server listening at {LocalAddress}:{Port}");

            while(Running)
            {
                if (!listener.Pending())
                {
                    Thread.Sleep(250);
                    continue;
                }

                System.Net.Sockets.TcpClient client = listener.AcceptTcpClient();
                AsyncTcpClient asyncTcpClient = new AsyncTcpClient(client);
                clients[asyncTcpClient.Id] = asyncTcpClient;
                OnClientConnected?.Invoke(this, new ClientEventArgs(asyncTcpClient));
            }
        }

        public void Stop()
        {
            Running = false;
            try
            {
                foreach (var item in clients)
                {
                    item.Value.Close();
                }
            }
            catch(Exception exception)
            {
                logger.Info(exception, "Encountered exception while stopping server.");
            }
        }
    }

    public class ClientEventArgs : EventArgs
    {
        public AsyncTcpClient Client { get; }

        public ClientEventArgs(AsyncTcpClient client)
        {
            Client = client;
        }
    }
}
