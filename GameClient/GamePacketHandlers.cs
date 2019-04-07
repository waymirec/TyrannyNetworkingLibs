using NLog;

namespace Tyranny.Networking
{
    public class GamePacketHandlers : IPacketHandler
    {
        private static Logger logger = NLog.LogManager.GetCurrentClassLogger();

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
    }
}
