namespace VirtualPhenix.PokemonSnapRipper
{
    public enum FileType : byte
    {
        BYML = 0,
        CRG1 = 1 // Jasper's BYML variant with extensions
    }

    public enum NodeType : byte
    {
        String = 0xA0,
        Path = 0xA1,
        Array = 0xC0,
        Dictionary = 0xC1,
        StringTable = 0xC2,
        PathTable = 0xC3,
        BinaryData = 0xCB, // CRG1 extension
        Bool = 0xD0,
        Int = 0xD1,
        Float = 0xD2,
        UInt = 0xD3,
        Int64 = 0xE4,
        UInt64 = 0xE5,
        Float64 = 0xE6,
        FloatArray = 0xE2, // CRG1 extension
        Null = 0xFF
    }

}