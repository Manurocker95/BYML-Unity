using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualPhenix.PokemonSnapRipper
{
    public class StringTable : List<string>, IBymlNode
    {
        public StringTable() { }

        public StringTable(IEnumerable<string> strings) : base(strings) { }
    }
}
