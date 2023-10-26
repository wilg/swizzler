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
        public LazyLoadReference<Texture2D> texture;

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.ShowIf("texture"), Sirenix.OdinInspector.EnumToggleButtons]
#endif
        public TextureChannel sourceChannel;

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.ShowIf("texture")]
#endif
        public bool invert = false;

    }
}
