using NLog;
using System;
using System.Security.Cryptography;
using Tyranny.GameClient;
using Tyranny.Networking;
using Tyranny.Networking.Events;

namespace Application
{
    class Program : IGamePacketHandler, IPacketHandler
    {
        public static Logger logger;
        public static SHA256 sha256 = SHA256Managed.Create();

        static void Main(string[] args)
        { 
            var config = new NLog.Config.LoggingConfiguration();
            var logConsole = new NLog.Targets.ConsoleTarget("Console");
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logConsole);
            LogManager.Configuration = config;

            logger = LogManager.GetCurrentClassLogger();
            AuthClient authClient = new AuthClient("192.168.0.142", 5554);
            AuthClient.AuthResult authResult = authClient.authenticate("waymirec", "password");
            logger.Debug($"Status: {authResult.Status}");
            if (authResult.Status != AuthClient.AuthStatus.Success)
            {
                logger.Debug("Failed to authenticate.");
                Environment.Exit(1);
            }

            logger.Debug($"Server: {authResult.Ip}:{authResult.Port}");
            logger.Debug($"Token:{authResult.Token.Length} => {BitConverter.ToString(authResult.Token).Replace("-", string.Empty)}");
            GameClient gameClient = new GameClient(authResult.Ip, authResult.Port, new Program());
            gameClient.Connect("waymirec", authResult.Token);

            Console.ReadLine();
        }

        public Program()
        {

        }

        public void OnLoggedIn()
        {
            logger.Debug("Logged In!");
        }

        public void OnMove(MovementEventArgs args)
        {
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
    }
}
