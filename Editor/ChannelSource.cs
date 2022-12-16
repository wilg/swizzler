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
    [System.Serializable]
    public class ChannelSource {
        public Texture2D texture;
        public TextureChannel sourceChannel;
        public bool invert = false;
    }
}
