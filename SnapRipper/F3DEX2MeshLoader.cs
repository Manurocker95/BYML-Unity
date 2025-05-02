using System;
using System.Collections.Generic;
using UnityEngine;

public static class F3DEX2MeshLoader
{
    public static List<GameObject> LoadModelsFromData(byte[] data)
    {
        var meshes = new List<GameObject>();

        if (data == null || data.Length < 8)
        {
            Debug.LogWarning("⚠️ Datos insuficientes para procesar display list.");
            return meshes;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();

        int offset = 0;
        Vector3[] vtxCache = new Vector3[32];

        while (offset + 8 <= data.Length)
        {
            uint w0 = BitConverter.ToUInt32(data, offset);
            uint w1 = BitConverter.ToUInt32(data, offset + 4);

            byte cmd = (byte)(w0 >> 24);

            switch (cmd)
            {
                case 0x04: // G_VTX
                    {
                        int numvtx = ((int)((w0 >> 12) & 0xFF) / 0x10);
                        int dstIndex = (int)((w0 >> 1) & 0x1F);
                        uint addr = w1 & 0x00FFFFFF;

                        for (int i = 0; i < numvtx; i++)
                        {
                            int vtxOffset = (int)(addr + i * 0x10);
                            if (vtxOffset + 0x10 > data.Length)
                                continue;

                            short x = BitConverter.ToInt16(data, vtxOffset);
                            short y = BitConverter.ToInt16(data, vtxOffset + 2);
                            short z = BitConverter.ToInt16(data, vtxOffset + 4);
                            vtxCache[dstIndex + i] = new Vector3(x / 32.0f, y / 32.0f, z / 32.0f);
                        }
                        break;
                    }

                case 0xBF: // G_TRI1
                    {
                        int i0 = (int)((w1 >> 16) & 0xFF) / 2;
                        int i1 = (int)((w1 >> 8) & 0xFF) / 2;
                        int i2 = (int)((w1 >> 0) & 0xFF) / 2;

                        if (i0 < vtxCache.Length && i1 < vtxCache.Length && i2 < vtxCache.Length)
                        {
                            int baseIndex = vertices.Count;
                            vertices.Add(vtxCache[i0]);
                            vertices.Add(vtxCache[i1]);
                            vertices.Add(vtxCache[i2]);

                            indices.Add(baseIndex);
                            indices.Add(baseIndex + 1);
                            indices.Add(baseIndex + 2);
                        }
                        break;
                    }
            }
            offset += 8;
        }

        if (vertices.Count > 0)
        {
            Mesh mesh = new Mesh();
            mesh.name = "ParsedMesh";
            mesh.SetVertices(vertices);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();

            GameObject go = new GameObject("ParsedMeshGO");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();

            meshes.Add(go);
        }
        else
        {
            Debug.LogWarning("⚠️ No se generaron vértices ni triángulos.");
        }

        return meshes;
    }
}
