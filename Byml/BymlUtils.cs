using System.Collections.Generic;
using System;
using System.Collections;

namespace VirtualPhenix.PokemonSnapRipper
{
    public static class BymlUtils
    {
        public static int AlignNonPowerOfTwo(int value, int multiple)
        {
            return ((value + multiple - 1) / multiple) * multiple;
        }
        public static string HexZero0x(int value, int digits = 8)
        {
            return value < 0
                ? $"-0x{HexZero((uint)(-value), digits)}"
                : $"0x{HexZero((uint)value, digits)}";
        }
        public static string HexZero(uint value, int digits)
        {
            return value.ToString("x").PadLeft(digits, '0');
        }
        public static string ReadStringUTF8Old(byte[] buffer, int offset)
        {
            int length = 0;
            while (offset + length < buffer.Length && buffer[offset + length] != 0)
                length++;
            return System.Text.Encoding.UTF8.GetString(buffer, offset, length);
        }

        public static string ReadStringUTF8(byte[] buffer, int offset, int length)
        {
            return System.Text.Encoding.UTF8.GetString(buffer, offset, length);
        }

        public static uint GetUInt24New(byte[] buffer, int offset, bool littleEndian)
        {
            byte b0 = buffer[offset];
            byte b1 = buffer[offset + 1];
            byte b2 = buffer[offset + 2];

            return littleEndian
                ? (uint)(b2 << 16 | b1 << 8 | b0)
                : (uint)(b0 << 16 | b1 << 8 | b2);
        }

        public static uint ReadUInt32New(byte[] buffer, int offset, Endianness endian)
        {
            if (endian == Endianness.LITTLE_ENDIAN)
                return (uint)(buffer[offset] |
                              (buffer[offset + 1] << 8) |
                              (buffer[offset + 2] << 16) |
                              (buffer[offset + 3] << 24));
            else
                return (uint)(buffer[offset + 3] |
                              (buffer[offset + 2] << 8) |
                              (buffer[offset + 1] << 16) |
                              (buffer[offset] << 24));
        }

        public static int AlignTo(int value, int alignment)
        {
            int remainder = value % alignment;
            return remainder == 0 ? value : value + (alignment - remainder);
        }

        public static int Align(int value, int multiple)
        {
            int mask = multiple - 1;
            return (value + mask) & ~mask;
        }

        public static float ReadFloat32(byte[] buffer, int offset, Endianness endian)
        {
            if (endian == Endianness.LITTLE_ENDIAN)
                return BitConverter.ToSingle(buffer, offset);

            byte[] reversed = new byte[4];
            Array.Copy(buffer, offset, reversed, 0, 4);
            Array.Reverse(reversed);
            return BitConverter.ToSingle(reversed, 0);
        }

        public static int ReadInt32(byte[] buffer, int offset, Endianness endian)
        {
            if (endian == Endianness.LITTLE_ENDIAN)
                return BitConverter.ToInt32(buffer, offset);

            byte[] reversed = new byte[4];
            Array.Copy(buffer, offset, reversed, 0, 4);
            Array.Reverse(reversed);
            return BitConverter.ToInt32(reversed, 0);
        }

        public static uint ReadUInt32(byte[] buffer, int offset, Endianness endian)
        {
            if (endian == Endianness.LITTLE_ENDIAN)
                return BitConverter.ToUInt32(buffer, offset);

            byte[] reversed = new byte[4];
            Array.Copy(buffer, offset, reversed, 0, 4);
            Array.Reverse(reversed);
            return BitConverter.ToUInt32(reversed, 0);
        }

        public static FloatArrayNode ReadFloatArray(byte[] buffer, int offset, int count, Endianness endian)
        {
            FloatArrayNode result = new FloatArrayNode();
            for (int i = 0; i < count; i++)
            {
                float value = ReadFloat32(buffer, offset + i * 4, endian);
                result.Add(value);
            }
            return result;
        }

        public static uint GetUInt24(byte[] buffer, int offset, bool littleEndian)
        {
            byte b0 = buffer[offset];
            byte b1 = buffer[offset + 1];
            byte b2 = buffer[offset + 2];

            return littleEndian
                ? (uint)(b2 << 16 | b1 << 8 | b0)
                : (uint)(b0 << 16 | b1 << 8 | b2);
        }

        public static string ReadStringUTF8(byte[] buffer, int offset)
        {
            int start = offset;
            while (offset < buffer.Length && buffer[offset] != 0)
                offset++;

            int length = offset - start;
            return System.Text.Encoding.UTF8.GetString(buffer, start, length);
        }

        public static long ReadInt64(byte[] buffer, int offset, Endianness endian)
        {
            if (endian == Endianness.LITTLE_ENDIAN)
                return BitConverter.ToInt64(buffer, offset);

            byte[] reversed = new byte[8];
            Array.Copy(buffer, offset, reversed, 0, 8);
            Array.Reverse(reversed);
            return BitConverter.ToInt64(reversed, 0);
        }

        public static ulong ReadUInt64(byte[] buffer, int offset, Endianness endian)
        {
            if (endian == Endianness.LITTLE_ENDIAN)
                return BitConverter.ToUInt64(buffer, offset);

            byte[] reversed = new byte[8];
            Array.Copy(buffer, offset, reversed, 0, 8);
            Array.Reverse(reversed);
            return BitConverter.ToUInt64(reversed, 0);
        }

        public static double ReadFloat64(byte[] buffer, int offset, Endianness endian)
        {
            if (endian == Endianness.LITTLE_ENDIAN)
                return BitConverter.ToDouble(buffer, offset);

            byte[] reversed = new byte[8];
            Array.Copy(buffer, offset, reversed, 0, 8);
            Array.Reverse(reversed);
            return BitConverter.ToDouble(reversed, 0);
        }

        public static string ReadFixedString(byte[] buffer, int offset, int length, bool nulTerminated = true)
        {
            int end = offset + length;
            int count = 0;
            while (offset + count < end && (!nulTerminated || buffer[offset + count] != 0))
                count++;

            return System.Text.Encoding.UTF8.GetString(buffer, offset, count);
        }

        public static void SetUInt24(byte[] buffer, int offset, uint value, Endianness endianness)
        {
            if (endianness == Endianness.LITTLE_ENDIAN)
            {
                buffer[offset + 0] = (byte)((value >> 0) & 0xFF);
                buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
                buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
            }
            else
            {
                buffer[offset + 0] = (byte)((value >> 16) & 0xFF);
                buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
                buffer[offset + 2] = (byte)((value >> 0) & 0xFF);
            }
        }

        public static int StrTableIndex(StringTable table, string value)
        {
            int index = table.IndexOf(value);
            if (index < 0)
                throw new Exception($"String '{value}' not found in string table.");
            return index;
        }

        public static void WriteHeader(WriteContext context, NodeType nodeType, int numEntries)
        {
            var stream = context.Stream;
            stream.WriteByte((byte)nodeType);
            stream.WriteUInt24((uint)numEntries, context.Endianness);
        }

        public static void GatherStrings(object node, HashSet<string> keyStrings, HashSet<string> valueStrings)
        {
            if (node == null ||
                node is int || node is uint || node is float || node is double ||
                node is long || node is ulong || node is bool ||
                node is FloatArrayNode || node is byte[])
            {
                // Nada que recolectar
                return;
            }
            else if (node is string str)
            {
                valueStrings.Add(str);
            }
            else if (node is IList list)
            {
                foreach (var item in list)
                    GatherStrings(item, keyStrings, valueStrings);
            }
            else if (node is IDictionary<string, object> dict)
            {
                foreach (var key in dict.Keys)
                    keyStrings.Add(key);

                foreach (var value in dict.Values)
                    GatherStrings(value, keyStrings, valueStrings);
            }
            else
            {
                throw new Exception($"Unsupported node type in GatherStrings: {node.GetType()}");
            }
        }

        public static int BymlStrCompare(string a, string b)
        {
            if (a == "")
                return 1;
            else if (b == "")
                return -1;
            else
                return string.Compare(a, b, StringComparison.Ordinal);
        }
    }
}
