using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualPhenix.Nintendo64
{
    /// <summary>
    /// Interfaz com�n para cualquier objeto que act�e como un buffer binario (ArrayBuffer-like).
    /// </summary>
    public interface IArrayBufferLike
    {
        byte[] Buffer { get; }
        long ByteLength { get; }
        long LongLength { get; }
        object this[long index] { get; set; }
    }
}
