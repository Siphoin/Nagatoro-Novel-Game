using SNEngine.Serialisation;
using UnityEngine;

namespace SiphoinUnityHelpers.XNodeExtensions.Varitables.Set
{
    public class SetTexture2DNode : SetVaritableNode<Texture2D>
    {
        protected override void OnSetTargetValueChanged(VaritableNode<Texture2D> targetNode, Texture2D newValue)
        {
            SNEngineSerialization.AddAssetToLibrary<Texture2DLibrary>(newValue);
        }
    }
}
