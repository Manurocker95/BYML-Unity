using System;
using System.Text;

namespace VirtualPhenix.PokemonSnapRipper
{
    public class WritableStream
    {
        public GrowableBuffer Buffer { get; private set; }
        public int Offset { get; private set; }

        public WritableStream(GrowableBuffer buffer = null, int offset = 0)
        {
            Buffer = buffer ?? new GrowableBuffer();
            Offset = offset;
        }

        public void SetByte(int offset, byte value)
        {
            Buffer.MaybeGrow(offset + 1);
            Buffer.GetBuffer()[offset] = value;
        }

        public void WriteByte(byte value)
        {
            SetByte(Offset, value);
            Offset += 1;
        }

        public void SetUInt24(int offset, uint value, Endianness endian)
        {
            Buffer.MaybeGrow(offset + 3);
            BymlUtils.SetUInt24(Buffer.GetBuffer(), offset, value, endian);
        }

        public void WriteUInt24(uint value, Endianness endian)
        {
            SetUInt24(Offset, value, endian);
            Offset += 3;
        }

        public void SetUInt32(int offset, uint value, Endianness endian)
        {
            Buffer.MaybeGrow(offset + 4);
            byte[] bytes = BitConverter.GetBytes(value);
            if (endian == Endianness.BIG_ENDIAN)
                Array.Reverse(bytes);
            Buffer.WriteBytes(offset, bytes);
        }

        public void WriteUInt32(uint value, Endianness endian)
        {
            SetUInt32(Offset, value, endian);
            Offset += 4;
        }

        public void SetInt32(int offset, int value, Endianness endian)
        {
            Buffer.MaybeGrow(offset + 4);
            byte[] bytes = BitConverter.GetBytes(value);
            if (endian == Endianness.BIG_ENDIAN)
                Array.Reverse(bytes);
            Buffer.WriteBytes(offset, bytes);
        }

        public void WriteInt32(int value, Endianness endian)
        {
            SetInt32(Offset, value, endian);
            Offset += 4;
        }

        public void SetFloat32(int offset, float value, Endianness endian)
        {
            Buffer.MaybeGrow(offset + 4);
            byte[] bytes = BitConverter.GetBytes(value);
            if (endian == Endianness.BIG_ENDIAN)
                Array.Reverse(bytes);
            Buffer.WriteBytes(offset, bytes);
        }

        public void WriteFloat32(float value, Endianness endian)
        {
            SetFloat32(Offset, value, endian);
            Offset += 4;
        }

        public void SetString(int offset, string text)
        {
            byte[] data = Encoding.ASCII.GetBytes(text);
            Buffer.MaybeGrow(offset + data.Length);
            Buffer.WriteBytes(offset, data);
        }

        public void WriteString(string text)
        {
            SetString(Offset, text);
            Offset += text.Length;
        }

        public void WriteFixedString(string text, int length)
        {
            if (text.Length > length)
                throw new ArgumentException("String too long for fixed size");

            WriteString(text);
            Offset += length - text.Length; // pad with zeros
        }

        public void WriteBytes(byte[] data)
        {
            Buffer.WriteBytes(Offset, data);
            Offset += data.Length;
        }

        public void SeekTo(int offset)
        {
            Offset = offset;
            Buffer.MaybeGrow(Offset);
        }

        public void Align(int multiple)
        {
            Offset = BymlUtils.Align(Offset, multiple);
            Buffer.MaybeGrow(Offset);
        }

        public byte[] FinalizeBuffer()
        {
            return Buffer.FinalizeBuffer();
        }
    }
}
