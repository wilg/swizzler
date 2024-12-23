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
                var path = AssetDatabase.GUIDToAssetPath(guidString);
                ctx.DependsOnSourceAsset(path);
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                return true;
            }
            tex = null;
            return false;
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

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button]
#endif
        public void BakeOut() {
            // save main asset and all sub assets of this asset into a subfolder
            var path = AssetDatabase.GetAssetPath(this);
            var dir = Path.GetDirectoryName(path);
            var name = Path.GetFileNameWithoutExtension(path);
            var outPath = dir + "/" + name + "_baked";
            if (System.IO.Directory.Exists(outPath)) {
                System.IO.Directory.Delete(outPath, true);
            }
            System.IO.Directory.CreateDirectory(outPath);

            var copyMap = new Dictionary<UnityEngine.Object, UnityEngine.Object>();

            foreach (var subAsset in AssetDatabase.LoadAllAssetsAtPath(path)) {
                var copy = Instantiate(subAsset);
                copy.name = $"{subAsset.name} (Baked)";
                if (copy is TerrainLayer) {
                    copy.name = $"{name} (Baked)";
                }
                copyMap[subAsset] = copy;
                // save the sub asset into the sub folder
                var extension = copy is Material ? ".mat" : ".bin.asset";
                AssetDatabase.CreateAsset(copy, outPath + "/" + copy.name + extension);
            }

            // ensure references to textures in all materials and terrain layers point to the copy
            foreach (var copy in copyMap.Values) {
                if (copy is Material material) {
                    foreach (var textureProperty in material.GetTexturePropertyNames()) {
                        var texture = material.GetTexture(textureProperty);
                        if (texture != null && copyMap.TryGetValue(texture, out var copyTexture)) {
                            material.SetTexture(textureProperty, (Texture)copyTexture);
                        }
                    }
                }
                else if (copy is TerrainLayer terrainLayer) {
                    if (copyMap.TryGetValue(terrainLayer.diffuseTexture, out var copyDiffuseTexture)) {
                        terrainLayer.diffuseTexture = (Texture2D)copyDiffuseTexture;
                    }
                    if (copyMap.TryGetValue(terrainLayer.normalMapTexture, out var copyNormalMapTexture)) {
                        terrainLayer.normalMapTexture = (Texture2D)copyNormalMapTexture;
                    }
                    if (copyMap.TryGetValue(terrainLayer.maskMapTexture, out var copyMaskMapTexture)) {
                        terrainLayer.maskMapTexture = (Texture2D)copyMaskMapTexture;
                    }
                }
            }

            AssetDatabase.Refresh();
        }
    }
}
