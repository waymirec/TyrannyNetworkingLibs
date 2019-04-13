using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tyranny.Networking;

namespace Tyranny.WorldServer
{
    class WorldServerClient
    {
        public string Id => client.Id;
        private AsyncTcpClient client;
        private Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private WorldServer worldServer;

        public WorldServerClient(WorldServer worldServer, AsyncTcpClient client)
        {
            this.worldServer = worldServer;
            this.client = client;
            client.OnDisconnected += OnDisconnected;
            client.OnDataReceived += OnDataReceived;
            client.ReadAsync();
        }

        public void OnDisconnected(object source, NetworkEventArgs args)
        {
            logger.Info($"Client {client.Id} disconnected");

        }

        public void OnDataReceived(object source, NetworkEventArgs args)
        {
            logger.Info($"Received packet from client {client.Id}");
        }
    }
}
