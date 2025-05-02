namespace VirtualPhenix.PokemonSnapRipper
{
    public class ParseContext
    {
        public FileType FileType { get; }
        public Endianness Endianness { get; }

        public bool LittleEndian => Endianness == Endianness.LITTLE_ENDIAN;

        public StringTable StrKeyTable { get; set; } = null;
        public StringTable StrValueTable { get; set; } = null;
        public PathTable PathTable { get; set; } = null;

        public ParseContext(FileType fileType, Endianness endianness)
        {
            FileType = fileType;
            Endianness = endianness;
        }
    }
}
