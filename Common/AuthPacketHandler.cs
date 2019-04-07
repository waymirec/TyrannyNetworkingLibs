using NLog;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Tyranny.Networking
{
    public class AuthPacketHandler : IPacketHandler
    {
        public static Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public static SHA256 sha256 = SHA256Managed.Create();

        [PacketHandler(TyrannyOpcode.AuthChallenge)]
        public static void ReceiveChallenge(PacketReader packetIn, AsyncTcpClient client)
        {
            int len = packetIn.ReadInt16();
            Console.WriteLine($"Challenge Length: {len}");
            byte[] challenge = packetIn.ReadBytes(len);
            Console.WriteLine($"DATA: {BitConverter.ToString(challenge).Replace("-", "")}");

            // SEND PROOF
            byte[] passwordHash = sha256.ComputeHash(Encoding.UTF8.GetBytes("password"));
            logger.Debug($"Password Hash: !{BitConverter.ToString(passwordHash).Replace("-", string.Empty)}!");

            IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            sha.AppendData(challenge);
            sha.AppendData(passwordHash);
            byte[] proof = sha.GetHashAndReset();
            logger.Debug($"Proof: !{BitConverter.ToString(proof).Replace("-", string.Empty)}!");

            byte[] proofLength = BitConverter.GetBytes((short)proof.Length);
            if (BitConverter.IsLittleEndian) Array.Reverse(proofLength);

            PacketWriter packetOut = new PacketWriter(TyrannyOpcode.AuthProof);
            packetOut.Write(proofLength);
            packetOut.Write(proof);
            client.Send(packetOut);
        }

        [PacketHandler(TyrannyOpcode.AuthProofAck)]
        public static void ReceiveProofAck(PacketReader packetIn, AsyncTcpClient client)
        {
            byte ack = packetIn.ReadByte();
            logger.Debug($"Got Proof Ack: {ack}");

            // SEND PROOF ACK ACK
            PacketWriter packetOut = new PacketWriter(TyrannyOpcode.AuthProofAckAck);
            packetOut.Write(1);
            client.Send(packetOut);
        }

        [PacketHandler(TyrannyOpcode.AuthComplete)]
        public static void ReceiveAuthComplete(PacketReader packet, AsyncTcpClient client)
        {
            int status = packet.ReadInt32();
            long ipValue = BitConverter.ToUInt32(packet.ReadBytes(4));
            int port = packet.ReadInt32();
            short authTokenLen = packet.ReadInt16();
            byte[] authToken = packet.ReadBytes(authTokenLen);
            logger.Debug($"AUTH: Status={status}, IP={new IPAddress(ipValue).ToString()}, Port={port}, Token={BitConverter.ToString(authToken).Replace("-", string.Empty)}");
        }
    }
}
