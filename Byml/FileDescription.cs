using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualPhenix.PokemonSnapRipper
{
    public class FileDescription
    {
        public string[] Magics { get; set; }
        public NodeType[] AllowedNodeTypes { get; set; }

        public FileDescription(string[] magics, NodeType[] allowedNodeTypes)
        {
            Magics = magics;
            AllowedNodeTypes = allowedNodeTypes;
        }
    }
}