using NLog;
using System;
using System.IO;
using System.Text;

namespace Tyranny.Networking
{
    public class PacketWriter : BinaryWriter
    {
        public readonly TyrannyOpcode Opcode;

        private Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public PacketWriter(TyrannyOpcode opcode) : base(new MemoryStream())
        {
            this.Opcode = opcode;
            this.Write(0);
            this.Write((short)opcode);
        }

        public override void Write(UInt64 value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            base.Write(bytes, 0, bytes.Length);
        }

        public override void Write(UInt32 value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            base.Write(bytes, 0, bytes.Length);
        }

        public override void Write(UInt16 value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            base.Write(bytes, 0, bytes.Length);
        }

        public override void Write(Int64 value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            base.Write(bytes, 0, bytes.Length);
        }

        public override void Write(Int32 value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            base.Write(bytes, 0, bytes.Length);
        }

        public override void Write(Int16 value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            base.Write(bytes, 0, bytes.Length);
        }

        public override void Write(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            base.Write(bytes, 0, bytes.Length);
        }

        public override void Write(String value)
        {
            byte[] lenBytes = BitConverter.GetBytes((short)value.Length);
            if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);
            base.Write(lenBytes);
            base.Write(Encoding.ASCII.GetBytes(value));
        }

        public byte[] ToBytes()
        {
            MemoryStream ms = (MemoryStream)this.BaseStream;
            int len = (int)ms.Length - 4;

            byte[] lenBytes = BitConverter.GetBytes(len);
            if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);

            long pos = ms.Position;
            ms.Seek(0, SeekOrigin.Begin);
            byte[] bytes = new byte[len+4];
            ms.Read(bytes, 0, len+4);
            ms.Seek(pos, SeekOrigin.Begin);
            Array.Copy(lenBytes, 0, bytes, 0, lenBytes.Length);
            return bytes;
        }
    }
}
