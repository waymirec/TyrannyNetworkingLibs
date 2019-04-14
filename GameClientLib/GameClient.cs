using NLog;
using System;
using System.Collections.Generic;
using Tyranny.Networking;
using Tyranny.Networking.Events;

namespace Tyranny.GameClient
{
    public class GameClient : IPacketHandler
    {
        public String Host { get; private set; }
        public int Port { get; private set; }
        public String Username { get; private set; }
        public byte[] AuthToken { get; private set; }

        public bool Connected
        {
            get
            {
                return tcpClient.Connected;
            }
        }

        private static Logger logger = LogManager.GetCurrentClassLogger();

        private AsyncTcpClient tcpClient;
        private IGamePacketHandler handler;

        private Dictionary<TyrannyOpcode, Handler> packetHandlers;

        public GameClient(String host, int port, IGamePacketHandler handler)
        {
            Host = host;
            Port = port;

            this.handler = handler;

            tcpClient = new AsyncTcpClient();
            tcpClient.OnConnected += OnConnected;
            tcpClient.OnConnectFailed += OnConnectFailed;
            tcpClient.OnDisconnected += OnDisconnected;
            tcpClient.OnDataReceived += OnDataReceived;

            packetHandlers = PacketHandler.Load(typeof(GameClient));
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

        public void OnConnected(object source, PacketEventArgs args)
        {
            PacketWriter ident = new PacketWriter(TyrannyOpcode.GameIdent);
            ident.Write(Username);
            ident.Write((short)AuthToken.Length);
            ident.Write(AuthToken);
            logger.Debug("Sending ident");
            tcpClient.Send(ident);
            handler.OnLoggedIn();
        }

        public void OnConnectFailed(object source, PacketEventArgs args)
        {
            logger.Error($"Failed to connect to {Host}:{Port}");
        }

        public void OnDisconnected(object source, PacketEventArgs args)
        {
            logger.Info($"Disconnected from {Host}:{Port}");
        }

        public void OnDataReceived(object source, PacketEventArgs args)
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

        [PacketHandler(TyrannyOpcode.Ping)]
        public static void HandlePing(PacketReader packetIn, AsyncTcpClient client)
        {
            int count = packetIn.ReadInt32();
            logger.Debug($"Ping({count})");

            PacketWriter pong = new PacketWriter(TyrannyOpcode.Pong);
            pong.Write(count + 1);
            client.Send(pong);
        }

        [PacketHandler(TyrannyOpcode.Pong)]
        public static void HandlePong(PacketReader packetIn, AsyncTcpClient client)
        {
            int count = packetIn.ReadInt32();
            logger.Debug($"Pong({count}");
        }

        [PacketHandler(TyrannyOpcode.Move)]
        public static void HandleMove(PacketReader packetIn, AsyncTcpClient client)
        {
            Guid guid = new Guid(packetIn.ReadBytes(16));
            float x = packetIn.ReadSingle();
            float y = packetIn.ReadSingle();
            float z = packetIn.ReadSingle();

            MovementEventArgs args = new MovementEventArgs();
            args.Guid = guid;
            args.Position = new MovementEventArgs.Vector3() { X = x, Y = y, Z = z };
            //handler.OnMove(args);
        }
    }

    public interface IGamePacketHandler
    {
        void OnLoggedIn();
        void OnMove(MovementEventArgs args);
    }
}
