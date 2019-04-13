using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Tyranny.Networking;

namespace ConsoleApp1
{
    class Program
    {
        public static SHA256 sha256 = SHA256Managed.Create();

        static void Main(string[] args)
        {

            AuthClient authClient = new AuthClient("192.168.0.142", 5554);
            AuthClient.AuthResult authResult = authClient.authenticate("waymirec", "password");
            if (authResult.Status != AuthClient.AuthStatus.Success)
            {
                Environment.Exit(1);
            }

        }
    }
}
