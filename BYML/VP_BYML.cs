using PlasticPipe.PlasticProtocol.Server.Stubs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

namespace VirtualPhenix.Nintendo64
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

    public class FileDescription
    {
        public string[] Magics { get; set; }
        public NodeType[] AllowedNodeTypes { get; set; }
    }

    public static class VP_BYML
    {
        private static readonly Dictionary<FileType, FileDescription> FileDescriptions = new Dictionary<FileType, FileDescription>
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

        public static string ReadStringUTF8(VP_ArrayBufferSlice buffer, long offs)
        {
            return ReadString(buffer, offs, -1, true, encoding: "utf-8");
        }

        public class StringTable : List<string>
        {

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

            public PathPoint()
            {

            }

            public PathPoint(float px, float py, float pz, float nx, float ny, float nz, float arg)
            {
                Px = px;
                Py = py;
                Pz = pz;
                Nx = nx;
                Ny = ny;
                Nz = nz;
                Arg = arg;
            }
        }

        public class NodePath : List<PathPoint>
        {

        }

        public class PathTable : List<NodePath>
        {

        }

        public class ParseOptions
        {
            public bool HasPathTable { get; set; }
        }

        public class Node
        {
            public object Data;

            public Node(object data)
            {
                Data = data;
            }

            public virtual bool IsDataCorrectType()
            {
                return true;
            }

            public virtual bool IsNumber()
            {
                return Data is float || Data is double || Data is long || Data is ulong || Data is int || Data is uint;
            }

            public virtual bool IsNumberFull()
            {
                return IsDataObjectNumber(Data);
            }
        }

        public static bool IsDataObjectNumber(object data)
        {
            return data is float || data is double || data is long || data is ulong || data is int || data is uint || data is short || data is ushort || data is byte || data is sbyte;
        }

        public static bool IsDataCorrectTypeForSimpleNode(object data)
        {
            if (data == null)
                return true;

            return data is string ||
                data is bool ||
                data is int ||
                data is float ||
                data is double ||
                data is ushort ||
                data is short ||
                data is uint ||
                data is byte ||
                data is sbyte ||
                data is long ||
                data is ulong;
        }

        public static bool IsDataCorrectTypeForComplexNode(object data)
        {
            return data is NodeDict ||
                data is NodeArray ||
                data is NodePath ||
                data is PathTable ||
                data is StringTable ||
                data is VP_ArrayBufferSlice ||
                data is VP_Float32Array<VP_ArrayBuffer>;
        }

        public class SimpleNode : Node
        {
            public SimpleNode(object data) : base(data)
            {

            }

            public override bool IsDataCorrectType()
            {
                if (Data == null)
                    return true;

                return Data is string ||
                    Data is bool ||
                    Data is int ||
                    Data is float ||
                    Data is double ||
                    Data is ushort ||
                    Data is short ||
                    Data is uint ||
                    Data is byte ||
                    Data is sbyte ||
                    Data is long ||
                    Data is ulong;
            }
        }

        public enum ComplexNodeTypes
        {
            NodeDict,
            NodeArray,
            NodePath,
            PathTable,
            StringTable,
            VP_ArrayBufferSlice,
            VP_Float32Array
        }

        public class ComplexNode : Node
        {
            public override bool IsDataCorrectType()
            {
                return Data is NodeDict ||
                    Data is NodeArray ||
                    Data is NodePath ||
                    Data is PathTable ||
                    Data is StringTable ||
                    Data is VP_ArrayBufferSlice ||
                    Data is VP_Float32Array<VP_ArrayBuffer>;
            }

            public ComplexNode(object data) : base(data)
            {

            }
        }

        public class NodeArray : List<Node>
        {

        }

        public class NodeDict : Dictionary<string, Node>
        {

        }

        public class ParseContext
        {
            public FileType FileType { get; }
            public Endianness Endianness { get; }
            public bool LittleEndian => Endianness == Endianness.LittleEndian;
            public StringTable StrKeyTable { get; set; }
            public StringTable StrValueTable { get; set; }
            public PathTable PathTable { get; set; }

            public ParseContext(FileType fileType, Endianness endianness)
            {
                FileType = fileType;
                Endianness = endianness;
            }
        }

        public static long GetUint24<T>(VP_DataView<T> view, long offs, bool littleEndian) where T : IArrayBufferLike
        {
            byte b0 = view.GetUint8(offs + 0x00);
            byte b1 = view.GetUint8(offs + 0x01);
            byte b2 = view.GetUint8(offs + 0x02);

            if (littleEndian)
            {
                return ((long)b2 << 16) | ((long)b1 << 8) | (long)b0;
            }
            else
            {
                return ((long)b0 << 16) | ((long)b1 << 8) | (long)b2;
            }
        }

        public static StringTable ParseStringTable(ParseContext context, VP_ArrayBufferSlice buffer, long offs)
        {
            var view = buffer.CreateDataView();
            var nodeType = (NodeType)view.GetUint8(offs + 0x00);
            var numValues = GetUint24(view, offs + 0x01, context.LittleEndian);
            if (nodeType != NodeType.StringTable)
            {
                throw new InvalidOperationException("Invalid node type. Expected StringTable.");
            }

            var stringTableIdx = offs + 0x04;
            var strings = new StringTable();

            // Leemos cada cadena en la tabla de cadenas
            for (long i = 0; i < numValues; i++)
            {
                long strOffs = offs + view.GetUint32(stringTableIdx, context.LittleEndian);
                strings.Add(ReadStringUTF8(buffer, strOffs));
                stringTableIdx += 0x04;
            }

            return strings;
        }

        public static NodeDict ParseDict(ParseContext context, VP_ArrayBufferSlice buffer, long offs)
        {
            var view = buffer.CreateDataView();
            byte nodeType = view.GetUint8(offs + 0x00);
            long numValues = GetUint24(view, offs + 0x01, context.LittleEndian);

            if ((NodeType)nodeType != NodeType.Dictionary)
                throw new InvalidOperationException("Invalid node type. Expected Dictionary.");

            var result = new NodeDict();
            long dictIdx = offs + 0x04;

            // Leer cada entrada en el diccionario
            for (long i = 0; i < numValues; i++)
            {
                long entryStrKeyIdx = GetUint24(view, dictIdx + 0x00, context.LittleEndian);

                // Obtener la clave del diccionario
                string entryKey = context.StrKeyTable[(int)entryStrKeyIdx];

                NodeType entryNodeType = (NodeType)view.GetUint8(dictIdx + 0x03);

                // Parsear el valor del nodo usando el tipo de nodo
                var entryValue = ParseNode(context, buffer, entryNodeType, dictIdx + 0x04);

                // Almacenar la clave y el valor en el diccionario
                result[entryKey] = entryValue;

                dictIdx += 0x08; // Avanzar al siguiente nodo
            }

            return result;
        }
        public static long Align(long n, long multiple)
        {
            // Asegurarse de que el múltiplo es una potencia de dos
            if ((multiple & (multiple - 1)) != 0)
            {
                throw new ArgumentException("Multiple must be a power of two.");
            }

            long mask = multiple - 1;
            return (n + mask) & ~mask;
        }

        public static NodeArray ParseArray(ParseContext context, VP_ArrayBufferSlice buffer, long offs)
        {
            var view = buffer.CreateDataView();
            byte nodeType = view.GetUint8(offs + 0x00);
            long numValues = GetUint24(view, offs + 0x01, context.LittleEndian);

            if ((NodeType)nodeType != NodeType.Array)
                throw new InvalidOperationException("Invalid node type. Expected Array.");

            var result = new NodeArray();
            long entryTypeIdx = offs + 0x04;
            var entryOffsIdx = Align(entryTypeIdx + numValues, 4);

            // Leer cada entrada en el diccionario
            for (long i = 0; i < numValues; i++)
            {
                var entryNodeType = (NodeType)view.GetUint8(entryTypeIdx);
                result.Add(ParseNode(context, buffer, entryNodeType, entryOffsIdx));
                entryTypeIdx++;
                entryOffsIdx += 0x04;
            }

            return result;
        }

        public static PathTable ParsePathTable(ParseContext context, VP_ArrayBufferSlice buffer, long offs)
        {
            var view = buffer.CreateDataView();
            byte nodeType = view.GetUint8(offs + 0x00);
            long numValues = GetUint24(view, offs + 0x01, context.LittleEndian);

            if ((NodeType)nodeType != NodeType.PathTable)
                throw new InvalidOperationException("Invalid node type. Expected PathTable.");

            var result = new PathTable();
            for (long i = 0; i < numValues; i++)
            {
                long startOffs = offs + view.GetUint32(offs + 0x04 + 0x04 * (i + 0), context.LittleEndian);
                long endOffs = offs + view.GetUint32(offs + 0x04 + 0x04 * (i + 1), context.LittleEndian);

                var path = new NodePath();
                for (long j = startOffs; j < endOffs; j += 0x1C)
                {
                    var px = view.GetFloat32(j + 0x00, context.LittleEndian);
                    var py = view.GetFloat32(j + 0x04, context.LittleEndian);
                    var pz = view.GetFloat32(j + 0x08, context.LittleEndian);
                    var nx = view.GetFloat32(j + 0x0C, context.LittleEndian);
                    var ny = view.GetFloat32(j + 0x10, context.LittleEndian);
                    var nz = view.GetFloat32(j + 0x14, context.LittleEndian);
                    var arg = view.GetFloat32(j + 0x18, context.LittleEndian);
                    path.Add(new PathPoint(px, py, pz, nx, ny, nz, arg));
                }
                result.Add(path);
            }

            return result;
        }

        public static ComplexNode ParseComplexNode(ParseContext context, VP_ArrayBufferSlice buffer, long offs, NodeType? expectedNodeType)
        {
            var view = buffer.CreateDataView();
            NodeType nodeType = (NodeType)view.GetUint8(offs + 0x00);
            long numValues = GetUint24(view, offs + 0x01, context.LittleEndian);

            if (expectedNodeType != null && expectedNodeType != nodeType)
                throw new InvalidOperationException("Invalid node type. Expected NodeType with value " + expectedNodeType);

            switch (nodeType)
            {
                case NodeType.Dictionary:
                    return new ComplexNode(ParseDict(context, buffer, offs));
                case NodeType.Array:
                    return new ComplexNode(ParseArray(context, buffer, offs));
                case NodeType.StringTable:
                    return new ComplexNode(ParseStringTable(context, buffer, offs));
                case NodeType.PathTable:
                    return new ComplexNode(ParsePathTable(context, buffer, offs));
                case NodeType.BinaryData:
                    if (numValues == 0x00FFFFFF)
                    {
                        var numValues2 = view.GetUint32(offs + 0x04, context.LittleEndian);
                        return new ComplexNode(buffer.Subarray(offs + 0x08, numValues + numValues2));
                    }
                    else
                    {
                        return new ComplexNode(buffer.Subarray(offs + 0x04, numValues));
                    }
                case NodeType.FloatArray:
                    return new ComplexNode(buffer.CreateTypedArray<VP_ArrayBuffer>(TypedArrayKind.Float32, offs + 0x04, numValues, context.Endianness));
                default:
                    throw new InvalidOperationException("whoops");
            }
        }

        private static void ValidateNodeType(ParseContext context, NodeType nodeType)
        {
            if (!FileDescriptions[context.FileType].AllowedNodeTypes.Contains(nodeType))
                throw new Exception($"Node type {nodeType} not allowed for file type {context.FileType}");
        }

        public static void Assert(bool condition, string message = "")
        {
            if (!condition)
            {
                UnityEngine.Debug.LogError(message);
                // Imprimir el stack trace de la excepción actual
                UnityEngine.Debug.LogError(new Exception().StackTrace);

                // Lanzar la excepción con un mensaje personalizado
                throw new Exception($"Assert failed: {message}");
            }
        }

        public static Node ParseNode(ParseContext context, VP_ArrayBufferSlice buffer, NodeType nodeType, long offs)
        {
            var view = buffer.CreateDataView();
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
                        var complexOffs = view.GetUint32(offs, context.LittleEndian);
                        return ParseComplexNode(context, buffer, complexOffs, nodeType);
                    }
                case NodeType.String:
                    {
                        var idx = view.GetUint32(offs, context.LittleEndian);
                        return new SimpleNode(context.StrValueTable[(int)idx]);
                    }
                case NodeType.Path:
                    {
                        var idx = view.GetUint32(offs, context.LittleEndian);
                        return new ComplexNode(context.PathTable[(int)idx]);
                    }
                case NodeType.Bool:
                    {
                        var value = view.GetUint32(offs, context.LittleEndian);
                        Assert(value == 0 || value == 1);

                        return new SimpleNode(value);
                    }
                case NodeType.Int:
                    return new SimpleNode(view.GetInt32(offs, context.LittleEndian));
                case NodeType.UInt:
                    return new SimpleNode(view.GetUint32(offs, context.LittleEndian));
                case NodeType.Float:
                    return new SimpleNode(view.GetFloat32(offs, context.LittleEndian));
                case NodeType.Int64:
                    return new SimpleNode(view.GetBigInt64(offs, context.LittleEndian));
                case NodeType.UInt64:
                    return new SimpleNode(view.GetBigUint64(offs, context.LittleEndian));
                case NodeType.Float64:
                    return new SimpleNode(view.GetFloat64(offs, context.LittleEndian));
                case NodeType.Null:
                    return null;
                default:
                    throw new Exception($"Error parsing node!");
            }
        }

        public static T MapToObject<T>(NodeDict data) where T : new() 
        {
            T obj = new T();
            Type type = typeof(T);

            foreach (var kvp in data)
            {
                var field = type.GetField(kvp.Key, BindingFlags.Public | BindingFlags.Instance);
                if (field != null && kvp.Value != null)
                {
                    try
                    {
                        object value = Convert.ChangeType(kvp.Value.Data, field.FieldType);
                        field.SetValue(obj, value);
                    }
                    catch
                    {
                        // Opcional: manejar errores de conversión
                    }
                }

                // Alternativamente, también soporta propiedades públicas
                var property = type.GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.Instance);
                if (property != null && property.CanWrite && kvp.Value != null)
                {
                    try
                    {
                        object value = Convert.ChangeType(kvp.Value.Data, property.PropertyType);
                        property.SetValue(obj, value);
                    }
                    catch
                    {
                        // Opcional: manejar errores de conversión
                    }
                }
            }

            return obj;
        }

        public static object Parse<T>(byte[] bufferBytes, FileType fileType = FileType.BYML, ParseOptions opt = null, bool _mapToObject = true) where T: new()
        {
            return Parse<T>(new VP_ArrayBufferSlice(bufferBytes), fileType, opt);
        }

        public static object Parse<T>(VP_ArrayBufferSlice buffer, FileType fileType = FileType.BYML, ParseOptions opt = null, bool _mapToObject = true) where T : new()
        {
            string magic = ReadString(buffer, 0x00, 0x04, false);
            var magics = FileDescriptions[fileType].Magics;

            Assert(magics.Contains(magic));
            var view = buffer.CreateDataView();
            bool littleEndian = magic.Substring(0, 2) == "YB";
            Endianness endianness = littleEndian ? Endianness.LittleEndian : Endianness.BigEndian;
            ParseContext context = new ParseContext(fileType, endianness);

            long strKeyTableOffs = view.GetUint32(0x04, context.LittleEndian);
            long strValueTableOffs = view.GetUint32(0x08, context.LittleEndian);
            long headerOffs = 0x0C;
            long pathTableOffs = 0;

            if (opt != null && opt.HasPathTable)
            {
                pathTableOffs = view.GetUint32(headerOffs + 0x00, context.LittleEndian);
                headerOffs += 0x04;
            }
            var rootNodeOffs = view.GetUint32(headerOffs + 0x00, context.LittleEndian);

            if (rootNodeOffs == 0)
                return null;

            context.StrKeyTable = strKeyTableOffs != 0 ? ParseStringTable(context, buffer, strKeyTableOffs) : null;
            context.StrValueTable = strValueTableOffs != 0 ? ParseStringTable(context, buffer, strValueTableOffs) : null;
            context.PathTable = pathTableOffs != 0 ? ParsePathTable(context, buffer, pathTableOffs) : null;

            var node = ParseComplexNode(context, buffer, rootNodeOffs, null);
            NodeDict dict = (NodeDict)node.Data;

            object ret = null;

            if (_mapToObject && (typeof(T) != typeof(NodeDict)))
            {
                try
                {
                    T rr = MapToObject<T>(dict);
                    ret = rr;
                }
                catch
                {
                    ret = dict;
                }
            }
            else
            {
                ret = dict;
            }

            return ret;
        }

        public static string ReadString(VP_ArrayBufferSlice buffer, long offs, long length = -1, bool nulTerminated = true, string encoding = null)
        {
            // Crear un TypedArray (equivalente a un Uint8Array en C#)
            var buf = buffer.CreateTypedArray<VP_ArrayBuffer>(TypedArrayKind.Uint8, offs);

            if (length < 0)
                length = buf.LongLength; 

            long byteLength = 0;

            // Leer byte por byte hasta encontrar el terminador nulo (0)
            while (true)
            {
                if (byteLength >= length)
                    break;

                if (nulTerminated)
                {
                    var byt = buffer.Buffer.Buffer[offs + byteLength];
                   
                    if (byt == 0)
                    {
                        break;
                    }
                }
                
                byteLength++;
            }

            if (byteLength == 0)
                return string.Empty;

            if (encoding != null)
            {
                return DecodeString(buffer, offs, byteLength, encoding);
            }
            else
            {
                return CopyBufferToString(buffer, offs, byteLength);
            }
        }

        public static string DecodeString(VP_ArrayBufferSlice buffer, long offs, long byteLength, string encoding)
        {
            var arrayBuffer = buffer.CopyToBuffer(offs, byteLength);
            byte[] byteArray = arrayBuffer.Buffer;

            var str = Encoding.UTF8.GetString(byteArray);

            //UnityEngine.Debug.Log("Parsed in decode string " + str + " - blength:" + byteLength);

            return str;
        }

        public static string CopyBufferToString(VP_ArrayBufferSlice buffer, long offs, long byteLength)
        {
            var buf = buffer.CreateTypedArray<VP_ArrayBuffer>(TypedArrayKind.Uint8, offs, byteLength);

            var nl = byteLength;

            if (offs + nl > buffer.Buffer.ByteLength)
            {
                nl = buffer.Buffer.ByteLength - offs;  // Ajustar la longitud si se excede el tamaño
            }

            var byteArray = new byte[nl];
            Array.Copy(buffer.Buffer.Buffer, offs, byteArray, 0, byteLength);

            var str = Encoding.UTF8.GetString(byteArray);

            // UnityEngine.Debug.Log("Parsed in copy to buffer " + str);

            return str;
        }

        public class GrowableBuffer
        {
            public VP_ArrayBuffer Buffer;
            public VP_DataView<VP_ArrayBuffer> View;
            public long UserSize = 0;
            public long BufferSize = 0;
            public long GrowAmount = 0;

            public GrowableBuffer()
            {
                Buffer = new VP_ArrayBuffer();
                View = new VP_DataView<VP_ArrayBuffer>(Buffer);
            }

            public GrowableBuffer(long initialSize = 0x10000, long growAmount = 0x1000)
            {
                Buffer = new VP_ArrayBuffer();
                View = new VP_DataView<VP_ArrayBuffer>(Buffer);
                GrowAmount = growAmount;

                MaybeGrow(0, initialSize);
            }

            public void MaybeGrow(long newBufferSize, long? newUserSize = null)
            { 
                if (newUserSize == null)
                    newUserSize = newBufferSize;

                if (newUserSize > UserSize)
                    UserSize = newUserSize.Value;

                if (newBufferSize > BufferSize)
                {
                    BufferSize = Align(newBufferSize, GrowAmount);
                    Buffer = Buffer.Transfer(BufferSize);
                    View = new VP_DataView<VP_ArrayBuffer>(Buffer);
                }
            }

            public VP_ArrayBuffer Finalize()
            {
                return Buffer.Transfer(UserSize);
            }
        }

        public static void SetUint24(VP_DataView<VP_ArrayBuffer> view, long offs, long v, bool littleEndian)
        {
            if (littleEndian)
            {
                view.SetUint8(offs + 0x00, (byte)(v & 0xFF));
                view.SetUint8(offs + 0x01, (byte)((v >> 8) & 0xFF));
                view.SetUint8(offs + 0x02, (byte)((v >> 16) & 0xFF));
            }
            else
            {
                view.SetUint8(offs + 0x00, (byte)((v >> 16) & 0xFF));
                view.SetUint8(offs + 0x01, (byte)((v >> 8) & 0xFF));
                view.SetUint8(offs + 0x02, (byte)(v & 0xFF));
            }
        }

        public class WritableStream
        {
            public GrowableBuffer Buffer;
            public long Offs;

            public WritableStream(GrowableBuffer buffer = null, long offs = 0)
            {
                if (buffer == null)
                {
                    buffer = new GrowableBuffer();
                }

                Buffer = buffer;
                Offs = offs;
            }

            public void SetBufferSlice(long offs, VP_ArrayBufferSlice src)
            {
                Buffer.MaybeGrow(offs + src.ByteLength);

                var cp = src.CreateTypedArray<VP_ArrayBuffer>(TypedArrayKind.Uint8);
                var arrayLike = new VP_ArrayLike()
                {
                    Length = cp.ByteLength,
                    N = cp.Buffer.ToList()
                };
                var v = new VP_Uint8Array<VP_ArrayBuffer>(Buffer.Buffer, Offs);
                v.Set(arrayLike);
                Buffer.Buffer = v.BufferSource;
            }

            public void WriteBufferSlice(VP_ArrayBufferSlice src)
            {
                SetBufferSlice(Offs, src);
                Offs += src.ByteLength;
            }

            public void SetString(long offs, string v)
            {
                Buffer.MaybeGrow(offs + v.Length);
                var a = new VP_Uint8Array<VP_ArrayBuffer>(Buffer.Buffer, Offs);
                for (long i = 0; i < v.Length; i++)
                    a[i] = v.ElementAt((int)i);
            }

            public void WriteString(string v)
            {
                SetString(Offs, v);
                Offs += v.Length;
            }

            public void WriteFixedString(string v, long s)
            {
                Assert(v.Length < s);
                SetString(Offs, v);
                Offs += s;
            }

            public void SetUint8(long offs, long v)
            {
                Buffer.MaybeGrow(offs + 0x01);
                Buffer.View.SetUint8(offs, (byte)v);
            }

            public void WriteUint8(byte v)
            {
                SetUint8(Offs, v);
                Offs += 0x01;
            }

            public void SetUint24(long offs, long v, bool littleEndian)
            {
                Buffer.MaybeGrow(offs + 0x03);
                VP_BYML.SetUint24(Buffer.View, offs, v, littleEndian);
            }

            public void WriteUint24(byte v, bool littleEndian)
            {
                SetUint24(Offs, v, littleEndian);
                Offs += 0x03;
            }

            public void SetUint32(long offs, long v, bool littleEndian)
            {
                Buffer.MaybeGrow(offs + 0x04);
                Buffer.View.SetUint32(offs, (uint)v, littleEndian);
            }

            public void WriteUint32(byte v, bool littleEndian)
            {
                SetUint32(Offs, v, littleEndian);
                Offs += 0x04;
            }

            public void SetInt32(long offs, long v, bool littleEndian)
            {
                Buffer.MaybeGrow(offs + 0x04);
                Buffer.View.SetInt32(offs, (int)v, littleEndian);
            }

            public void WriteInt32(byte v, bool littleEndian)
            {
                SetInt32(Offs, v, littleEndian);
                Offs += 0x04;
            }

            public void SetFloat32(long offs, long v, bool littleEndian)
            {
                Buffer.MaybeGrow(offs + 0x04);
                Buffer.View.SetFloat32(offs, (int)v, littleEndian);
            }

            public void WriteFloat32(byte v, bool littleEndian)
            {
                SetFloat32(Offs, v, littleEndian);
                Offs += 0x04;
            }

            public void SeekTo(long n)
            {
                Offs = n;
                Buffer.MaybeGrow(Offs);
            }

            public void Align(long m)
            {
                SeekTo(VP_BYML.Align(Offs, m));
            }

            public VP_ArrayBuffer Finalize()
            {
                return Buffer.Finalize();
            }
        }


        public class WriteContext
        {
            public WritableStream Stream { get; }
            public FileType FileType { get; }
            public Endianness Endianness { get; }
            public bool LittleEndian => Endianness == Endianness.LittleEndian;
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

            public bool CanUseNodeType(NodeType type)
            {
                return FileDescriptions[FileType].AllowedNodeTypes.Contains(type);
            }
        }

        public static int StrTableIndex(StringTable table, string value)
        {
            var index = table.IndexOf(value);
            Assert(index >= 0);

            return index;
        }


        public static void WriteHeader(WriteContext w, NodeType nodeType, long numEntries)
        {
            var stream = w.Stream;
            stream.WriteUint8((byte)nodeType);
            stream.WriteUint24((byte)numEntries, w.LittleEndian);
        }

        public static NodeType ClassifyNodeValue(WriteContext w, Node v)
        {
            if (v == null)
            {
                return NodeType.Null;
            }
            else if (v.Data is bool)
            {
                return NodeType.Bool;
            }
            else if (v.Data is string)
            {
                return NodeType.String;
            }
            else if (v.Data is int)
            {
                return NodeType.Int;
            }
            else if (v.Data is uint)
            {
                return NodeType.UInt;
            }
            else if (v.Data is float)
            {
                return NodeType.Float;
            }
            else if (v.Data is long)
            {
                return NodeType.Int64;
            }
            else if (v.Data is ulong)
            {
                return NodeType.UInt64;
            }
            else if (v.Data is double)
            {
                return NodeType.Float64;
            }
            else if (w.CanUseNodeType(NodeType.FloatArray) && v.Data is VP_Float32Array<VP_ArrayBuffer>)
            {
                return NodeType.FloatArray;
            }
            else if (w.CanUseNodeType(NodeType.BinaryData) && v.Data is VP_ArrayBufferSlice)
            {
                return NodeType.BinaryData;
            }
            else if (v.Data is NodeArray)
            {
                return NodeType.Array;
            }
            else if (v.Data is NodeDict || v.Data is object)
            {
                return NodeType.Dictionary;
            }
            else
            {
                throw new ArgumentException($"Unsupported value type: {v.Data.GetType()}");
            }
        }

        private static void WriteComplexValueArray(WriteContext w, NodeArray v)
        {
            var stream = w.Stream;

            var numEntries = v.Count;
            WriteHeader(w, NodeType.Array, numEntries);
            
            // First up is child value types.
            for (int i = 0; i < v.Count; i++)
                stream.WriteUint8((byte)ClassifyNodeValue(w, v[i]));

            stream.Align(0x04);

            var headerIdx = stream.Offs;
            long headerSize = 0x04 * numEntries;
            stream.SeekTo(stream.Offs + headerSize);

            for (int i = 0; i < v.Count; i++)
            {
                WriteValue(w, ClassifyNodeValue(w, v.ElementAt(i)), v[i], headerIdx + 0x00);
                headerIdx += 0x04;
            }
        }

        private static void WriteComplexValueDict(WriteContext w, object t)
        {
            if (!(t is NodeDict))
            {
                return;
            }

            NodeDict v = (NodeDict)t;

            var stream = w.Stream;

            var keys = v.Keys;
            var numEntries = keys.Count;

            WriteHeader(w, NodeType.Dictionary, numEntries);

            // Write our children values, then go back and write our header.
            // Each header item is 0x08 bytes.
            var headerIdx = stream.Offs;

            long headerSize = 0x08 * numEntries;
            stream.SeekTo(stream.Offs + headerSize);

            for (int i = 0; i < keys.Count; i++)
            {
                var key = keys.ElementAt(i);
                Node childValue = v[key];
                NodeType nodeType = ClassifyNodeValue(w, childValue);

                long keyStrIndex = StrTableIndex(w.StrKeyTable, key);
                stream.SetUint24(headerIdx + 0x00, keyStrIndex, w.LittleEndian);
                stream.SetUint8(headerIdx + 0x03, (byte)nodeType);
                WriteValue(w, nodeType, childValue, headerIdx + 0x04);
                headerIdx += 0x08;
            }
        }

        private static void WriteComplexValueDict(WriteContext w, NodeDict v)
        {
            var stream = w.Stream;

            var keys = v.Keys;
            var numEntries = keys.Count;

            WriteHeader(w, NodeType.Dictionary, numEntries);

            // Write our children values, then go back and write our header.
            // Each header item is 0x08 bytes.
            var headerIdx = stream.Offs;

            long headerSize = 0x08 * numEntries;
            stream.SeekTo(stream.Offs + headerSize);

            for (int i = 0; i < keys.Count; i++)
            {
                var key = keys.ElementAt(i);
                Node childValue = v[key];
                NodeType nodeType = ClassifyNodeValue(w, childValue);

                long keyStrIndex = StrTableIndex(w.StrKeyTable, key);
                stream.SetUint24(headerIdx + 0x00, keyStrIndex, w.LittleEndian);
                stream.SetUint8(headerIdx + 0x03, (byte)nodeType);
                WriteValue(w, nodeType, childValue, headerIdx + 0x04);
                headerIdx += 0x08;
            }
        }

        private static void WriteComplexValueFloatArray(WriteContext w, VP_Float32Array<VP_ArrayBuffer> v)
        {
            var stream = w.Stream;
            WriteHeader(w, NodeType.Float64, v.Buffer.Length);
            for (int i = 0; i < v.Buffer.Length; i++)
                stream.WriteFloat32((byte)v[i], w.LittleEndian);
        }

        private static void WriteComplexValueBinary(WriteContext w, VP_ArrayBufferSlice v)
        {
            var stream = w.Stream;
            if (v.ByteLength >= 0x00FFFFFF)
            {
                WriteHeader(w, NodeType.BinaryData, 0x00FFFFFF);
                long numValues2 = v.ByteLength - 0x00FFFFFF;
                Assert(numValues2 <= 0xFFFFFFFF);
                stream.WriteUint32((byte)numValues2, w.LittleEndian);
            }
            else
            {
                WriteHeader(w, NodeType.BinaryData, v.ByteLength);
            }
            stream.WriteBufferSlice(v);
            stream.Align(0x04);
        }

        private static void WriteValue(WriteContext w, NodeType nodeType, Node v, long valueOffs)
        {
            var stream = w.Stream;

            if (v == null || v.Data == null)
            {
                stream.SetUint32(valueOffs, 0x00, w.LittleEndian);
            }
            else if (v.Data is bool)
            {
                stream.SetUint32(valueOffs, (bool)v.Data ? 0x01 : 0x00, w.LittleEndian);
            }
            else if (v.Data is string)
            {
                stream.SetUint32(valueOffs, StrTableIndex(w.StrValueTable, (string)v.Data), w.LittleEndian);
            }
            else if (v.IsNumberFull())
            {
                if (nodeType == NodeType.Float)
                    stream.SetFloat32(valueOffs, (long)v.Data, w.LittleEndian);
                else if (nodeType == NodeType.UInt)
                    stream.SetUint32(valueOffs, (long)v.Data, w.LittleEndian);
                else
                    stream.SetInt32(valueOffs, (long)v.Data, w.LittleEndian);
            }
            else if (w.CanUseNodeType(NodeType.FloatArray) && v.Data is VP_Float32Array<VP_ArrayBuffer>)
            {
                stream.SetUint32(valueOffs, stream.Offs, w.LittleEndian);
                WriteComplexValueFloatArray(w, (VP_Float32Array<VP_ArrayBuffer>)v.Data);
            }
            else if (w.CanUseNodeType(NodeType.BinaryData) && v.Data is VP_ArrayBufferSlice)
            {
                stream.SetUint32(valueOffs, stream.Offs, w.LittleEndian);
                WriteComplexValueBinary(w, (VP_ArrayBufferSlice)v.Data);
            }
            else if (v.Data is NodeArray) {
                stream.SetUint32(valueOffs, stream.Offs, w.LittleEndian);
                WriteComplexValueArray(w, (NodeArray)v.Data);
            }
            else if (v.Data is NodeDict)
            {
                stream.SetUint32(valueOffs, stream.Offs, w.LittleEndian);
                WriteComplexValueDict(w, (NodeDict)v.Data);
            }
            else
            {
                throw new InvalidOperationException("whoops");
            }
        }

        public static void GatherStrings(Node v, List<string> keyStrings, List<string> valueStrings)
        {
            if (v == null || v.Data == null || v.IsNumberFull() || v.Data is bool || v.Data is VP_Float32Array<VP_ArrayBuffer> || v.Data is VP_ArrayBufferSlice)
            {
                // Nothing.
                return;
            }
            else if (v.Data is string)
            {
                valueStrings.Add((string)v.Data);
            }
            else if (v.Data is NodeArray)
            {
                var parsed = (NodeArray)v.Data;
                for (int i = 0; i < parsed.Count; i++)
                {
                    GatherStrings(parsed.ElementAt(i), keyStrings, valueStrings);
                }
            }
            else if (v.Data is NodeDict)
            {
                var parsed = (NodeDict)v.Data;
                // Generic object.
                var keys = parsed.Keys;
                for (int i = 0; i < keys.Count; i++)
                {
                    keyStrings.Add(keys.ElementAt(i));
                }

                for (int i = 0; i < keys.Count; i++)
                {
                    GatherStrings(parsed[keys.ElementAt(i)], keyStrings, valueStrings);
                }
            }
            else
            {
                throw new InvalidOperationException("whoops");
            }
        }

        public static void GatherStrings(object v, List<string> keyStrings, List<string> valueStrings)
        {
            if (v == null || v is float || v is double || v is long || v is ulong || v is int || v is uint || v is short || v is ushort || v is byte || v is sbyte || v is VP_Float32Array<VP_ArrayBuffer> || v is VP_ArrayBufferSlice)
            {
                // Nothing.
                return;
            }
            else if (v is string)
            {
                valueStrings.Add((string)v);
            }
            else if (v is NodeArray)
            {
                var parsed = (NodeArray)v;
                for (int i = 0; i < parsed.Count; i++)
                {
                    GatherStrings(parsed.ElementAt(i), keyStrings, valueStrings);
                }
            }
            else if (v is NodeDict)
            {
                var parsed = (NodeDict)v;
                // Generic object.
                var keys = parsed.Keys;
                for (int i = 0; i < keys.Count; i++)
                {
                    keyStrings.Add(keys.ElementAt(i));
                }

                for (int i = 0; i < keys.Count; i++)
                {
                    GatherStrings(parsed[keys.ElementAt(i)], keyStrings, valueStrings);
                }
            }
            else
            {
                throw new InvalidOperationException("whoops");
            }
        }

        private static long BymlStrCompare(string a, string b)
        {
            if (a == "")
                return 1;
            else if (b == "")
                return -1;
            else
                return string.Compare(a, b, StringComparison.Ordinal);
        }

        private static int BymlStrCompareInt(string a, string b)
        {
            if (a == "")
                return 1;
            else if (b == "")
                return -1;
            else
                return string.Compare(a, b, StringComparison.Ordinal);
        }

        private static void WriteStringTable(WriteContext w, StringTable v)
        {
            var stream = w.Stream;

            // A string table contains at least one entry, so this field is the number of entries minus one.
            long numEntries = v.Count - 1;

            WriteHeader(w, NodeType.StringTable, numEntries);

            // Strings should already be sorted.
            long strDataIdx = 0x04; // Header
            for (int i = 0; i < v.Count; i++)
                strDataIdx += 0x04;

            for (int i = 0; i < v.Count; i++)
            {
                stream.WriteUint32((byte)strDataIdx, w.LittleEndian);
                strDataIdx += v[i].Count() + 0x01;
            }

            for (int i = 0; i < v.Count; i++)
                stream.WriteString(v[i] + '\0');
        }

        public static VP_ArrayBuffer Write(object o, FileType fileType = FileType.CRG1, string magic = null)
        {
            object v = o;

            if (o is Dictionary<string, object>)
            {
                Dictionary<string, object> parsed = (Dictionary<string, object>)o;
                NodeDict dict = new NodeDict();

                foreach (var k in parsed.Keys)
                {
                    if (IsDataCorrectTypeForSimpleNode(parsed[k]))
                    {
                        dict.Add(k, new SimpleNode(parsed[k]));
                    }
                    else if (IsDataCorrectTypeForComplexNode(parsed[k]))
                    {
                        dict.Add(k, new ComplexNode(parsed[k]));
                    }
                }

                if (dict.Count > 0)
                {
                    v = dict;
                }
            }

            var stream = new WritableStream();

            var magics = FileDescriptions[fileType].Magics;

            if (magic != null)
                Assert(magics.Contains(magic));
            else
                magic = magics[magics.Count() - 1];

            Assert(magic.Length == 0x04);

            var littleEndian = magic.Substring(0, 2) == "YB";
            var endianness = littleEndian ? Endianness.LittleEndian : Endianness.BigEndian;


            var keyStringSet = new List<string>();
            var valueStringSet = new List<string>();
            GatherStrings(v, keyStringSet, valueStringSet);

            StringTable keyStrings = new StringTable();
            keyStrings.AddRange(keyStringSet);
            StringTable valueStrings = new StringTable();
            valueStrings.AddRange(valueStringSet);
            keyStrings.Sort(BymlStrCompareInt);
            valueStrings.Sort(BymlStrCompareInt);

            var w = new WriteContext(stream, fileType, endianness, keyStrings, valueStrings);
            stream.SetString(0x00, magic);

            stream.SeekTo(0x10);
            var keyStringTableOffs = stream.Offs;
            stream.SetUint32(0x04, keyStringTableOffs, w.LittleEndian);
            WriteStringTable(w, keyStrings);
            stream.Align(0x04);
            var valueStringTableOffs = stream.Offs;
            stream.SetUint32(0x08, valueStringTableOffs, w.LittleEndian);
            WriteStringTable(w, valueStrings);
            stream.Align(0x04);
            var rootNodeOffs = stream.Offs;
            stream.SetUint32(0x0C, rootNodeOffs, w.LittleEndian);
            WriteComplexValueDict(w, v);

            return stream.Finalize();
        }
    }
}