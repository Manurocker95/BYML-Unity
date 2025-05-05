using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualPhenix.Nintendo64
{
    public static class VP_BYMLUtils
    {
        public static bool IsAligned(long n, long m)
        {
            return (n & (m - 1)) == 0;
        }

        public static int GetBytesPerElement(TypedArrayKind kind)
        {
            switch (kind)
            {
                case TypedArrayKind.Int8:
                case TypedArrayKind.Uint8:
                    return 1;
                case TypedArrayKind.Int16:
                case TypedArrayKind.Uint16:
                    return 2;
                case TypedArrayKind.Int32:
                case TypedArrayKind.Uint32:
                case TypedArrayKind.Float32:
                    return 4;
                case TypedArrayKind.Float64:
                    return 8;
                default:
                    return 1;
            }
        }
     
    }
}
