using System;

namespace VirtualPhenix.Nintendo64
{
    public enum Endianness
    {
        LittleEndian,
        BigEndian
    }

    public static class VP_EndianUtils
    {
        private static readonly Endianness systemEndianness = BitConverter.IsLittleEndian
            ? Endianness.LittleEndian
            : Endianness.BigEndian;

        public static Endianness GetSystemEndianness()
        {
            return systemEndianness;
        }
    }
}