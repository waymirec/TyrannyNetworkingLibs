using NLog;
using System;
using Tyranny.Networking;

namespace GameClientLib.PacketHandlers
{
    class Login : IPacketHandler
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

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

        [PacketHandler(TyrannyOpcode.EnterWorld)]
        public static void HandleEnterWorld(PacketReader packetIn, AsyncTcpClient client)
        {
            Guid guid = new Guid(packetIn.ReadBytes(16));
            float x = packetIn.ReadSingle();
            float y = packetIn.ReadSingle();
            float z = packetIn.ReadSingle();

            logger.Debug($"Enter World: {x.ToString("F2")}, {y.ToString("F2")}, {z.ToString("F2")}");
            client.Id = guid;

            //handler.EnterWorld(guid, x, y, z);
        }
    }
}
