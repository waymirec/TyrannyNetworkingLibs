using NLog;
using System;
using System.Security.Cryptography;
using System.Text;
using Tyranny.Networking;

namespace Application
{
    class Program
    {
        //public static Logger logger;

        //TcpServer worldServer;

        private AsyncUdpClient client;
        
        static void Main(string[] args)
        {
            /*
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
            */

            var p = new Program();
            p.Start();
            p.Stop();
        }

        public void Start()
        {
            client = new AsyncUdpClient(54322);
            client.JoinMulticastGroup("239.0.1.1");
            client.OnDataReceived += OnDataReceived;
            client.Start();
            Console.ReadLine();
            client.Stop();
            /*
            worldServer = new TcpServer("192.168.255.128", 13579);
            worldServer.OnClientConnected += OnClientConnected;
            worldServer.Start();
            */
        }

        public void Stop()
        {
            //worldServer.Stop();
        }

        public void OnDataReceived(object source, AsyncUdpPacketEventArgs args)
        {
            Console.WriteLine(args.Packet.Opcode + " => " + args.Packet.ReadString());
        }
    }
}
