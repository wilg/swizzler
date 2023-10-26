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

    [ScriptedImporter(1, "swizzlerpbr")]
    public class PBRImporter : BaseImporter {

        public LazyLoadReference<Texture2D> albedo;
        public LazyLoadReference<Texture2D> normal;

        public Texture2D maskMap;
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

            LoadDependent(ctx, albedo, out var albedoTex);
            LoadDependent(ctx, normal, out var normalTex);

            var packer = new TexturePacker();
            packer.Initialize();

            // terrain mask map

            packer.Add(InputFor(ctx, metallic, TextureChannel.Red));
            packer.Add(InputFor(ctx, ambientOcclusion, TextureChannel.Green));
            packer.Add(InputFor(ctx, height, TextureChannel.Blue));
            packer.Add(InputFor(ctx, smoothness, TextureChannel.Alpha));

            var usedMaskMap = maskMap;

            if (usedMaskMap == null) {
                usedMaskMap = packer.Create().CopyWithMipMaps();
                usedMaskMap.name = "MaskMap";
                ctx.AddObjectToAsset("MaskMap", usedMaskMap);
            }

            // HDRP Material
            var material = new Material(Shader.Find("HDRP/Lit")) {
                name = "HDRPMaterial",
            };
            material.SetTexture("_BaseColorMap", albedoTex);
            material.SetTexture("_NormalMap", normalTex);
            material.SetTexture("_MaskMap", usedMaskMap);
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
                name = "TerrainLayer",
                diffuseTexture = albedoTex,
                normalMapTexture = normalTex,
                maskMapTexture = usedMaskMap,
                tileSize = tileSize,
                tileOffset = tileOffset,
            };
            ctx.AddObjectToAsset("TerrainLayer", terrainLayer);

            ctx.SetMainObject(material);

        }

        bool LoadDependent(AssetImportContext ctx, LazyLoadReference<Texture2D> dependency, out Texture2D tex) {
            if (dependency.isSet && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(dependency, out var guid, out var _)) {
                ctx.DependsOnArtifact(guid);
                tex = dependency.asset;
                return true;
            }
            else {
                tex = null;
                return false;
            }
        }

        TextureInput InputFor(AssetImportContext ctx, ChannelSource source, TextureChannel outputChannel) {
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
                outPath = prefix + "Combined.swizzlerpbr";
            }
            else {
                outPath = pathToCurrentFolder + "/Combined.swizzlerpbr";
            }

            var filenames = paths.Select(Path.GetFileNameWithoutExtension).ToArray();

            // guids for the textures we want to import
            Texture2D likelyAlbedo = null;
            Texture2D likelyNormal = null;
            Texture2D likelyMaskMap = null;
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
                    Debug.Log("Found albedo: " + filename);
                }
                if (likelyNormal == null && normalKeywords.Any(k => filename.Contains(k, StringComparison.OrdinalIgnoreCase))) {
                    likelyNormal = Extensions.LoadAssetAtGUID<Texture2D>(guid);
                }
                if (likelyMaskMap == null && maskMapKeywords.Any(k => filename.Contains(k, StringComparison.OrdinalIgnoreCase))) {
                    likelyMaskMap = Extensions.LoadAssetAtGUID<Texture2D>(guid);
                }
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
            importer.maskMap = likelyMaskMap;
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
