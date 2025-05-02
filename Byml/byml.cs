using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace BYML
{
    public enum FileType
    {
        BYML,
        CRG1 // Jasper's BYML variant with extensions
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

    public enum Endianness
    {
        LittleEndian,
        BigEndian
    }

    public class FileDescription
    {
        public string[] Magics { get; set; }
        public NodeType[] AllowedNodeTypes { get; set; }
    }

    public static class BYML
    {
        private static readonly Dictionary<FileType, FileDescription> _fileDescriptions = new Dictionary<FileType, FileDescription>
        {
            [FileType.BYML] = new FileDescription
            {
                Magics = new[] { "BY\0\x01", "BY\0\x02", "YB\x03\0", "YB\x01\0" },
                AllowedNodeTypes = new[] 
                { 
                    NodeType.String, NodeType.Path, NodeType.Array, NodeType.Dictionary, 
                    NodeType.StringTable, NodeType.PathTable, NodeType.Bool, NodeType.Int, 
                    NodeType.UInt, NodeType.Float, NodeType.Null 
                }
            },
            [FileType.CRG1] = new FileDescription
            {
                Magics = new[] { "CRG1" },
                AllowedNodeTypes = new[] 
                { 
                    NodeType.String, NodeType.Path, NodeType.Array, NodeType.Dictionary, 
                    NodeType.StringTable, NodeType.PathTable, NodeType.Bool, NodeType.Int, 
                    NodeType.UInt, NodeType.Float, NodeType.Null, NodeType.FloatArray, 
                    NodeType.BinaryData 
                }
            }
        };

        public class ParseOptions
        {
            public bool HasPathTable { get; set; }
        }

        public class ParseContext
        {
            public FileType FileType { get; }
            public Endianness Endianness { get; }
            public bool LittleEndian => Endianness == Endianness.LittleEndian;
            public List<string> StrKeyTable { get; set; }
            public List<string> StrValueTable { get; set; }
            public List<List<PathPoint>> PathTable { get; set; }

            public ParseContext(FileType fileType, Endianness endianness)
            {
                FileType = fileType;
                Endianness = endianness;
            }
        }

        public class PathPoint
        {
            public float Px { get; set; }
            public float Py { get; set; }
            public float Pz { get; set; }
            public float Nx { get; set; }
            public float Ny { get; set; }
            public float Nz { get; set; }
            public float Arg { get; set; }
        }

        public static T Parse<T>(byte[] data, FileType fileType = FileType.BYML, ParseOptions options = null) where T : class
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                var magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
                var magics = _fileDescriptions[fileType].Magics;
                
                if (!magics.Contains(magic))
                    throw new Exception("Invalid magic");

                var littleEndian = magic.Substring(0, 2) == "YB";
                var endianness = littleEndian ? Endianness.LittleEndian : Endianness.BigEndian;
                var context = new ParseContext(fileType, endianness);

                var strKeyTableOffset = ReadUInt32(reader, littleEndian);
                var strValueTableOffset = ReadUInt32(reader, littleEndian);
                
                var headerOffset = 0x0C;
                var pathTableOffset = 0u;
                
                if (options != null && options.HasPathTable)
                {
                    pathTableOffset = ReadUInt32(reader, littleEndian);
                    headerOffset += 4;
                }
                
                var rootNodeOffset = ReadUInt32(reader, littleEndian);

                if (rootNodeOffset == 0)
                    return Activator.CreateInstance<T>();

                // Parse string tables
                if (strKeyTableOffset != 0)
                    context.StrKeyTable = ParseStringTable(context, data, strKeyTableOffset);
                
                if (strValueTableOffset != 0)
                    context.StrValueTable = ParseStringTable(context, data, strValueTableOffset);
                
                if (pathTableOffset != 0)
                    context.PathTable = ParsePathTable(context, data, pathTableOffset);

                var node = ParseComplexNode(context, data, rootNodeOffset);
                return node as T;
            }
        }

        private static List<string> ParseStringTable(ParseContext context, byte[] data, uint offset)
        {
            var nodeType = (NodeType)data[offset];
            var numValues = GetUInt24(data, offset + 1, context.LittleEndian);
            
            if (nodeType != NodeType.StringTable)
                throw new Exception("Expected string table");

            var stringTableIdx = offset + 4;
            var strings = new List<string>();
            
            for (int i = 0; i < numValues; i++)
            {
                var strOffset = offset + BitConverter.ToUInt32(data, (int)stringTableIdx);
                if (context.LittleEndian)
                    strOffset = (uint)IPAddress.HostToNetworkOrder((int)strOffset);
                
                strings.Add(ReadStringUTF8(data, strOffset));
                stringTableIdx += 4;
            }
            
            return strings;
        }

        private static Dictionary<string, object> ParseDict(ParseContext context, byte[] data, uint offset)
        {
            var nodeType = (NodeType)data[offset];
            var numValues = GetUInt24(data, offset + 1, context.LittleEndian);
            
            if (nodeType != NodeType.Dictionary)
                throw new Exception("Expected dictionary");

            var result = new Dictionary<string, object>();
            var dictIdx = offset + 4;
            
            for (int i = 0; i < numValues; i++)
            {
                var entryStrKeyIdx = GetUInt24(data, dictIdx, context.LittleEndian);
                var entryKey = context.StrKeyTable[(int)entryStrKeyIdx];
                var entryNodeType = (NodeType)data[dictIdx + 3];
                var entryValue = ParseNode(context, data, entryNodeType, dictIdx + 4);
                result[entryKey] = entryValue;
                dictIdx += 8;
            }
            
            return result;
        }

        private static List<object> ParseArray(ParseContext context, byte[] data, uint offset)
        {
            var nodeType = (NodeType)data[offset];
            var numValues = GetUInt24(data, offset + 1, context.LittleEndian);
            
            if (nodeType != NodeType.Array)
                throw new Exception("Expected array");

            var result = new List<object>();
            var entryTypeIdx = offset + 4;
            var entryOffsIdx = Align(entryTypeIdx + numValues, 4);
            
            for (int i = 0; i < numValues; i++)
            {
                var entryNodeType = (NodeType)data[entryTypeIdx];
                result.Add(ParseNode(context, data, entryNodeType, entryOffsIdx));
                entryTypeIdx++;
                entryOffsIdx += 4;
            }
            
            return result;
        }

        private static List<List<PathPoint>> ParsePathTable(ParseContext context, byte[] data, uint offset)
        {
            var nodeType = (NodeType)data[offset];
            var numPaths = GetUInt24(data, offset + 1, context.LittleEndian);
            
            if (nodeType != NodeType.PathTable)
                throw new Exception("Expected path table");

            var pathTable = new List<List<PathPoint>>();
            
            for (int i = 0; i < numPaths; i++)
            {
                var startOffset = offset + BitConverter.ToUInt32(data, (int)(offset + 4 + 4 * i));
                var endOffset = offset + BitConverter.ToUInt32(data, (int)(offset + 4 + 4 * (i + 1)));
                
                if (context.LittleEndian)
                {
                    startOffset = (uint)IPAddress.HostToNetworkOrder((int)startOffset);
                    endOffset = (uint)IPAddress.HostToNetworkOrder((int)endOffset);
                }

                var path = new List<PathPoint>();
                
                for (uint j = startOffset; j < endOffset; j += 0x1C)
                {
                    var px = BitConverter.ToSingle(data, (int)j);
                    var py = BitConverter.ToSingle(data, (int)j + 4);
                    var pz = BitConverter.ToSingle(data, (int)j + 8);
                    var nx = BitConverter.ToSingle(data, (int)j + 0xC);
                    var ny = BitConverter.ToSingle(data, (int)j + 0x10);
                    var nz = BitConverter.ToSingle(data, (int)j + 0x14);
                    var arg = BitConverter.ToSingle(data, (int)j + 0x18);
                    
                    if (context.LittleEndian)
                    {
                        px = IPAddress.HostToNetworkOrder(BitConverter.ToInt32(BitConverter.GetBytes(px), 0));
                        py = IPAddress.HostToNetworkOrder(BitConverter.ToInt32(BitConverter.GetBytes(py), 0));
                        pz = IPAddress.HostToNetworkOrder(BitConverter.ToInt32(BitConverter.GetBytes(pz), 0));
                        nx = IPAddress.HostToNetworkOrder(BitConverter.ToInt32(BitConverter.GetBytes(nx), 0));
                        ny = IPAddress.HostToNetworkOrder(BitConverter.ToInt32(BitConverter.GetBytes(ny), 0));
                        nz = IPAddress.HostToNetworkOrder(BitConverter.ToInt32(BitConverter.GetBytes(nz), 0));
                        arg = IPAddress.HostToNetworkOrder(BitConverter.ToInt32(BitConverter.GetBytes(arg), 0));
                    }

                    path.Add(new PathPoint 
                    { 
                        Px = px, Py = py, Pz = pz, 
                        Nx = nx, Ny = ny, Nz = nz, 
                        Arg = arg 
                    });
                }
                
                pathTable.Add(path);
            }
            
            return pathTable;
        }

        private static object ParseComplexNode(ParseContext context, byte[] data, uint offset, NodeType? expectedNodeType = null)
        {
            var nodeType = (NodeType)data[offset];
            var numValues = GetUInt24(data, offset + 1, context.LittleEndian);
            
            if (expectedNodeType.HasValue && expectedNodeType.Value != nodeType)
                throw new Exception($"Expected node type {expectedNodeType} but got {nodeType}");

            switch (nodeType)
            {
                case NodeType.Dictionary:
                    return ParseDict(context, data, offset);
                case NodeType.Array:
                    return ParseArray(context, data, offset);
                case NodeType.StringTable:
                    return ParseStringTable(context, data, offset);
                case NodeType.PathTable:
                    return ParsePathTable(context, data, offset);
                case NodeType.BinaryData:
                    if (numValues == 0x00FFFFFF)
                    {
                        var numValues2 = BitConverter.ToUInt32(data, (int)offset + 4);
                        if (context.LittleEndian)
                            numValues2 = (uint)IPAddress.HostToNetworkOrder((int)numValues2);
                        
                        var subarray = new byte[numValues + numValues2];
                        Array.Copy(data, offset + 8, subarray, 0, subarray.Length);
                        return subarray;
                    }
                    else
                    {
                        var subarray = new byte[numValues];
                        Array.Copy(data, offset + 4, subarray, 0, subarray.Length);
                        return subarray;
                    }
                case NodeType.FloatArray:
                    var floatArray = new float[numValues];
                    for (int i = 0; i < numValues; i++)
                    {
                        floatArray[i] = BitConverter.ToSingle(data, (int)offset + 4 + i * 4);
                        if (context.LittleEndian)
                            floatArray[i] = IPAddress.HostToNetworkOrder(BitConverter.ToInt32(BitConverter.GetBytes(floatArray[i]), 0));
                    }
                    return floatArray;
                default:
                    throw new Exception("Unsupported complex node type");
            }
        }

        private static void ValidateNodeType(ParseContext context, NodeType nodeType)
        {
            if (!_fileDescriptions[context.FileType].AllowedNodeTypes.Contains(nodeType))
                throw new Exception($"Node type {nodeType} not allowed for file type {context.FileType}");
        }

        private static object ParseNode(ParseContext context, byte[] data, NodeType nodeType, uint offset)
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
                    var complexOffset = BitConverter.ToUInt32(data, (int)offset);
                    if (context.LittleEndian)
                        complexOffset = (uint)IPAddress.HostToNetworkOrder((int)complexOffset);
                    return ParseComplexNode(context, data, complexOffset, nodeType);
                
                case NodeType.String:
                    var idx = BitConverter.ToUInt32(data, (int)offset);
                    if (context.LittleEndian)
                        idx = (uint)IPAddress.HostToNetworkOrder((int)idx);
                    return context.StrValueTable[(int)idx];
                
                case NodeType.Path:
                    var pathIdx = BitConverter.ToUInt32(data, (int)offset);
                    if (context.LittleEndian)
                        pathIdx = (uint)IPAddress.HostToNetworkOrder((int)pathIdx);
                    return context.PathTable[(int)pathIdx];
                
                case NodeType.Bool:
                    var value = BitConverter.ToUInt32(data, (int)offset);
                    if (context.LittleEndian)
                        value = (uint)IPAddress.HostToNetworkOrder((int)value);
                    if (value != 0 && value != 1)
                        throw new Exception("Invalid boolean value");
                    return value != 0;
                
                case NodeType.Int:
                    var intValue = BitConverter.ToInt32(data, (int)offset);
                    if (context.LittleEndian)
                        intValue = IPAddress.HostToNetworkOrder(intValue);
                    return intValue;
                
                case NodeType.UInt:
                    var uintValue = BitConverter.ToUInt32(data, (int)offset);
                    if (context.LittleEndian)
                        uintValue = (uint)IPAddress.HostToNetworkOrder((int)uintValue);
                    return uintValue;
                
                case NodeType.Float:
                    int raw = BitConverter.ToInt32(data, (int)offset);
                    if (context.LittleEndian)
                        raw = IPAddress.NetworkToHostOrder(raw);
                    return BitConverter.ToSingle(BitConverter.GetBytes(raw), 0);
                
                case NodeType.Int64:
                    var longValue = BitConverter.ToInt64(data, (int)offset);
                    if (context.LittleEndian)
                        longValue = IPAddress.HostToNetworkOrder(longValue);
                    return longValue;
                
                case NodeType.UInt64:
                    var ulongValue = BitConverter.ToUInt64(data, (int)offset);
                    if (context.LittleEndian)
                        ulongValue = (ulong)IPAddress.HostToNetworkOrder((long)ulongValue);
                    return ulongValue;
                
                case NodeType.Float64:
                    var doubleValue = BitConverter.ToDouble(data, (int)offset);
                    if (context.LittleEndian)
                        doubleValue = BitConverter.Int64BitsToDouble(IPAddress.HostToNetworkOrder(BitConverter.DoubleToInt64Bits(doubleValue)));
                    return doubleValue;
                
                case NodeType.Null:
                    return null;
                
                default:
                    throw new Exception("Unsupported node type");
            }
        }

        private static string ReadStringUTF8(byte[] data, uint offset)
        {
            var length = 0;
            while (data[offset + length] != 0)
                length++;
            
            return Encoding.UTF8.GetString(data, (int)offset, length);
        }

        private static uint GetUInt24(byte[] data, uint offset, bool littleEndian)
        {
            var b0 = data[offset];
            var b1 = data[offset + 1];
            var b2 = data[offset + 2];
            
            if (littleEndian)
                return (uint)(b2 << 16 | b1 << 8 | b0);
            else
                return (uint)(b0 << 16 | b1 << 8 | b2);
        }

        private static uint ReadUInt32(BinaryReader reader, bool littleEndian)
        {
            var bytes = reader.ReadBytes(4);
            if (!littleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        private static uint Align(uint value, uint alignment)
        {
            return (value + alignment - 1) & ~(alignment - 1);
        }

        // Writer implementation goes here...

        public class WritableStream
        {
            private MemoryStream _stream;
            private BinaryWriter _writer;
            private uint _position;

            public uint Position
            {
                get
                {
                    return _position;
                }
            }

            public WritableStream(int initialSize = 0x10000)
            {
                _stream = new MemoryStream(initialSize);
                _writer = new BinaryWriter(_stream);
                _position = 0;
            }

            public void WriteBuffer(byte[] buffer, uint offset, int count)
            {
                EnsureCapacity(offset + (uint)count);
                Array.Copy(buffer, 0, _stream.GetBuffer(), offset, count);
                if (offset + count > _position)
                    _position = offset + (uint)count;
            }

            public void WriteString(string value, uint offset)
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                EnsureCapacity(offset + (uint)bytes.Length);
                Array.Copy(bytes, 0, _stream.GetBuffer(), offset, bytes.Length);
                if (offset + bytes.Length > _position)
                    _position = offset + (uint)bytes.Length;
            }

            public void WriteFixedString(string value, uint offset, int length)
            {
                if (value.Length >= length)
                    throw new ArgumentException("String too long");

                var bytes = Encoding.UTF8.GetBytes(value);
                EnsureCapacity(offset + (uint)length);
                Array.Copy(bytes, 0, _stream.GetBuffer(), (int)offset, bytes.Length);

                // Pad with zeros - need to cast offset and length to int
                Array.Clear(
                    _stream.GetBuffer(),
                    (int)(offset + bytes.Length),
                    (int)(length - bytes.Length)
                );

                if (offset + length > _position)
                    _position = offset + (uint)length;
            }

            public void WriteUInt8(byte value, uint offset)
            {
                EnsureCapacity(offset + 1);
                _stream.GetBuffer()[offset] = value;
                if (offset + 1 > _position)
                    _position = offset + 1;
            }

            public void WriteUInt24(uint value, uint offset, bool littleEndian)
            {
                EnsureCapacity(offset + 3);
                if (littleEndian)
                {
                    _stream.GetBuffer()[offset] = (byte)(value & 0xFF);
                    _stream.GetBuffer()[offset + 1] = (byte)((value >> 8) & 0xFF);
                    _stream.GetBuffer()[offset + 2] = (byte)((value >> 16) & 0xFF);
                }
                else
                {
                    _stream.GetBuffer()[offset] = (byte)((value >> 16) & 0xFF);
                    _stream.GetBuffer()[offset + 1] = (byte)((value >> 8) & 0xFF);
                    _stream.GetBuffer()[offset + 2] = (byte)(value & 0xFF);
                }
                if (offset + 3 > _position)
                    _position = offset + 3;
            }

            public void WriteUInt32(uint value, uint offset, bool littleEndian)
            {
                EnsureCapacity(offset + 4);
                var bytes = BitConverter.GetBytes(value);
                if (!littleEndian)
                    Array.Reverse(bytes);
                Array.Copy(bytes, 0, _stream.GetBuffer(), offset, 4);
                if (offset + 4 > _position)
                    _position = offset + 4;
            }

            public void WriteInt32(int value, uint offset, bool littleEndian)
            {
                EnsureCapacity(offset + 4);
                var bytes = BitConverter.GetBytes(value);
                if (!littleEndian)
                    Array.Reverse(bytes);
                Array.Copy(bytes, 0, _stream.GetBuffer(), offset, 4);
                if (offset + 4 > _position)
                    _position = offset + 4;
            }

            public void WriteFloat32(float value, uint offset, bool littleEndian)
            {
                EnsureCapacity(offset + 4);
                var bytes = BitConverter.GetBytes(value);
                if (!littleEndian)
                    Array.Reverse(bytes);
                Array.Copy(bytes, 0, _stream.GetBuffer(), offset, 4);
                if (offset + 4 > _position)
                    _position = offset + 4;
            }

            public void WriteInt64(long value, uint offset, bool littleEndian)
            {
                EnsureCapacity(offset + 8);
                var bytes = BitConverter.GetBytes(value);
                if (!littleEndian)
                    Array.Reverse(bytes);
                Array.Copy(bytes, 0, _stream.GetBuffer(), offset, 8);
                if (offset + 8 > _position)
                    _position = offset + 8;
            }

            public void WriteUInt64(ulong value, uint offset, bool littleEndian)
            {
                EnsureCapacity(offset + 8);
                var bytes = BitConverter.GetBytes(value);
                if (!littleEndian)
                    Array.Reverse(bytes);
                Array.Copy(bytes, 0, _stream.GetBuffer(), offset, 8);
                if (offset + 8 > _position)
                    _position = offset + 8;
            }

            public void WriteFloat64(double value, uint offset, bool littleEndian)
            {
                EnsureCapacity(offset + 8);
                var bytes = BitConverter.GetBytes(value);
                if (!littleEndian)
                    Array.Reverse(bytes);
                Array.Copy(bytes, 0, _stream.GetBuffer(), offset, 8);
                if (offset + 8 > _position)
                    _position = offset + 8;
            }

            public void Seek(uint position)
            {
                _position = position;
                EnsureCapacity(_position);
            }

            public void Align(uint alignment)
            {
                _position = (_position + alignment - 1) & ~(alignment - 1);
                EnsureCapacity(_position);
            }

            public byte[] FinalizeBuffer()
            {
                var result = new byte[_position];
                Array.Copy(_stream.GetBuffer(), result, _position);
                return result;
            }

            private void EnsureCapacity(uint requiredCapacity)
            {
                if (_stream.Capacity < requiredCapacity)
                {
                    var newCapacity = Math.Max(_stream.Capacity * 2, requiredCapacity);
                    var newBuffer = new byte[newCapacity];
                    Array.Copy(_stream.GetBuffer(), newBuffer, _stream.Length);
                    _stream = new MemoryStream(newBuffer);
                    _writer = new BinaryWriter(_stream);
                }
            }
        }

        public class WriteContext
        {
            public WritableStream Stream { get; }
            public FileType FileType { get; }
            public Endianness Endianness { get; }
            public bool LittleEndian => Endianness == Endianness.LittleEndian;
            public List<string> StrKeyTable { get; }
            public List<string> StrValueTable { get; }

            public WriteContext(WritableStream stream, FileType fileType, Endianness endianness, List<string> strKeyTable, List<string> strValueTable)
            {
                Stream = stream;
                FileType = fileType;
                Endianness = endianness;
                StrKeyTable = strKeyTable;
                StrValueTable = strValueTable;
            }

            public bool CanUseNodeType(NodeType type)
            {
                return _fileDescriptions[FileType].AllowedNodeTypes.Contains(type);
            }
        }

        private static int StrTableIndex(List<string> table, string value)
        {
            var index = table.IndexOf(value);
            if (index < 0)
                throw new KeyNotFoundException("String not found in table");
            return index;
        }

        private static void WriteHeader(WriteContext context, NodeType nodeType, int numEntries)
        {
            context.Stream.WriteUInt8((byte)nodeType, context.Stream.Position);
            context.Stream.WriteUInt24((uint)numEntries, context.Stream.Position + 1, context.LittleEndian);
            context.Stream.Seek(context.Stream.Position + 4);
        }

        private static NodeType ClassifyNodeValue(WriteContext context, object value)
        {
            if (value == null)
            {
                return NodeType.Null;
            }
            else if (value is bool)
            {
                return NodeType.Bool;
            }
            else if (value is string)
            {
                return NodeType.String;
            }
            else if (value is int)
            {
                return NodeType.Int;
            }
            else if (value is uint)
            {
                return NodeType.UInt;
            }
            else if (value is float)
            {
                return NodeType.Float;
            }
            else if (value is long)
            {
                return NodeType.Int64;
            }
            else if (value is ulong)
            {
                return NodeType.UInt64;
            }
            else if (value is double)
            {
                return NodeType.Float64;
            }
            else if (context.CanUseNodeType(NodeType.FloatArray) && value is float[])
            {
                return NodeType.FloatArray;
            }
            else if (context.CanUseNodeType(NodeType.BinaryData) && value is byte[])
            {
                return NodeType.BinaryData;
            }
            else if (value is List<object>)
            {
                return NodeType.Array;
            }
            else if (value is Dictionary<string, object>)
            {
                return NodeType.Dictionary;
            }
            else
            {
                throw new ArgumentException($"Unsupported value type: {value.GetType()}");
            }
        }

        private static void WriteComplexValueArray(WriteContext context, List<object> array)
        {
            var stream = context.Stream;
            var numEntries = array.Count;

            WriteHeader(context, NodeType.Array, numEntries);
            
            // Write child value types
            foreach (var item in array)
            {
                stream.WriteUInt8((byte)ClassifyNodeValue(context, item), stream.Position);
                stream.Seek(stream.Position + 1);
            }
            
            stream.Align(4);

            var headerIdx = stream.Position;
            var headerSize = 4 * numEntries;
            stream.Seek(stream.Position + (uint)headerSize);

            foreach (var item in array)
            {
                WriteValue(context, ClassifyNodeValue(context, item), item, headerIdx);
                headerIdx += 4;
            }
        }

        private static void WriteComplexValueDict(WriteContext context, Dictionary<string, object> dict)
        {
            var stream = context.Stream;
            var keys = dict.Keys.ToList();
            var numEntries = keys.Count;

            WriteHeader(context, NodeType.Dictionary, numEntries);
            
            var headerIdx = stream.Position;
            var headerSize = 8 * numEntries;
            stream.Seek(stream.Position + (uint)headerSize);

            foreach (var key in keys)
            {
                var value = dict[key];
                var nodeType = ClassifyNodeValue(context, value);

                var keyStrIndex = StrTableIndex(context.StrKeyTable, key);
                stream.WriteUInt24((uint)keyStrIndex, headerIdx, context.LittleEndian);
                stream.WriteUInt8((byte)nodeType, headerIdx + 3);
                WriteValue(context, nodeType, value, headerIdx + 4);
                headerIdx += 8;
            }
        }

        private static void WriteComplexValueFloatArray(WriteContext context, float[] array)
        {
            var stream = context.Stream;
            WriteHeader(context, NodeType.FloatArray, array.Length);
            
            foreach (var value in array)
            {
                stream.WriteFloat32(value, stream.Position, context.LittleEndian);
                stream.Seek(stream.Position + 4);
            }
        }

        private static void WriteComplexValueBinary(WriteContext context, byte[] data)
        {
            var stream = context.Stream;
            
            if (data.Length >= 0x00FFFFFF)
            {
                WriteHeader(context, NodeType.BinaryData, 0x00FFFFFF);
                var numValues2 = data.Length - 0x00FFFFFF;
                stream.WriteUInt32((uint)numValues2, stream.Position, context.LittleEndian);
                stream.Seek(stream.Position + 4);
            }
            else
            {
                WriteHeader(context, NodeType.BinaryData, data.Length);
            }
            
            stream.WriteBuffer(data, stream.Position, data.Length);
            stream.Align(4);
        }

        private static void WriteValue(WriteContext context, NodeType nodeType, object value, uint valueOffset)
        {
            var stream = context.Stream;

            if (value == null)
            {
                stream.WriteUInt32(0, valueOffset, context.LittleEndian);
            }
            else if (value is bool b)
            {
                stream.WriteUInt32(b ? 1u : 0u, valueOffset, context.LittleEndian);
            }
            else if (value is string s)
            {
                var index = StrTableIndex(context.StrValueTable, s);
                stream.WriteUInt32((uint)index, valueOffset, context.LittleEndian);
            }
            else if (value is int i)
            {
                stream.WriteInt32(i, valueOffset, context.LittleEndian);
            }
            else if (value is uint u)
            {
                stream.WriteUInt32(u, valueOffset, context.LittleEndian);
            }
            else if (value is float f)
            {
                stream.WriteFloat32(f, valueOffset, context.LittleEndian);
            }
            else if (value is long l)
            {
                stream.WriteInt64(l, valueOffset, context.LittleEndian);
            }
            else if (value is ulong ul)
            {
                stream.WriteUInt64(ul, valueOffset, context.LittleEndian);
            }
            else if (value is double d)
            {
                stream.WriteFloat64(d, valueOffset, context.LittleEndian);
            }
            else if (context.CanUseNodeType(NodeType.FloatArray) && value is float[] floatArray)
            {
                stream.WriteUInt32(stream.Position, valueOffset, context.LittleEndian);
                WriteComplexValueFloatArray(context, floatArray);
            }
            else if (context.CanUseNodeType(NodeType.BinaryData) && value is byte[] byteArray)
            {
                stream.WriteUInt32(stream.Position, valueOffset, context.LittleEndian);
                WriteComplexValueBinary(context, byteArray);
            }
            else if (value is List<object> list)
            {
                stream.WriteUInt32(stream.Position, valueOffset, context.LittleEndian);
                WriteComplexValueArray(context, list);
            }
            else if (value is Dictionary<string, object> dict)
            {
                stream.WriteUInt32(stream.Position, valueOffset, context.LittleEndian);
                WriteComplexValueDict(context, dict);
            }
            else
            {
                throw new ArgumentException($"Unsupported value type: {value.GetType()}");
            }
        }

        private static void GatherStrings(object value, HashSet<string> keyStrings, HashSet<string> valueStrings)
        {
            if (value == null || value is int || value is uint || value is long || value is ulong || 
                value is float || value is double || value is bool || value is float[] || value is byte[])
            {
                return;
            }
            else if (value is string s)
            {
                valueStrings.Add(s);
            }
            else if (value is List<object> list)
            {
                foreach (var item in list)
                    GatherStrings(item, keyStrings, valueStrings);
            }
            else if (value is Dictionary<string, object> dict)
            {
                foreach (var key in dict.Keys)
                    keyStrings.Add(key);
                foreach (var val in dict.Values)
                    GatherStrings(val, keyStrings, valueStrings);
            }
            else
            {
                throw new ArgumentException($"Unsupported value type: {value.GetType()}");
            }
        }

        private static int BymlStrCompare(string a, string b)
        {
            if (a == "")
                return 1;
            else if (b == "")
                return -1;
            else
                return string.Compare(a, b, StringComparison.Ordinal);
        }

        private static void WriteStringTable(WriteContext context, List<string> strings)
        {
            var stream = context.Stream;
            var numEntries = strings.Count - 1;

            WriteHeader(context, NodeType.StringTable, numEntries);

            // Strings should already be sorted
            var strDataIdx = 4u; // Header
            foreach (var s in strings)
                strDataIdx += 4;

            foreach (var s in strings)
            {
                stream.WriteUInt32(strDataIdx, stream.Position, context.LittleEndian);
                stream.Seek(stream.Position + 4);
                strDataIdx += (uint)(s.Length + 1);
            }

            foreach (var s in strings)
            {
                stream.WriteString(s + "\0", stream.Position);
                stream.Seek(stream.Position + (uint)(s.Length + 1));
            }
        }

        public static byte[] Write<T>(T value, FileType fileType = FileType.CRG1, string magic = null) where T : class
        {
            var stream = new WritableStream();
            var magics = _fileDescriptions[fileType].Magics;

            if (magic != null)
            {
                if (!magics.Contains(magic))
                    throw new ArgumentException("Invalid magic");
            }
            else
            {
                magic = magics[magics.Length - 1];
            }

            if (magic.Length != 4)
                throw new ArgumentException("Magic must be 4 characters");

            var littleEndian = magic.Substring(0, 2) == "YB";
            var endianness = littleEndian ? Endianness.LittleEndian : Endianness.BigEndian;

            var keyStringSet = new HashSet<string> { "" };
            var valueStringSet = new HashSet<string> { "" };
            GatherStrings(value, keyStringSet, valueStringSet);

            var keyStrings = keyStringSet.ToList();
            var valueStrings = valueStringSet.ToList();
            keyStrings.Sort(BymlStrCompare);
            valueStrings.Sort(BymlStrCompare);

            var context = new WriteContext(stream, fileType, endianness, keyStrings, valueStrings);
            stream.WriteString(magic, 0);

            stream.Seek(0x10);
            var keyStringTableOffset = stream.Position;
            stream.WriteUInt32(keyStringTableOffset, 0x04, context.LittleEndian);
            WriteStringTable(context, keyStrings);
            stream.Align(4);
            
            var valueStringTableOffset = stream.Position;
            stream.WriteUInt32(valueStringTableOffset, 0x08, context.LittleEndian);
            WriteStringTable(context, valueStrings);
            stream.Align(4);
            
            var rootNodeOffset = stream.Position;
            stream.WriteUInt32(rootNodeOffset, 0x0C, context.LittleEndian);
            
            if (value is Dictionary<string, object> dict)
            {
                WriteComplexValueDict(context, dict);
            }
            else
            {
                throw new ArgumentException("Root value must be a dictionary");
            }

            return stream.FinalizeBuffer();
        }
    }
}