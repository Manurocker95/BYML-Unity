using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VirtualPhenix.Nintendo64;

[System.Serializable]
public class CRGLevelArchive 
{
    public uint Name { get; set; }

    public VP_ArrayBufferSlice Data { get; set; }
    public VP_ArrayBufferSlice Code { get; set; }
    public VP_ArrayBufferSlice Photo { get; set; }
    public VP_ArrayBufferSlice ParticleData { get; set; }

    public uint StartAddress { get; set; }
    public uint CodeStartAddress { get; set; }
    public uint PhotoStartAddress { get; set; }

    public uint Header { get; set; }
    public uint Objects { get; set; }
    public uint Collision { get; set; }

    public CRGLevelArchive()
    {

    }

    public CRGLevelArchive(VP_BYML.NodeDict dict)
    {
        FromNodeDict(dict);
    }

    public void FromNodeDict(VP_BYML.NodeDict dict)
    {
        if (dict == null || dict.Count == 0) return;

        foreach (var kvp in dict)
        {
            Debug.Log(kvp.Key);
        }

        Name = (uint)dict["Name"].Data;
        Data = (VP_ArrayBufferSlice)dict["Data"].Data;
        Code = (VP_ArrayBufferSlice)dict["Code"].Data;
        Photo = (VP_ArrayBufferSlice)dict["Photo"].Data;
        ParticleData = (VP_ArrayBufferSlice)dict["ParticleData"].Data;
        StartAddress = (uint)dict["StartAddress"].Data;
        CodeStartAddress = (uint)dict["CodeStartAddress"].Data;
        PhotoStartAddress = (uint)dict["PhotoStartAddress"].Data;
        Header = (uint)dict["Header"].Data;
        Objects = (uint)dict["Objects"].Data;
        Collision = (uint)dict["Collision"].Data;
    }

    public void Log()
    {
        Debug.Log("========== LEVEL ARCHIVE ==========");
        Debug.Log($"Name: 0x{this.Name:X8} ({this.Name})");
        Debug.Log($"StartAddress: 0x{this.StartAddress:X8} ({this.StartAddress})");
        Debug.Log($"CodeStartAddress: 0x{this.CodeStartAddress:X8} ({this.CodeStartAddress})");
        Debug.Log($"PhotoStartAddress: 0x{this.PhotoStartAddress:X8} ({this.PhotoStartAddress})");
        Debug.Log($"Header: 0x{this.Header:X8} ({this.Header})");
        Debug.Log($"Objects: 0x{this.Objects:X8} ({this.Objects})");
        Debug.Log($"Collision: 0x{this.Collision:X8} ({this.Collision})");
        Debug.Log($"Data: {this.Data}");
        Debug.Log($"Code: {this.Code}");
        Debug.Log($"Photo: {this.Photo}");
        Debug.Log($"ParticleData: {this.ParticleData}");
        Debug.Log("===================================");
    }

}
