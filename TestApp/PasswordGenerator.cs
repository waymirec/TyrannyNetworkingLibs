using System;
using System.Security.Cryptography;
using System.Text;

namespace Application
{
    public class PasswordGenerator
    {
        static void Main(string[] args)
        {
            var sha256 = SHA256Managed.Create();
            var password = "password";
            byte[] passwordHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            Console.WriteLine($"Password Hash: !{BitConverter.ToString(passwordHash).Replace("-", string.Empty)}!");
        }
    }
}