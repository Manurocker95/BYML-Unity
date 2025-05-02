using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace VirtualPhenix.PokemonSnapRipper
{
    public static class BymlParser
    {
        public static readonly Dictionary<FileType, FileDescription> FileDescriptions = new Dictionary<FileType, FileDescription>()
        {
            {
                FileType.BYML,
                new FileDescription(
                    new[] { "BY\0\u0001", "BY\0\u0002", "YB\u0003\0", "YB\u0001\0" },
                    new[]
                    {
                        NodeType.String,
                        NodeType.Path,
                        NodeType.Array,
                        NodeType.Dictionary,
                        NodeType.StringTable,
                        NodeType.PathTable,
                        NodeType.Bool,
                        NodeType.Int,
                        NodeType.UInt,
                        NodeType.Float,
                        NodeType.Null
                    }
                )
            },
            {
                FileType.CRG1,
                new FileDescription(
                    new[] { "CRG1" },
                    new[]
                    {
                        NodeType.String,
                        NodeType.Path,
                        NodeType.Array,
                        NodeType.Dictionary,
                        NodeType.StringTable,
                        NodeType.PathTable,
                        NodeType.Bool,
                        NodeType.Int,
                        NodeType.UInt,
                        NodeType.Float,
                        NodeType.Null,
                        NodeType.FloatArray,
                        NodeType.BinaryData
                    }
                )
            }
        };

        public static List<string> ParseStringTable(ParseContext context, byte[] buffer, int offset)
        {
            NodeType nodeType = (NodeType)buffer[offset];
            uint numValues = BymlUtils.GetUInt24(buffer, offset + 0x01, context.LittleEndian);

            if (nodeType != NodeType.StringTable)
                throw new Exception($"Expected StringTable node, but found {nodeType}");

            int stringTableIdx = offset + 0x04;
            var strings = new List<string>();

            for (int i = 0; i < numValues; i++)
            {
                int strOffset = offset + (int)BymlUtils.ReadUInt32(buffer, stringTableIdx, context.Endianness);
                strings.Add(BymlUtils.ReadStringUTF8(buffer, strOffset));
                stringTableIdx += 4;
            }

            return strings;
        }

        public static NodeDict ParseDict(ParseContext context, byte[] buffer, int offset)
        {
            NodeType nodeType = (NodeType)buffer[offset];
            uint numValues = BymlUtils.GetUInt24(buffer, offset + 1, context.LittleEndian);

            if (nodeType != NodeType.Dictionary)
                throw new Exception($"Expected Dictionary node, found: {nodeType}");

            if (context.StrKeyTable == null)
                throw new Exception("String key table is null");

            NodeDict result = new NodeDict();

            int dictIdx = offset + 4;
            for (int i = 0; i < numValues; i++)
            {
                uint keyIndex = BymlUtils.GetUInt24(buffer, dictIdx + 0, context.LittleEndian);
                string key = context.StrKeyTable[(int)keyIndex];

                NodeType entryType = (NodeType)buffer[dictIdx + 3];

                // Aquí deberás implementar ParseNode para manejar el tipo real
                IBymlNode value = ParseNode(context, buffer, entryType, dictIdx + 4);

                result[key] = value;
                dictIdx += 8;
            }

            return result;
        }

        public static NodeArray ParseArray(ParseContext context, byte[] buffer, int offset)
        {
            NodeType nodeType = (NodeType)buffer[offset];
            uint numValues = BymlUtils.GetUInt24(buffer, offset + 1, context.LittleEndian);

            if (nodeType != NodeType.Array)
                throw new Exception($"Expected Array node, found: {nodeType}");

            NodeArray result = new NodeArray();

            int entryTypeIdx = offset + 4;
            int entryOffsIdx = BymlUtils.Align(entryTypeIdx + (int)numValues, 4);

            for (int i = 0; i < numValues; i++)
            {
                NodeType entryNodeType = (NodeType)buffer[entryTypeIdx];
                int entryOffset = BymlUtils.ReadInt32(buffer, entryOffsIdx, context.Endianness);

                result.Add(ParseNode(context, buffer, entryNodeType, entryOffset));

                entryTypeIdx++;
                entryOffsIdx += 4;
            }

            return result;
        }

        public static PathTable ParsePathTable(ParseContext context, byte[] buffer, int offset)
        {
            NodeType nodeType = (NodeType)buffer[offset];
            uint numPaths = BymlUtils.GetUInt24(buffer, offset + 1, context.LittleEndian);

            if (nodeType != NodeType.PathTable)
                throw new Exception($"Expected PathTable node, but found {nodeType}");

            PathTable pathTable = new PathTable();

            for (int i = 0; i < numPaths; i++)
            {
                int startOffset = offset + (int)BymlUtils.ReadUInt32(buffer, offset + 4 + 4 * (i + 0), context.Endianness);
                int endOffset = offset + (int)BymlUtils.ReadUInt32(buffer, offset + 4 + 4 * (i + 1), context.Endianness);

                NodePath path = new NodePath();

                for (int j = startOffset; j < endOffset; j += 0x1C)
                {
                    float px = BymlUtils.ReadFloat32(buffer, j + 0x00, context.Endianness);
                    float py = BymlUtils.ReadFloat32(buffer, j + 0x04, context.Endianness);
                    float pz = BymlUtils.ReadFloat32(buffer, j + 0x08, context.Endianness);
                    float nx = BymlUtils.ReadFloat32(buffer, j + 0x0C, context.Endianness);
                    float ny = BymlUtils.ReadFloat32(buffer, j + 0x10, context.Endianness);
                    float nz = BymlUtils.ReadFloat32(buffer, j + 0x14, context.Endianness);
                    float arg = BymlUtils.ReadFloat32(buffer, j + 0x18, context.Endianness);

                    path.Add(new NodePathPoint
                    {
                        Px = px,
                        Py = py,
                        Pz = pz,
                        Nx = nx,
                        Ny = ny,
                        Nz = nz,
                        Arg = arg
                    });
                }

                pathTable.Add(path);
            }

            return pathTable;
        }

        public static IBymlNode ParseComplexNode(ParseContext context, byte[] buffer, int offset, NodeType? expectedNodeType = null)
        {
            NodeType nodeType = (NodeType)buffer[offset];
            uint numValues = BymlUtils.GetUInt24(buffer, offset + 1, context.LittleEndian);

            if (expectedNodeType.HasValue && nodeType != expectedNodeType.Value)
                throw new Exception($"Expected node type {expectedNodeType.Value}, but got {nodeType}");

            switch (nodeType)
            {
                case NodeType.Dictionary:
                    return ParseDict(context, buffer, offset);

                case NodeType.Array:
                    return ParseArray(context, buffer, offset);

                case NodeType.StringTable:
                    return new StringTable(ParseStringTable(context, buffer, offset));

                case NodeType.PathTable:
                    return ParsePathTable(context, buffer, offset);

                case NodeType.BinaryData:
                    if (numValues == 0x00FFFFFF)
                    {
                        uint len = BymlUtils.ReadUInt32(buffer, offset + 4, context.Endianness);
                        byte[] data = new byte[len];
                        Buffer.BlockCopy(buffer, offset + 8, data, 0, (int)len);
                        return new NodeBinaryData(data);
                    }
                    else
                    {
                        byte[] data = new byte[numValues];
                        Buffer.BlockCopy(buffer, offset + 4, data, 0, (int)numValues);
                        return new NodeBinaryData(data);
                    }

                case NodeType.FloatArray:
                    FloatArrayNode floats = new FloatArrayNode();
                    for (int i = 0; i < numValues; i++)
                    {
                        float f = BymlUtils.ReadFloat32(buffer, offset + 4 + i * 4, context.Endianness);
                        floats.Add(f);
                    }
                    return floats;

                default:
                    throw new Exception($"Unhandled complex node type: {nodeType}");
            }
        }

        public static IBymlNode ParseNode(ParseContext context, byte[] buffer, NodeType nodeType, int offset)
        {
            ValidateNodeType(context, nodeType);

            switch (nodeType)
            {
                case NodeType.Array:
                case NodeType.Dictionary:
                case NodeType.StringTable:
                case NodeType.PathTable:
                case NodeType.BinaryData:
                case NodeType.FloatArray:
                    {
                        int complexOffset = (int)BymlUtils.ReadUInt32(buffer, offset, context.Endianness);
                        return ParseComplexNode(context, buffer, complexOffset, nodeType);
                    }

                case NodeType.String:
                    {
                        uint idx = BymlUtils.ReadUInt32(buffer, offset, context.Endianness);
                        if (context.StrValueTable == null)
                            throw new Exception("String value table is null");
                        return new SimpleBymlNode<string>(context.StrValueTable[(int)idx]);
                    }

                case NodeType.Path:
                    {
                        uint idx = BymlUtils.ReadUInt32(buffer, offset, context.Endianness);
                        if (context.PathTable == null)
                            throw new Exception("Path table is null");
                        return context.PathTable[(int)idx];
                    }

                case NodeType.Bool:
                    {
                        uint val = BymlUtils.ReadUInt32(buffer, offset, context.Endianness);
                        if (val != 0 && val != 1)
                            throw new Exception("Invalid boolean value in BYML");
                        return new SimpleBymlNode<bool>(val == 1);
                    }

                case NodeType.Int:
                    return new SimpleBymlNode<int>(BymlUtils.ReadInt32(buffer, offset, context.Endianness));

                case NodeType.UInt:
                    return new SimpleBymlNode<uint>(BymlUtils.ReadUInt32(buffer, offset, context.Endianness));

                case NodeType.Float:
                    return new SimpleBymlNode<float>(BymlUtils.ReadFloat32(buffer, offset, context.Endianness));

                case NodeType.Null:
                    return new SimpleBymlNode<object>(null);

                case NodeType.Int64:
                    return new SimpleBymlNode<long>(BymlUtils.ReadInt64(buffer, offset, context.Endianness));

                case NodeType.UInt64:
                    return new SimpleBymlNode<ulong>(BymlUtils.ReadUInt64(buffer, offset, context.Endianness));

                case NodeType.Float64:
                    return new SimpleBymlNode<double>(BymlUtils.ReadFloat64(buffer, offset, context.Endianness));
                default:
                    throw new NotImplementedException($"Unsupported node type: {nodeType}");
            }
        }

        public static void ValidateNodeType(ParseContext context, NodeType nodeType)
        {
            if (!FileDescriptions.TryGetValue(context.FileType, out var desc))
                throw new Exception($"Unknown FileType: {context.FileType}");

            if (Array.IndexOf(desc.AllowedNodeTypes, nodeType) == -1)
                throw new Exception($"NodeType {nodeType} not allowed in FileType {context.FileType}");
        }

        public static T Parse<T>(byte[] buffer, FileType fileType = FileType.BYML, ParseOptions opt = null)
        {
            string magic = BymlUtils.ReadFixedString(buffer, 0x00, 4);
            var fileDesc = FileDescriptions[fileType];

            if (Array.IndexOf(fileDesc.Magics, magic) == -1)
                throw new Exception($"Unrecognized BYML magic: {magic}");

            bool littleEndian = magic.StartsWith("YB");
            Endianness endianness = littleEndian ? Endianness.LITTLE_ENDIAN : Endianness.BIG_ENDIAN;
            ParseContext context = new ParseContext(fileType, endianness);

            uint strKeyTableOffs = BymlUtils.ReadUInt32(buffer, 0x04, endianness);
            uint strValueTableOffs = BymlUtils.ReadUInt32(buffer, 0x08, endianness);
            int headerOffs = 0x0C;

            uint pathTableOffs = 0;
            if (opt?.HasPathTable == true)
            {
                pathTableOffs = BymlUtils.ReadUInt32(buffer, headerOffs, endianness);
                headerOffs += 4;
            }

            uint rootNodeOffs = BymlUtils.ReadUInt32(buffer, headerOffs, endianness);
            if (rootNodeOffs == 0)
                return (T)(object)new NodeDict();

            context.StrKeyTable = strKeyTableOffs != 0 ? new StringTable(ParseStringTable(context, buffer, (int)strKeyTableOffs)) : null;
            context.StrValueTable = strValueTableOffs != 0 ? new StringTable(ParseStringTable(context, buffer, (int)strValueTableOffs)) : null;
            context.PathTable = pathTableOffs != 0 ? ParsePathTable(context, buffer, (int)pathTableOffs) : null;

            object node = ParseComplexNode(context, buffer, (int)rootNodeOffs);

            if (node is T casted)
                return casted;

            throw new InvalidCastException($"Cannot cast node of type {node.GetType()} to {typeof(T)}");

            return (T)(object)node;
        }

        public static NodeType ClassifyNodeValue(WriteContext w, object v)
        {
            if (v == null)
            {
                return NodeType.Null;
            }
            else if (v is bool)
            {
                return NodeType.Bool;
            }
            else if (v is string)
            {
                return NodeType.String;
            }
            else if (v is int intVal)
            {
                return NodeType.Int;
            }
            else if (v is uint)
            {
                return NodeType.UInt;
            }
            else if (v is float)
            {
                return NodeType.Float;
            }
            else if (v is long lVal && lVal >= 0 && lVal <= uint.MaxValue)
            {
                return NodeType.UInt;
            }
            else if (v is FloatArrayNode && w.CanUseNodeType(NodeType.FloatArray))
            {
                return NodeType.FloatArray;
            }
            else if (v is byte[] && w.CanUseNodeType(NodeType.BinaryData))
            {
                return NodeType.BinaryData;
            }
            else if (v is IList) // general array
            {
                return NodeType.Array;
            }
            else if (v is IDictionary<string, object>) // dictionary
            {
                return NodeType.Dictionary;
            }
            else
            {
                throw new Exception($"Unrecognized node value type: {v.GetType()}");
            }
        }

        public static void WriteComplexValueArray(WriteContext w, List<object> values)
        {
            var stream = w.Stream;
            int numEntries = values.Count;

            // Write header (NodeType + UInt24 count)
            BymlUtils.WriteHeader(w, NodeType.Array, numEntries);

            // Write each entry's node type (as byte)
            foreach (var val in values)
            {
                NodeType type = ClassifyNodeValue(w, val);
                stream.WriteByte((byte)type);
            }

            // Align to 4 bytes
            stream.Align(4);

            // Reserve space for offsets
            int headerOffset = stream.Offset;
            int headerSize = 4 * numEntries;
            stream.SeekTo(stream.Offset + headerSize);

            // Write each value
            for (int i = 0; i < values.Count; i++)
            {
                var val = values[i];
                var nodeType = ClassifyNodeValue(w, val);
                WriteValue(w, nodeType, val, headerOffset + i * 4);
            }
        }

        public static void WriteComplexValueDict(WriteContext w, Dictionary<string, object> dict)
        {
            var stream = w.Stream;
            var keys = new List<string>(dict.Keys);
            int numEntries = keys.Count;

            // NodeType + UInt24 count
            BymlUtils.WriteHeader(w, NodeType.Dictionary, numEntries);

            // Reserve header area (8 bytes per entry)
            int headerOffset = stream.Offset;
            int headerSize = 8 * numEntries;
            stream.SeekTo(stream.Offset + headerSize);

            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                object value = dict[key];
                NodeType type = ClassifyNodeValue(w, value);

                int keyIndex = BymlUtils.StrTableIndex(w.StrKeyTable, key);

                stream.SetUInt24(headerOffset + 0x00, (uint)keyIndex, w.Endianness);
                stream.SetByte(headerOffset + 0x03, (byte)type);

                WriteValue(w, type, value, headerOffset + 0x04);
                headerOffset += 0x08;
            }
        }

        public static void WriteComplexValueFloatArray(WriteContext w, FloatArrayNode v)
        {
            var stream = w.Stream;
            BymlUtils.WriteHeader(w, NodeType.FloatArray, v.Count);

            foreach (float f in v)
                stream.WriteFloat32(f, w.Endianness);
        }

        public static void WriteComplexValueBinary(WriteContext w, byte[] data)
        {
            var stream = w.Stream;
            int length = data.Length;

            if (length >= 0x00FFFFFF)
            {
                BymlUtils.WriteHeader(w, NodeType.BinaryData, 0x00FFFFFF);
                uint extra = (uint)(length - 0x00FFFFFF);
                if (extra > uint.MaxValue)
                    throw new Exception("Binary data too large.");
                stream.WriteUInt32(extra, w.Endianness);
            }
            else
            {
                BymlUtils.WriteHeader(w, NodeType.BinaryData, length);
            }

            stream.WriteBytes(data);
            stream.Align(4);
        }

        public static void WriteValue(WriteContext w, NodeType nodeType, object v, int valueOffset)
        {
            var stream = w.Stream;

            if (v == null)
            {
                stream.SetUInt32(valueOffset, 0x00, w.Endianness);
            }
            else if (v is bool b)
            {
                stream.SetUInt32(valueOffset, b ? 0x01u : 0x00u, w.Endianness);
            }
            else if (v is string str)
            {
                int index = BymlUtils.StrTableIndex(w.StrValueTable, str);
                stream.SetUInt32(valueOffset, (uint)index, w.Endianness);
            }
            else if (v is float f)
            {
                stream.SetFloat32(valueOffset, f, w.Endianness);
            }
            else if (v is uint u)
            {
                stream.SetUInt32(valueOffset, u, w.Endianness);
            }
            else if (v is int i)
            {
                stream.SetInt32(valueOffset, i, w.Endianness);
            }
            else if (v is FloatArrayNode floatArray && w.CanUseNodeType(NodeType.FloatArray))
            {
                stream.SetUInt32(valueOffset, (uint)stream.Offset, w.Endianness);
                WriteComplexValueFloatArray(w, floatArray);
            }
            else if (v is byte[] data && w.CanUseNodeType(NodeType.BinaryData))
            {
                stream.SetUInt32(valueOffset, (uint)stream.Offset, w.Endianness);
                WriteComplexValueBinary(w, data);
            }
            else if (v is List<object> list)
            {
                stream.SetUInt32(valueOffset, (uint)stream.Offset, w.Endianness);
                WriteComplexValueArray(w, list);
            }
            else if (v is Dictionary<string, object> dict)
            {
                stream.SetUInt32(valueOffset, (uint)stream.Offset, w.Endianness);
                WriteComplexValueDict(w, dict);
            }
            else
            {
                throw new Exception($"Unsupported value type for BYML: {v.GetType()}");
            }
        }

        public static void WriteStringTable(WriteContext w, List<string> table)
        {
            var stream = w.Stream;

            // BYML stores string count as count - 1
            int numEntries = table.Count - 1;

            BymlUtils.WriteHeader(w, NodeType.StringTable, numEntries);

            // Reserve space for the string offsets
            int offsetListStart = stream.Offset;
            int strDataOffset = offsetListStart + 4 * table.Count;

            for (int i = 0; i < table.Count; i++)
            {
                stream.WriteUInt32((uint)strDataOffset, w.Endianness);
                strDataOffset += System.Text.Encoding.UTF8.GetByteCount(table[i]) + 1; // +1 for null terminator
            }

            // Write null-terminated UTF-8 strings
            foreach (string str in table)
            {
                stream.WriteString(str);
                stream.WriteByte(0); // null terminator
            }
        }

        public static byte[] Write(Dictionary<string, object> rootNode, FileType fileType = FileType.CRG1, string magicOverride = null)
        {
            var stream = new WritableStream();

            // Obtener magias permitidas
            var magics = BymlParser.FileDescriptions[fileType].Magics;

            string magic;
            if (!string.IsNullOrEmpty(magicOverride))
            {
                if (!magics.Contains(magicOverride))
                    throw new Exception($"Invalid magic: {magicOverride}");
                magic = magicOverride;
            }
            else
            {
                magic = magics[magics.Count() - 1];
            }

            if (magic.Length != 4)
                throw new Exception("Magic must be exactly 4 characters");

            var littleEndian = magic.StartsWith("YB");
            var endianness = littleEndian ? Endianness.LITTLE_ENDIAN : Endianness.BIG_ENDIAN;

            // Recolectar strings
            var keySet = new HashSet<string> { "" };
            var valueSet = new HashSet<string> { "" };
            BymlUtils.GatherStrings(rootNode, keySet, valueSet);

            var keyStrings = new List<string>(keySet);
            var valueStrings = new List<string>(valueSet);
            keyStrings.Sort(BymlUtils.BymlStrCompare);
            valueStrings.Sort(BymlUtils.BymlStrCompare);

            var context = new WriteContext(stream, fileType, endianness, new StringTable(keyStrings), new StringTable(valueStrings));

            // Escribir magic
            stream.SetString(0x00, magic);

            stream.SeekTo(0x10);

            // Key String Table
            int keyStrTableOffset = stream.Offset;
            stream.SetUInt32(0x04, (uint)keyStrTableOffset, endianness);
            WriteStringTable(context, keyStrings);
            stream.Align(4);

            // Value String Table
            int valueStrTableOffset = stream.Offset;
            stream.SetUInt32(0x08, (uint)valueStrTableOffset, endianness);
            WriteStringTable(context, valueStrings);
            stream.Align(4);

            // Root Node
            int rootNodeOffset = stream.Offset;
            stream.SetUInt32(0x0C, (uint)rootNodeOffset, endianness);
            WriteComplexValueDict(context, rootNode);

            return stream.FinalizeBuffer();
        }
    }
}
