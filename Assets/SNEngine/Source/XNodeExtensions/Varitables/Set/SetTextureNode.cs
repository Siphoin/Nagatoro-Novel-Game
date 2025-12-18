using SNEngine.Serialisation;
using UnityEngine;

namespace SiphoinUnityHelpers.XNodeExtensions.Varitables.Set
{
    public class SetTextureNode : SetVaritableNode<Texture>
    {
        protected override void OnSetTargetValueChanged(VaritableNode<Texture> targetNode, Texture newValue)
        {
            SNEngineSerialization.AddAssetToLibrary<TextureLibrary>(newValue);
        }
    }
}
