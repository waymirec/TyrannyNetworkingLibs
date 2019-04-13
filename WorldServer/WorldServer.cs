using NLog;
using System;
using System.Collections.Generic;
using Tyranny.Networking;

namespace Tyranny.WorldServer
{
    class WorldServer
    {
        public string LocalAddress { get; private set; }
        public int Port { get; private set; }

        private Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private TcpServer server;
        private Dictionary<string, WorldServerClient> clients = new Dictionary<string, WorldServerClient>();

        public WorldServer(string localAddress, int port)
        {
            server = new TcpServer(localAddress, port);
            server.OnClientConnected += OnClientConnected;
        }

        public void Start()
        {
            server.Start();
        }

        public void Stop()
        {
            server.Stop();
        }

        public void OnClientConnected(object source, ClientEventArgs args)
        {
            AsyncTcpClient client = args.Client;
            logger.Info($"Client {client.Id} connected");
            WorldServerClient worldServerClient = new WorldServerClient(this, client);
            clients[client.Id] = worldServerClient;
        }

        public void ClientDisconnected(WorldServerClient client)
        {
            logger.Info($"Client {client.Id} disconnected");
            clients.Remove(client.Id);
        }
    }
}
