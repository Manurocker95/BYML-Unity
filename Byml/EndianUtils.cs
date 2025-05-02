using System;

namespace VirtualPhenix.PokemonSnapRipper
{
    public enum Endianness
    {
        LITTLE_ENDIAN,
        BIG_ENDIAN
    }

    public static class EndianUtils
    {
        public static Endianness GetSystemEndianness()
        {
            // Si el valor 0xFEFF como ushort se representa en memoria como FF FE, el sistema es little-endian
            ushort value = 0xFEFF;
            byte[] bytes = BitConverter.GetBytes(value);
            return bytes[0] == 0xFF ? Endianness.LITTLE_ENDIAN : Endianness.BIG_ENDIAN;
        }
    }
}
