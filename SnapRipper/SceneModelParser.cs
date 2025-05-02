using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using VirtualPhenix.PokemonSnapRipper;
using System.Linq;

namespace VirtualPhenix.PokemonSnapRipper
{
    public class SceneModelParser : MonoBehaviour
    {
      
        public static void ParseAndInstantiateSceneModels()
        {
            string path = EditorUtility.OpenFilePanel("Selecciona archivo .crg1", Application.dataPath, "crg1");
            if (string.IsNullOrEmpty(path)) return;

            byte[] data = File.ReadAllBytes(path);
            var buffer = new ArrayBufferSlice(data);
            var node = BymlParser.Parse<NodeDict>(buffer.Buffer, FileType.CRG1);
            var dict = node.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
            if (!dict.ContainsKey("Data"))
            {
                Debug.LogError("❌ No se encontró la sección 'Data' en el archivo .crg1");
                return;
            }
            Debug.Log("Data is of type: "+dict["Data"].GetType());

            var binaryNode = (NodeBinaryData)dict["Data"];
            byte[] modelData = binaryNode.Data;
            List<GameObject> generated = F3DEX2MeshLoader.LoadModelsFromData(modelData);

            GameObject root = new GameObject("ParsedScene");
            foreach (var go in generated)
            {
                go.transform.SetParent(root.transform);
            }

            Debug.Log($"✅ Instanciados {generated.Count} modelos desde '{Path.GetFileName(path)}'");
        }
    }

}