using System;

namespace Tyranny.Networking
{
    public interface IPacketHandler<out T> where T : Enum
    {
        T Opcode { get; }
    }
}
