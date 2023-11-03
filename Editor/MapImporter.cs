using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Swizzler {

    [ScriptedImporter(3, "swizzlermap")]
    public class MapImporter : BaseImporter {

        public ChannelSource red = new();
        public ChannelSource green = new();
        public ChannelSource blue = new();
        public ChannelSource alpha = new();

        public override void OnImportAsset(AssetImportContext ctx) {
            var maskMap = Pack(ctx);
            maskMap.name = "MaskMap";
            ctx.AddObjectToAsset("MaskMap", maskMap);
        }

        Texture2D Pack(AssetImportContext ctx) {
            var packer = new TexturePacker();
            packer.Initialize();

            packer.Add(InputFor(ctx, red, TextureChannel.Red));
            packer.Add(InputFor(ctx, green, TextureChannel.Green));
            packer.Add(InputFor(ctx, blue, TextureChannel.Blue));
            packer.Add(InputFor(ctx, alpha, TextureChannel.Alpha));

            return packer.Create().CopyWithMipMaps();
        }

        // #if ODIN_INSPECTOR
        //         [Sirenix.OdinInspector.Button]
        // #endif
        //         void ExportAsPng() {
        //             var path = EditorUtility.SaveFilePanel("Export as PNG", "", "MaskMap", "png");
        //             if (string.IsNullOrEmpty(path)) return;
        //             var png = Pack().EncodeToPNG();
        //             File.WriteAllBytes(path, png);
        //             AssetDatabase.Refresh();
        //         }

        [MenuItem("Tools/Swizzler/Create Map")]
        static void CreateMap() {
            var projectWindowUtilType = typeof(ProjectWindowUtil);
            var getActiveFolderPath = projectWindowUtilType.GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
            var obj = getActiveFolderPath.Invoke(null, System.Array.Empty<object>());
            var pathToCurrentFolder = obj.ToString();
            System.IO.File.Create(pathToCurrentFolder + "/New Map.swizzlermap").Dispose();

            AssetDatabase.Refresh();
        }
    }
}
