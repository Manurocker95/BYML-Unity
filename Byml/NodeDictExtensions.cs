using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualPhenix.PokemonSnapRipper
{
    public static class NodeDictExtensions
    {
        public static Dictionary<string, object> ToPlainDictionary(this NodeDict dict)
        {
            var result = new Dictionary<string, object>();
            foreach (var kvp in dict)
            {
                result[kvp.Key] = ConvertNode(kvp.Value);
            }
            return result;
        }

        private static object ConvertNode(object node)
        {
            if (node is NodeDict nd)
                return nd.ToPlainDictionary(); // Recursivo
            if (node is List<object> list)
            {
                var converted = new List<object>();
                foreach (var item in list)
                    converted.Add(ConvertNode(item));
                return converted;
            }
            // Añade otros casos si tienes Float32Array, byte[]...
            return node;
        }
    }

}