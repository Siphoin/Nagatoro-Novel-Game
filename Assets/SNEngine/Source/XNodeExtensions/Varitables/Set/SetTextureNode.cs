using SNEngine.Serialisation;
using UnityEngine;

namespace SiphoinUnityHelpers.XNodeExtensions.Variables.Set
{
    public class SetTextureNode : SetVariableNode<Texture>
    {
        protected override void OnSetTargetValueChanged(VariableNode<Texture> targetNode, Texture newValue)
        {
            SNEngineSerialization.AddAssetToLibrary<TextureLibrary>(newValue);
        }
    }
}
