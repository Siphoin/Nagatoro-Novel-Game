using SNEngine.Serialisation;
using UnityEngine;

namespace SiphoinUnityHelpers.XNodeExtensions.Varitables.Set
{
    [NodeTint("#4a5052")]
    public class SetPrefabNode : SetVaritableNode<GameObject>
    {
        protected override void OnSetTargetValueChanged(VaritableNode<GameObject> targetNode, GameObject newValue)
        {
            SNEngineSerialization.AddAssetToLibrary<PrefabLibrary>(newValue);
        }
    }
}
