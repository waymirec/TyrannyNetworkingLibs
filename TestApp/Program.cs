using NLog;
using System;
using Tyranny.Networking;

namespace Application
{
    class Program
    {
        public static Logger logger;

        TcpServer worldServer;

        static void Main(string[] args)
        { 
            var config = new NLog.Config.LoggingConfiguration();
            var logConsole = new NLog.Targets.ConsoleTarget("Console");
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logConsole);
            LogManager.Configuration = config;

            logger = LogManager.GetCurrentClassLogger();

            Program p = new Program();
            p.Start();
            logger.Info("Started.");
            Console.ReadLine();
            p.Stop();
        }

        public void Start()
        {
            worldServer = new TcpServer("192.168.0.127", 13579);
            worldServer.OnClientConnected += OnClientConnected;
            worldServer.Start();
        }

        public void Stop()
        {
            worldServer.Stop();
        }

        public void OnClientConnected(object source, SocketEventArgs args)
        {
            logger.Info($"[P] Client {args.TcpClient.Id} Connected.");
        }
    }
}
