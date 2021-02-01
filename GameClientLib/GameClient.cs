using NLog;
using System;
using System.Collections.Generic;
using CommonLib;
using Tyranny.Networking;
using Tyranny.Networking.Events;

namespace Tyranny.GameClient
{
    public class GameClient 
    {
        public delegate void Handler(PacketReader packetIn, AsyncTcpClient tcpClient);

        public Guid Id { get => tcpClient.Id;  }  
        public String Host { get; private set; }
        public int Port { get; private set; }
        public String Username { get; private set; }
        public byte[] AuthToken { get; private set; }

        public bool Connected => tcpClient?.Connected ?? false;
 
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private AsyncTcpClient tcpClient;
        private GamePacketHandler handler;

        private Dictionary<TyrannyOpcode, Handler> packetHandlers;

        public GameClient(String host, int port, GamePacketHandler handler)
        {
            Host = host;
            Port = port;

            this.handler = handler;

            tcpClient = new AsyncTcpClient();
            tcpClient.OnConnected += OnConnected;
            tcpClient.OnConnectFailed += OnConnectFailed;
            tcpClient.OnDisconnected += OnDisconnected;
            tcpClient.OnDataReceived += OnDataReceived;

            packetHandlers = new GamePacketHandlerLoader().Load<Handler>(this);
        }

        void OnDestroy()
        {
            Close();
        }

        public void Connect(String username, byte[] authToken)
        {
            Username = username;
            AuthToken = authToken;

            tcpClient.Connect(Host, Port);

        }

        public void Close()
        {
            tcpClient.Close();
        }

        public void Send(PacketWriter packetOut)
        {
            tcpClient.Send(packetOut);
        }

        public void OnConnected(object source, TcpSocketEventArgs args)
        {
            PacketWriter ident = new PacketWriter(TyrannyOpcode.GameIdent);
            ident.Write(Username);
            ident.Write((short)AuthToken.Length);
            ident.Write(AuthToken);
            logger.Debug("Sending ident");
            tcpClient.Send(ident);
            handler.OnLoggedIn();
        }

        public void OnConnectFailed(object source, TcpSocketEventArgs args)
        {
            logger.Error($"Failed to connect to {Host}:{Port}");
        }

        public void OnDisconnected(object source, TcpSocketEventArgs args)
        {
            logger.Info($"Disconnected from {Host}:{Port}");
        }

        public void OnDataReceived(object source, TcpPacketEventArgs args)
        {
            TyrannyOpcode opcode = args.Packet.Opcode;
            Handler handler;
            if (packetHandlers.TryGetValue(opcode, out handler))
            {
                try
                {
                    handler(args.Packet, args.TcpClient);
                }
                catch (Exception ex)
                {
                    logger.Warn(ex.ToString());
                }
            }
            else
            {
                logger.Warn($"No handler found for opcode {opcode}");
            }
        }

        [GamePacketHandler(TyrannyOpcode.Ping)]
        public void HandlePing(PacketReader packetIn, AsyncTcpClient client)
        {
            int count = packetIn.ReadInt32();
            logger.Debug($"Ping({count})");

            PacketWriter pong = new PacketWriter(TyrannyOpcode.Pong);
            pong.Write(count + 1);
            client.Send(pong);
        }

        [GamePacketHandler(TyrannyOpcode.Pong)]
        public void HandlePong(PacketReader packetIn, AsyncTcpClient client)
        {
            int count = packetIn.ReadInt32();
            logger.Debug($"Pong({count}");
        }

        [GamePacketHandler(TyrannyOpcode.EnterWorld)]
        public void HandleEnterWorld(PacketReader packetIn, AsyncTcpClient client)
        {
            Guid guid = new Guid(packetIn.ReadBytes(16));
            float x = packetIn.ReadSingle();
            float y = packetIn.ReadSingle();
            float z = packetIn.ReadSingle();

            logger.Debug($"Enter World: {x.ToString("F2")}, {y.ToString("F2")}, {z.ToString("F2")}");
            client.Id = guid;

            handler.EnterWorld(guid, x, y, z);
        }
    }
}
