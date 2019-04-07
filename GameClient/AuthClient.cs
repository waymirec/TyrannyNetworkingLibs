using NLog;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Tyranny.Networking
{
    public class AuthClient
    {
        public String Host { get; private set; }
        public int Port { get; private set; }

        private Logger logger = LogManager.GetCurrentClassLogger();
        private SHA256 sha256;
        private TcpClient tcpClient;

        public AuthClient(String host, int port)
        {
            sha256 = SHA256Managed.Create();

            Host = host;
            Port = port;

            tcpClient = new TcpClient();
        }

        public AuthResult authenticate(String username, String password)
        {
            bool connected = tcpClient.Connect(Host, Port);
            if (!connected)
            {
                throw new IOException($"failed to connect to {Host}:{Port}");
            }

            byte[] challenge = Identify(username);
            byte ack = Verify(challenge, password);
            AuthResult result;
            if (ack == 0)
            {
                result = CompleteAuth();
            }
            else
            {
                result = new AuthResult((AuthStatus)ack);
            }
            return result;
        }

        private byte[] Identify(String username)
        {
            // SEND IDENT
            PacketWriter identPacket = new PacketWriter(TyrannyOpcode.AuthIdent);
            identPacket.Write((short)1); // Major Vsn
            identPacket.Write((short)1); // Minor Vsn
            identPacket.Write((short)1); // Maint Vsn
            identPacket.Write((short)1); // Build Vsn
            identPacket.Write(username);
            tcpClient.Send(identPacket);

            if (tcpClient.Read(out PacketReader challengePacket))
            {
                int len = challengePacket.ReadInt16();
                Console.WriteLine($"Challenge Length: {len}");
                byte[] challenge = challengePacket.ReadBytes(len);
                Console.WriteLine($"DATA: {BitConverter.ToString(challenge).Replace("-", "")}");

                return challenge;
            }
            else
            {
                throw new IOException("Failed to receive challenge");
            }
        }
        private byte Verify(byte[] challenge, String password)
        {
            byte[] passwordHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            logger.Debug($"Password Hash: !{BitConverter.ToString(passwordHash).Replace("-", string.Empty)}!");

            IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            sha.AppendData(challenge);
            sha.AppendData(passwordHash);
            byte[] proof = sha.GetHashAndReset();
            logger.Debug($"Proof: !{BitConverter.ToString(proof).Replace("-", string.Empty)}!");

            byte[] proofLength = BitConverter.GetBytes((short)proof.Length);
            if (BitConverter.IsLittleEndian) Array.Reverse(proofLength);

            PacketWriter proofPacket = new PacketWriter(TyrannyOpcode.AuthProof);
            proofPacket.Write(proofLength);
            proofPacket.Write(proof);
            tcpClient.Send(proofPacket);

            if (tcpClient.Read(out PacketReader proofAckPacket))
            {
                byte ack = proofAckPacket.ReadByte();
                logger.Debug($"Got Proof Ack: {ack}");

                return ack;
            }
            else
            {
                throw new IOException("Failed to receive proof ack");
            }
        }

        private AuthResult CompleteAuth()
        {
            PacketWriter packetOut = new PacketWriter(TyrannyOpcode.AuthProofAckAck);
            packetOut.Write(1);
            tcpClient.Send(packetOut);

            if (tcpClient.Read(out PacketReader authCompletePacket))
            {
                int status = authCompletePacket.ReadInt32();
                if (status == 0)
                {
                    long ipValue = BitConverter.ToUInt32(authCompletePacket.ReadBytes(4));
                    int port = authCompletePacket.ReadInt32();
                    short authTokenLen = authCompletePacket.ReadInt16();
                    byte[] authToken = authCompletePacket.ReadBytes(authTokenLen);
                    String ip = new IPAddress(ipValue).ToString();
                    logger.Debug($"Auth successful: Status={status}, IP={ip}, Port={port}, Token={BitConverter.ToString(authToken).Replace("-", string.Empty)}");
                    return new AuthResult((AuthStatus)status, ip, port, authToken);
                } else
                {
                    logger.Debug($"Auth failed with status {(AuthStatus)status}");
                    return new AuthResult((AuthStatus)status);
                }
            }
            else
            {
                throw new IOException("Failed to receive auth complete");
            }
        }

        public class AuthResult
        {
            public AuthStatus Status { get; private set; }
            public String Ip { get; private set; }
            public int Port { get; private set; }
            public byte[] Token { get; private set; }

            public AuthResult(AuthStatus status)
            {
                Status = status;
            }

            public AuthResult(AuthStatus status, String ip, int port, byte[] token)
            {
                Status = status;
                Ip = ip;
                Port = port;
                Token = token;
            }
        }

        public enum AuthStatus
        {
            Success=0,
            InvalidCredentials=1,
            NoServersAvailable=999
        }
    }
}
