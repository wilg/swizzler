using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Swizzler {
    public static class Extensions {

        public static Texture2D CopyWithMipMaps(this Texture2D texture) {
            var textureDestination = new Texture2D(texture.width, texture.height, texture.format, mipChain: true);
            textureDestination.SetPixels32(texture.GetPixels32());
            textureDestination.Apply(updateMipmaps: true);
            return textureDestination;
        }

        public static string PrefixInCommon(this string[] strings) {
            var prefix = "";
            var i = 0;
            while (true) {
                var c = strings[0][i];
                if (strings.All(s => s.Length > i && s[i] == c)) {
                    prefix += c;
                    i++;
                }
                else {
                    break;
                }
            }
            return prefix;
        }
#if UNITY_EDITOR
        public static T LoadAssetAtGUID<T>(string guid) where T : UnityEngine.Object {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        }
#endif
    }
}
