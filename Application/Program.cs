using NLog;
using System;
using System.Security.Cryptography;
using Tyranny.Networking;

namespace Application
{
    class Program
    {
        public static Logger logger;
        public static SHA256 sha256 = SHA256Managed.Create();

        static void Main(string[] args)
        {
            var config = new NLog.Config.LoggingConfiguration();
            var logConsole = new NLog.Targets.ConsoleTarget("Console");
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logConsole);
            NLog.LogManager.Configuration = config;

            logger = NLog.LogManager.GetCurrentClassLogger();
            AuthClient authClient = new AuthClient("10.211.55.2", 5554);
            AuthClient.AuthResult authResult = authClient.authenticate("waymirec", "password");
            logger.Debug($"Status: {authResult.Status}");
            if (authResult.Status == AuthClient.AuthStatus.Success)
            {
                logger.Debug($"Server: {authResult.Ip}:{authResult.Port}");
            }
            Console.ReadLine();
        }
    }
}
