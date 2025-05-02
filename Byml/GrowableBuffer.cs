using System;
using System.IO;

namespace VirtualPhenix.PokemonSnapRipper
{
    public class GrowableBuffer
    {
        private byte[] buffer;
        private int growAmount;
        public int UserSize { get; private set; } = 0;
        public int BufferSize => buffer.Length;

        public GrowableBuffer(int initialSize = 0x10000, int growAmount = 0x1000)
        {
            this.growAmount = growAmount;
            buffer = new byte[initialSize];
        }

        public void MaybeGrow(int newUserSize, int newBufferSize = -1)
        {
            if (newUserSize > UserSize)
                UserSize = newUserSize;

            if (newBufferSize == -1)
                newBufferSize = newUserSize;

            if (newBufferSize > buffer.Length)
            {
                int newSize = BymlUtils.Align(newBufferSize, growAmount);
                Array.Resize(ref buffer, newSize);
            }
        }

        public byte[] FinalizeBuffer()
        {
            byte[] result = new byte[UserSize];
            Array.Copy(buffer, 0, result, 0, UserSize);
            return result;
        }

        public byte[] GetBuffer() => buffer;
        public void WriteByte(int offset, byte value) => buffer[offset] = value;
        public void WriteBytes(int offset, byte[] data)
        {
            MaybeGrow(offset + data.Length);
            Array.Copy(data, 0, buffer, offset, data.Length);
        }

        public void WriteUInt32(int offset, uint value, Endianness endianness)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (endianness == Endianness.BIG_ENDIAN)
                Array.Reverse(bytes);

            WriteBytes(offset, bytes);
        }

        public void WriteFloat32(int offset, float value, Endianness endianness)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (endianness == Endianness.BIG_ENDIAN)
                Array.Reverse(bytes);

            WriteBytes(offset, bytes);
        }
    }
}
