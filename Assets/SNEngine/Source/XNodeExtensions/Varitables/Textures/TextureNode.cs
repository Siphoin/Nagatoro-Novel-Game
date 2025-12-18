using SNEngine.Serialisation;
using UnityEngine;

namespace SiphoinUnityHelpers.XNodeExtensions.Varitables.Textures
{
    public class TextureNode : UnityVaritableNode<Texture, TextureLibrary>
    {
        protected override void OnValueChanged(Texture oldValue, Texture newValue)
        {
            SNEngineSerialization.AddAssetToLibrary<TextureLibrary>(newValue);
        }
    }
}
