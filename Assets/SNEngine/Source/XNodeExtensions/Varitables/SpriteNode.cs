using SNEngine.Serialisation;
using UnityEngine;

namespace SiphoinUnityHelpers.XNodeExtensions.Varitables
{
    [NodeTint("#464778")]
    public class SpriteNode : VaritableNode<Sprite>
    {
        protected override void OnValueChanged(Sprite oldValue, Sprite newValue)
        {
            SNEngineSerialization.AddAssetToLibrary<SpriteLibrary>(newValue);
        }
    }
}
