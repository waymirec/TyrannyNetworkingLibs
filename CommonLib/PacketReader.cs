﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CommonLib;

namespace Tyranny.Networking
{
    public class PacketReader<TOpcode> : BinaryReader where TOpcode : Enum
    {
        public readonly TOpcode Opcode;

        public PacketReader(byte[] bytes) : this(new MemoryStream(bytes))
        {
        }

        public PacketReader(MemoryStream ms) : base(ms)
        {
            ms.Seek(0, SeekOrigin.Begin);
            byte[] opcodeBytes = new byte[2];
            ms.Read(opcodeBytes, 0, 2);
            if (BitConverter.IsLittleEndian) Array.Reverse(opcodeBytes);
            int opcodeValue = BitConverter.ToInt16(opcodeBytes, 0);
            Opcode = (TOpcode)Enum.ToObject(typeof(TOpcode), opcodeValue);
            ms.Seek(2, SeekOrigin.Begin);
        }

        public override int Read()
        {
            byte[] bytes = base.ReadBytes(4);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public override ushort ReadUInt16()
        {
            byte[] bytes = base.ReadBytes(2);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToUInt16(bytes, 0);
        }

        public override uint ReadUInt32()
        {
            byte[] bytes = base.ReadBytes(4);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        public override ulong ReadUInt64()
        {
            byte[] bytes = base.ReadBytes(8);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }

        public override short ReadInt16()
        {
            byte[] bytes = base.ReadBytes(2);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToInt16(bytes, 0);
        }

        public override int ReadInt32()
        {
            byte[] bytes = base.ReadBytes(4);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public override long ReadInt64()
        {
            byte[] bytes = base.ReadBytes(8);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }

        public override float ReadSingle()
        {
            byte[] bytes = base.ReadBytes(4);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToSingle(bytes, 0);
        }

        public override string ReadString()
        {
            int len = ReadUInt16();
            byte[] bytes = ReadBytes(len);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
