using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RootMotion;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Swizzler {

    public abstract class BaseImporter : ScriptedImporter {

        protected bool LoadDependent(AssetImportContext ctx, LazyLoadReference<Texture2D> dependency, out Texture2D tex) {
            if (dependency.isSet && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(dependency, out var guidString, out var _)) {
                var guid = new GUID(guidString);
                ctx.DependsOnArtifact(guid);
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));
                return true;
            }
            else {
                tex = null;
                return false;
            }
        }

        protected TextureInput InputFor(AssetImportContext ctx, ChannelSource source, TextureChannel outputChannel) {
            LoadDependent(ctx, source.texture, out var loadedTex);
            var input = new TextureInput {
                texture = loadedTex,
            };
            var channelInput = input.GetChannelInput(source.sourceChannel);
            channelInput.enabled = loadedTex != null;
            channelInput.output = outputChannel;
            channelInput.invert = source.invert;
            return input;
        }
    }

}
