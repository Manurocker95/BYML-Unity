using System;
using System.Linq;

namespace VirtualPhenix.PokemonSnapRipper
{
    public class WriteContext
    {
        public WritableStream Stream { get; }
        public FileType FileType { get; }
        public Endianness Endianness { get; }
        public StringTable StrKeyTable { get; }
        public StringTable StrValueTable { get; }

        public WriteContext(WritableStream stream, FileType fileType, Endianness endianness, StringTable strKeyTable, StringTable strValueTable)
        {
            Stream = stream;
            FileType = fileType;
            Endianness = endianness;
            StrKeyTable = strKeyTable;
            StrValueTable = strValueTable;
        }

        public bool LittleEndian => Endianness == Endianness.LITTLE_ENDIAN;

        public bool CanUseNodeType(NodeType nodeType)
        {
            if (!BymlParser.FileDescriptions.TryGetValue(FileType, out var desc))
                throw new Exception($"Unknown FileType: {FileType}");

            return desc.AllowedNodeTypes.Contains(nodeType);
        }
    }
}
