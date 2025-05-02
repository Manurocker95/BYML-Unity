using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualPhenix.PokemonSnapRipper
{
    public class NodeBinaryData : IBymlNode
    {
        public byte[] Data { get; }

        public NodeBinaryData(byte[] data)
        {
            Data = data;
        }
    }

}
