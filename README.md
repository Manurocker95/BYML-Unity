# CRG1-BYML-Unity

* Parser for C# to manually parse CRG1 files from NoClip.Website that are formatted as custom BYML files. This works like the original BYML parser from Jasper but with DotNet functionality that can be used in Unity Engine.
* Tested on Unity 2022.3.61f1 with Pokémon Snap CRG1' files that obtain:

CRGLevelArchive binary data as struct with:

- Name (uint)
- Code (Byte Array Buffer)
- Photo (Byte Array Buffer)
- Data (Byte Array Buffer)
- CodeAddress (uint)
- PhotoAddress (uint)
- DataAddress (uint)
- ParticleData (Byte Array Buffer)
- Collision (uint)
- Header (uint)

* In order to call "Parse" function from VP_BYML static class, you need to send a buffer array allocated in a VP_ArraySliceBuffer. For example:
```
  byte[] cgr1Data = System.IO.File.ReadAllBytes("C:/10_arc.crg1");
  VP_ArrayBufferSlice arrayBufferSlice = new VP_ArrayBufferSlice(new VP_ArrayBuffer(cgr1Data));
  object parsedObject = VP_BYML.Parse(arrayBufferSlice, FileType.CRG1);
  VP_BYML.NodeDict parsedDictionary = (VP_BYML.NodeDict)parsedObject;

  // Parse here the dictionary to your stuff, for example:
 CRGLevelArchive LevelArchive = new CRGLevelArchive(parsedDictionary);

  // In levelArchive:

    public CRGLevelArchive(VP_BYML.NodeDict dict)
    {
        FromNodeDict(dict);
    }

  // Note that you need to know beforehand the keys you want to parse as Data is a generic "object"
    public void FromNodeDict(VP_BYML.NodeDict dict)
    {
        if (dict == null || dict.Count == 0) return;

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
```
