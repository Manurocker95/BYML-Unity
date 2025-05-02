using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualPhenix.PokemonSnapRipper
{
    public class SimpleBymlNode<T> : IBymlNode
    {
        public T Value { get; }

        public SimpleBymlNode(T value)
        {
            Value = value;
        }
    }
}