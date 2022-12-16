using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Swizzler {
    public class TextureInput {
        public Texture2D texture;

        private Dictionary<TextureChannel, TextureChannelInput> _inputs = new Dictionary<TextureChannel, TextureChannelInput>();

        public TextureInput() {
            _inputs[TextureChannel.Red] = new TextureChannelInput();
            _inputs[TextureChannel.Green] = new TextureChannelInput();
            _inputs[TextureChannel.Blue] = new TextureChannelInput();
            _inputs[TextureChannel.Alpha] = new TextureChannelInput();
        }

        public TextureChannelInput GetChannelInput(TextureChannel channel) {
            return _inputs[channel];
        }

        public void SetChannelInput(TextureChannel channel, TextureChannelInput channelInput) {
            _inputs[channel] = channelInput;
        }
    }
}
