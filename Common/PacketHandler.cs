using System;
using System.Collections.Generic;
using System.Text;

namespace Tyranny.Networking.PacketHandler
{
    public delegate void HandlePacket(PacketReader packetIn, AsyncTcpClient tcpClient);

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
    public class PacketHandler : System.Attribute
    {
        TyrannyOpcode opcode;

        public PacketHandler(TyrannyOpcode opcode)
        {
            this.opcode = opcode;
        }

        public TyrannyOpcode GetOpcode()
        {
            return opcode;
        }
    }

    public interface IPacketHandler
    {

    }
}
