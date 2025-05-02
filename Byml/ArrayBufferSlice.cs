
using System;
using System.IO;

namespace VirtualPhenix.PokemonSnapRipper
{
    public class ArrayBufferSlice
    {
        public byte[] Buffer { get; }
        public int Start { get; }
        public int Length { get; }

        public ArrayBufferSlice(byte[] buffer) : this(buffer, 0, buffer.Length) { }

        public ArrayBufferSlice(byte[] buffer, int start, int length)
        {
            Buffer = buffer;
            Start = start;
            Length = length;
        }

        public byte this[int index] => Buffer[Start + index];

        public ArraySegment<byte> AsSegment() => new ArraySegment<byte>(Buffer, Start, Length);

        public BinaryReader CreateReader()
        {
            var stream = new MemoryStream(Buffer, Start, Length, false);
            return new BinaryReader(stream);
        }

        public Span<byte> AsSpan() => new Span<byte>(Buffer, Start, Length);

        public ArrayBufferSlice Slice(int offset, int length)
        {
            return new ArrayBufferSlice(Buffer, Start + offset, length);
        }

        public int ByteLength => Length;
    }

}