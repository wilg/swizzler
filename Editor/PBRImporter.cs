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

    [ScriptedImporter(7, "swizzlerpbr")]
    public class PBRImporter : BaseImporter {

        public Texture2D albedo;
        public Texture2D normal;

        public ChannelSource metallic = new();
        public ChannelSource ambientOcclusion = new();
        public ChannelSource height = new();
        public ChannelSource smoothness = new();

        public Vector2 tileSize = Vector2.one;
        public Vector2 tileOffset = Vector2.zero;

        public bool triplanar = false;
        // [ShowIf("triplanar")]
        public float triplanarScale = 1f;

        public override void OnImportAsset(AssetImportContext ctx) {
            var fileName = Path.GetFileNameWithoutExtension(ctx.assetPath);

            LoadDependent(ctx, albedo, out var albedoTex);
            LoadDependent(ctx, normal, out var normalTex);
            LoadDependent(ctx, height.texture, out var heightTex);

            var terrainMaskMap = HDRPTerrainMaskMap(ctx, metallic, ambientOcclusion, height, smoothness);
            terrainMaskMap.name = $"{fileName} HDRP Terrain Mask Map";
            ctx.AddObjectToAsset("HDRPTerrainMaskMap", terrainMaskMap);

            var litMaskMap = HDRPMaskMap(ctx, metallic, ambientOcclusion, smoothness);
            litMaskMap.name = $"{fileName} HDRP Lit Mask Map";
            ctx.AddObjectToAsset("HDRPLitMaskMap", litMaskMap);


            // HDRP Material
            var material = new Material(Shader.Find("HDRP/Lit")) {
                name = $"{fileName} HDRP Lit Material",
            };
            material.SetTexture("_BaseColorMap", albedoTex);
            material.SetTexture("_NormalMap", normalTex);
            material.SetTexture("_MaskMap", litMaskMap);
            material.SetTexture("_HeightMap", heightTex);
            // if you want displacement you need to create a material variant
            if (triplanar) {
                material.SetFloat("_UVBase", 5);
                material.SetColor("_UVMappingMask", new Color(0, 0, 0, 0));
                material.EnableKeyword("_MAPPING_TRIPLANAR");
                material.SetFloat("_TexWorldScale", triplanarScale);
                material.SetFloat("_InvTilingScale", 1f / triplanarScale);
            }
            ctx.AddObjectToAsset("HDRPMaterial", material);

            // Terrain Layer
            var terrainLayer = new TerrainLayer {
                name = $"{fileName} Terrain Layer",
                diffuseTexture = albedoTex,
                normalMapTexture = normalTex,
                maskMapTexture = terrainMaskMap,
                tileSize = tileSize,
                tileOffset = tileOffset,
            };
            ctx.AddObjectToAsset("TerrainLayer", terrainLayer);

            ctx.SetMainObject(material);

        }

        Texture2D HDRPTerrainMaskMap(AssetImportContext ctx, ChannelSource metallic, ChannelSource ao, ChannelSource height, ChannelSource smoothness) {
            return PackRGBA(ctx, metallic, ao, height, smoothness);
        }

        Texture2D HDRPMaskMap(AssetImportContext ctx, ChannelSource metallic, ChannelSource ao, ChannelSource smoothness) {
            return PackRGBA(ctx, metallic, ao, null, smoothness);
        }

        Texture2D PackRGBA(AssetImportContext ctx, ChannelSource r, ChannelSource g, ChannelSource b, ChannelSource a) {
            var packer = new TexturePacker();
            packer.Initialize();
            var maxResolution = -1;
            if (r != null) {
                packer.Add(InputFor(ctx, r, TextureChannel.Red));
                if (r.texture != null) {
                    maxResolution = Mathf.Max(maxResolution, r.texture.width);
                }
            }
            if (g != null) {
                packer.Add(InputFor(ctx, g, TextureChannel.Green));
                if (g.texture != null) {
                    maxResolution = Mathf.Max(maxResolution, g.texture.width);
                }
            }
            if (b != null) {
                packer.Add(InputFor(ctx, b, TextureChannel.Blue));
                if (b.texture != null) {
                    maxResolution = Mathf.Max(maxResolution, b.texture.width);
                }
            }
            if (a != null) {
                packer.Add(InputFor(ctx, a, TextureChannel.Alpha));
                if (a.texture != null) {
                    maxResolution = Mathf.Max(maxResolution, a.texture.width);
                }
            }
            if (maxResolution != -1) {
                packer.resolution = maxResolution;
            }
            return packer.Create().CopyWithMipMaps();
        }

        [MenuItem("Tools/Swizzler/Create PBR Material")]
        static void CreatePBR() {

            var textAsset = new TextAsset("");

            var projectWindowUtilType = typeof(ProjectWindowUtil);
            var getActiveFolderPath = projectWindowUtilType.GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
            var obj = getActiveFolderPath.Invoke(null, System.Array.Empty<object>());
            var pathToCurrentFolder = obj.ToString();
            Debug.Log(pathToCurrentFolder);

            // all textures in the current folder
            var guids = AssetDatabase.FindAssets("t:texture2D", new[] { pathToCurrentFolder });

            var paths = guids.Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var prefix = paths.ToArray().PrefixInCommon();
            string outPath;
            if (prefix != "" && prefix != null) {
                outPath = prefix + "PBR.swizzlerpbr";
            }
            else {
                outPath = pathToCurrentFolder + "/PBR.swizzlerpbr";
            }

            var filenames = paths.Select(Path.GetFileNameWithoutExtension).ToArray();

            // guids for the textures we want to import
            Texture2D likelyAlbedo = null;
            Texture2D likelyNormal = null;
            // Texture2D likelyMaskMap = null;
            Texture2D likelyMetallic = null;
            Texture2D likelyAO = null;
            Texture2D likelyHeight = null;
            Texture2D likelySmoothness = null;
            Texture2D likelyRoughness = null;

            string[] albedoKeywords = { "albedo", "diffuse", "color", "base" };
            string[] normalKeywords = { "normal", "bump", "nrm" };
            string[] maskMapKeywords = { "mask" };
            string[] metallicKeywords = { "metal" };
            string[] aoKeywords = { "ao", "ambient", "occlusion" };
            string[] heightKeywords = { "height", "displacement" };
            string[] smoothnessKeywords = { "smooth", "gloss" };
            string[] roughnessKeywords = { "rough" };


            // try to guess which textures are which

            for (var i = 0; i < filenames.Length; i++) {
                var filename = filenames[i];
                var guid = guids[i];

                if (likelyAlbedo == null && albedoKeywords.Any(k => filename.Contains(k, StringComparison.OrdinalIgnoreCase))) {
                    likelyAlbedo = Extensions.LoadAssetAtGUID<Texture2D>(guid);
                }
                if (likelyNormal == null && normalKeywords.Any(k => filename.Contains(k, StringComparison.OrdinalIgnoreCase))) {
                    likelyNormal = Extensions.LoadAssetAtGUID<Texture2D>(guid);
                }
                // if (likelyMaskMap == null && maskMapKeywords.Any(k => filename.Contains(k, StringComparison.OrdinalIgnoreCase))) {
                //     likelyMaskMap = Extensions.LoadAssetAtGUID<Texture2D>(guid);
                // }
                if (likelyMetallic == null && metallicKeywords.Any(k => filename.Contains(k, StringComparison.OrdinalIgnoreCase))) {
                    likelyMetallic = Extensions.LoadAssetAtGUID<Texture2D>(guid);
                }
                if (likelyAO == null && aoKeywords.Any(k => filename.Contains(k, StringComparison.OrdinalIgnoreCase))) {
                    likelyAO = Extensions.LoadAssetAtGUID<Texture2D>(guid);
                }
                if (likelyHeight == null && heightKeywords.Any(k => filename.Contains(k, StringComparison.OrdinalIgnoreCase))) {
                    likelyHeight = Extensions.LoadAssetAtGUID<Texture2D>(guid);
                }
                if (likelySmoothness == null && smoothnessKeywords.Any(k => filename.Contains(k, StringComparison.OrdinalIgnoreCase))) {
                    likelySmoothness = Extensions.LoadAssetAtGUID<Texture2D>(guid);
                }
                if (likelyRoughness == null && roughnessKeywords.Any(k => filename.Contains(k, StringComparison.OrdinalIgnoreCase))) {
                    likelyRoughness = Extensions.LoadAssetAtGUID<Texture2D>(guid);
                }
            }

            System.IO.File.Create(outPath).Dispose();

            AssetDatabase.Refresh();

            var importer = AssetImporter.GetAtPath(outPath) as PBRImporter;
            importer.albedo = likelyAlbedo;
            importer.normal = likelyNormal;
            importer.metallic.texture = likelyMetallic;
            importer.ambientOcclusion.texture = likelyAO;
            importer.height.texture = likelyHeight;
            importer.smoothness.texture = likelySmoothness;
            if (likelyRoughness != null) {
                importer.smoothness.texture = likelyRoughness;
                importer.smoothness.invert = true;
            }

            EditorUtility.SetDirty(importer);

            importer.SaveAndReimport();
        }
    }
}
