using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualPhenix.Nintendo64
{
    public class VP_ArrayBufferLike
    {
        public VP_ArrayBuffer ArrayBuffer;

        public VP_ArrayBufferLike()
        {

        }

        public VP_ArrayBufferLike(VP_ArrayBuffer buffer)
        {
            ArrayBuffer = buffer;
        }

        public VP_ArrayBufferLike(IArrayBufferLike buffer)
        {
            ArrayBuffer = new VP_ArrayBuffer(buffer);
        }
    }
}
