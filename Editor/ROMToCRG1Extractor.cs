using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using VirtualPhenix.Nintendo64;
using System;


namespace VirtualPhenix.PokemonSnapRipper
{
    public static class ROMToCRG1Extractor
    {
        private const string OutputFolder = "Assets/GeneratedCRG1";

        public class OverlaySpec
        {
            public uint Rom { get; set; }
            public uint Ram { get; set; }
            public uint Length { get; set; }
        }


        private static readonly uint[] ParticleAddresses = new uint[]
        {
            0xAB5860,
            0xAB85E0,
            0xABE7A0,
            0xAC6890,
            0xAC8510,
            0xACF6F0,
            0xAD0E00,
            0xADD310,
            0xADEC60
        };

        private static byte[] FetchDataSync(string path)
        {
            return File.ReadAllBytes(path);
        }
        [MenuItem("Pokemon Snap/Extraer .crg1 desde .z64 ROM")]
        public static void ExtractCrg1FromROM()
        {
            string romPath = EditorUtility.OpenFilePanel(
               "Selecciona la ROM de Pokémon Snap",
               "",
               ""
           );
            if (string.IsNullOrEmpty(romPath)) return;
            if (!romPath.EndsWith(".z64") && !romPath.EndsWith(".n64") && !romPath.EndsWith(".v64"))
            {
                EditorUtility.DisplayDialog("Error", "Extensión de archivo no válida. Debe ser .z64, .n64 o .v64.", "OK");
                return;
            }
            byte[] romData = FetchDataSync(romPath);
            Directory.CreateDirectory(OutputFolder);

            // Extract maps
            ExtractMap(romData, 14, new OverlaySpec { Rom = 0x5959C, Ram = 0x800ADBEC, Length = 83 * 0x14 }); // common data
            ExtractMap(romData, 16, new OverlaySpec { Rom = 0x13C780, Ram = 0x801B0310, Length = 0x26530 }, 0x8011B914, 0x802CBEE4, 0x80318F00); // beach
            ExtractMap(romData, 18, new OverlaySpec { Rom = 0x1D1D90, Ram = 0x8018BC50, Length = 0x240E0 }, 0x8011E6CC, 0x802EDFAC, 0x80326EE0); // tunnel
            ExtractMap(romData, 24, new OverlaySpec { Rom = 0x3D0560, Ram = 0x801A9900, Length = 0x25E70 }, 0x800FFFB8, 0x802E0D44, 0x8031D4D0); // volcano
            ExtractMap(romData, 22, new OverlaySpec { Rom = 0x30AF90, Ram = 0x8019AEE0, Length = 0x1BC80 }, 0x8012AC90, 0x802E271C, 0x80321560); // river
            ExtractMap(romData, 20, new OverlaySpec { Rom = 0x27AB80, Ram = 0x801AEDF0, Length = 0x1F610 }, 0x8012A0E8, 0x802C6234, 0x80317610); // cave
            ExtractMap(romData, 26, new OverlaySpec { Rom = 0x47CF30, Ram = 0x80186B10, Length = 0x2B230 }, 0x80100720, 0x802D282C, 0x8031F9C0); // valley
            ExtractMap(romData, 28, new OverlaySpec { Rom = 0x4EC000, Ram = 0x80139C50, Length = 0x04610 }, 0x800F5DA0, 0x8034AB34); // rainbow cloud

            // Extract pokemon
            ExtractPokemon(romData, "magikarp",
                new OverlaySpec { Rom = 0x731B0, Ram = 0x800F5D90, Length = 0xA200 },
                new OverlaySpec { Rom = 0x54B5D0, Ram = 0x8034E130, Length = 0x20D0 },
                new OverlaySpec { Rom = 0x82F8E0, Ram = 0x803B1F80, Length = 0x3080 }
            );

            ExtractPokemon(romData, "pikachu",
                new OverlaySpec { Rom = 0x7D3B0, Ram = 0x800FFF90, Length = 0x1B0C0 },
                new OverlaySpec { Rom = 0x54D6A0, Ram = 0x803476A0, Length = 0x6A90 },
                new OverlaySpec { Rom = 0x832960, Ram = 0x803AD580, Length = 0x4A00 }
            );

            ExtractPokemon(romData, "bulbasaur",
                new OverlaySpec { Rom = 0x99F70, Ram = 0x8011CB50, Length = 0xD570 },
                new OverlaySpec { Rom = 0x557050, Ram = 0x8033F6C0, Length = 0x50C0 },
                new OverlaySpec { Rom = 0x83A1E0, Ram = 0x803A71B0, Length = 0x3550 }
            );

            ExtractPokemon(romData, "zubat",
                new OverlaySpec { Rom = 0x98470, Ram = 0x8011B050, Length = 0x1B00 },
                new OverlaySpec { Rom = 0x554130, Ram = 0x80344780, Length = 0x2F20 },
                new OverlaySpec { Rom = 0x837360, Ram = 0x803AA700, Length = 0x2E80 }
            );

            AssetDatabase.Refresh();
            Debug.Log("✅ Extracción completada. Archivos .crg1 guardados en: " + OutputFolder);
        }

        private static void ExtractMap(byte[] romData, int sceneId, OverlaySpec photo, uint header = 0, uint objectStart = 0, uint collisionStart = 0)
        {
            const int SCENE_TABLE_OFFSET = 0x57580;
            int offset = SCENE_TABLE_OFFSET + sceneId * 0x24;

            // Read with proper endianness handling
            uint romStart = BitConverter.ToUInt32(romData, offset);
            uint romEnd = BitConverter.ToUInt32(romData, offset + 4);
            uint startAddress = BitConverter.ToUInt32(romData, offset + 8);

            uint codeRomStart = BitConverter.ToUInt32(romData, offset + 0x24);
            uint codeRomEnd = BitConverter.ToUInt32(romData, offset + 0x28);
            uint codeStartAddress = BitConverter.ToUInt32(romData, offset + 0x2C);

            // Debug output before processing
            Debug.Log($"Raw addresses for scene {sceneId}:\n" +
                      $"Data: 0x{romStart:X8} to 0x{romEnd:X8}\n" +
                      $"Code: 0x{codeRomStart:X8} to 0x{codeRomEnd:X8}");

            // Get slices with endian correction
            byte[] dataSection = GetSlice(romData, romStart, romEnd);
            byte[] codeSection = GetSlice(romData, codeRomStart, codeRomEnd);
            byte[] photoSection = GetSlice(romData, photo.Rom, photo.Rom + photo.Length);

            var crg1 = new Dictionary<string, object>
            {
                { "Name", sceneId },
                { "Data", GetSlice(romData, romStart, romEnd) },
                { "Code", GetSlice(romData, codeRomStart, codeRomEnd) },
                { "StartAddress", startAddress },
                { "CodeStartAddress", codeStartAddress },
                { "Photo", GetSlice(romData, photo.Rom, photo.Rom + photo.Length) },
                { "PhotoStartAddress", photo.Ram },
                { "Header", header },
                { "Objects", objectStart },
                { "Collision", collisionStart },
                { "ParticleData", null }
            };

            VP_ArrayBuffer data = VP_BYML.Write(crg1, FileType.CRG1);
            File.WriteAllBytes($"{OutputFolder}/{sceneId.ToString("X2")}_arc.crg1", data.Buffer);
        }

        private static void ExtractPokemon(byte[] romData, string name, OverlaySpec data, OverlaySpec code, OverlaySpec photo)
        {
            var crg1 = new Dictionary<string, object>
            {
                { "Data", GetSlice(romData, data.Rom, data.Rom + data.Length) },
                { "StartAddress", data.Ram },
                { "Code", GetSlice(romData, code.Rom, code.Rom + code.Length) },
                { "CodeStartAddress", code.Ram },
                { "Photo", GetSlice(romData, photo.Rom, photo.Rom + photo.Length) },
                { "PhotoStartAddress", photo.Ram }
            };

            VP_ArrayBuffer pokeData = VP_BYML.Write(crg1, FileType.CRG1);
            
            File.WriteAllBytes($"{OutputFolder}/{name}_arc.crg1", pokeData.Buffer);
        }

        private static byte[] GetSlice(byte[] source, uint start, uint end)
        {
            if (start >= source.Length || end > source.Length || end <= start)
            {
                Debug.LogWarning($"⚠️ Rango inválido en GetSlice: start=0x{start:X}, end=0x{end:X}, length={source.Length}");
                return Array.Empty<byte>();
            }

            int length = (int)(end - start);
            byte[] slice = new byte[length];

            try
            {
                Buffer.BlockCopy(source, (int)start, slice, 0, length);
                return slice;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error al copiar bloque: start=0x{start:X}, end=0x{end:X}, ex: {ex.Message}");
                return Array.Empty<byte>();
            }
        }

        private static uint SwapEndian(uint value)
        {
            return (value & 0x000000FF) << 24 |
                   (value & 0x0000FF00) << 8 |
                   (value & 0x00FF0000) >> 8 |
                   (value & 0xFF000000) >> 24;
        }

        private static uint MaskRomAddress(uint address)
        {
            // For 16MB ROMs, mask to 24-bit address space
            return address & 0xFFFFFF;
        }
    }
}
