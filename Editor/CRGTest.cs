using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VirtualPhenix.Nintendo64;

public class CRGTest : EditorWindow
{ 
    [MenuItem("Pokemon Snap/Test CRG1")]
    public static void TestCRG1()
    {
        var crg1 = Application.dataPath + "/10_arc.crg1";

        var arrayBufferSlice = new VP_ArrayBufferSlice(new VP_ArrayBuffer(System.IO.File.ReadAllBytes(crg1)));
        var levelArchiveO = VP_BYML.Parse(arrayBufferSlice, FileType.CRG1);
        VP_BYML.NodeDict dict = (VP_BYML.NodeDict)levelArchiveO;


        Debug.Log("Level Archive Name: " + (uint)dict["Name"].Data);
    }
}
