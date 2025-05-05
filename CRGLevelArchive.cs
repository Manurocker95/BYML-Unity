using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VirtualPhenix.Nintendo64;

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

    public override string ToString()
    {
        return $"LevelArchive(Name: {Name}, DataSize: {Data?.ByteLength ?? 0}, CodeSize: {Code?.ByteLength ?? 0}, PhotoSize: {Photo?.ByteLength ?? 0})";
    }

    public CRGLevelArchive()
    {

    }

    public CRGLevelArchive(VP_BYML.NodeDict dict)
    {
        FromNodeDict(dict);
    }

    public void FromNodeDict(VP_BYML.NodeDict dict)
    {
        if (dict == null) return;

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
}
