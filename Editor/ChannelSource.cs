using UnityEngine;

namespace Swizzler {
    [System.Serializable]
    public class ChannelSource {
        public Texture2D texture;

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
