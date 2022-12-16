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

    [ScriptedImporter(1, "swizzlermap")]
    public class MapImporter : ScriptedImporter {

        public ChannelSource red = new();
        public ChannelSource green = new();
        public ChannelSource blue = new();
        public ChannelSource alpha = new();

        public override void OnImportAsset(AssetImportContext ctx) {
            var packer = new TexturePacker();
            packer.Initialize();

            packer.Add(InputFor(red, TextureChannel.Red));
            packer.Add(InputFor(green, TextureChannel.Green));
            packer.Add(InputFor(blue, TextureChannel.Blue));
            packer.Add(InputFor(alpha, TextureChannel.Alpha));

            var maskMap = packer.Create().CopyWithMipMaps();
            maskMap.name = "MaskMap";
            ctx.AddObjectToAsset("MaskMap", maskMap);

        }

        TextureInput InputFor(ChannelSource source, TextureChannel outputChannel) {
            var input = new TextureInput {
                texture = source.texture,
            };
            var channelInput = input.GetChannelInput(source.sourceChannel);
            channelInput.enabled = source.texture != null;
            channelInput.output = outputChannel;
            channelInput.invert = source.invert;
            return input;
        }

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
