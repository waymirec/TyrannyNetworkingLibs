using NLog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tyranny.Networking
{
    public class GameClient
    {
        public String Host { get; private set; }
        public int Port { get; private set; }
        public String Username { get; private set; }
        public byte[] AuthToken { get; private set; }

        private Logger logger = LogManager.GetCurrentClassLogger();
        private AsyncTcpClient tcpClient;

        private Dictionary<TyrannyOpcode, Handler> packetHandlers;

        public GameClient(String host, int port)
        {
            Host = host;
            Port = port;

            tcpClient = new AsyncTcpClient();
            tcpClient.OnConnected += OnConnected;
            tcpClient.OnConnectFailed += OnConnectFailed;
            tcpClient.OnDisconnected += OnDisconnected;
            tcpClient.OnDataReceived += OnDataReceived;

            packetHandlers = PacketHandler.Load();
        }

        public void Connect(String username, byte[] authToken)
        {
            Username = username;
            AuthToken = authToken;

            tcpClient.Connect(Host, Port);

        }
        public void OnConnected(object source, NetworkEventArgs args)
        {
            PacketWriter ident = new PacketWriter(TyrannyOpcode.GameIdent);
            ident.Write(Username);
            ident.Write((short)AuthToken.Length);
            ident.Write(AuthToken);
            tcpClient.Send(ident);
        }

        public void OnConnectFailed(object source, NetworkEventArgs args)
        {
            logger.Error($"Failed to connect to {Host}:{Port}");
        }

        public void OnDisconnected(object source, NetworkEventArgs args)
        {
            logger.Info($"Disconnected from {Host}:{Port}");
        }

        public void OnDataReceived(object source, NetworkEventArgs args)
        {
            TyrannyOpcode opcode = args.Packet.Opcode;
            packetHandlers[opcode]?.Invoke(args.Packet, args.Client);
        }
    }
}
