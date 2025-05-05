using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualPhenix.Nintendo64
{
    public interface ISpeciesTypedArray
    {
        string Species { get; }
        TypedArrayKind Kind { get; }
    }

    public enum TypedArrayKind
    {
        Int8,
        Uint8,
        Int16,
        Uint16,
        Int32,
        Uint32,
        Int64,
        Uint64,
        Float32,
        Float64
    }
}