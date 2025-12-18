using SNEngine.Serialisation;
using UnityEngine;
using XNode;

namespace SiphoinUnityHelpers.XNodeExtensions.Varitables
{
    [NodeTint("#524c49")]
    public class PrefabNode : UnityVaritableNode<GameObject, PrefabLibrary>
    {
        protected override void OnValueChanged(GameObject oldValue, GameObject newValue)
        {
            SNEngineSerialization.AddAssetToLibrary<PrefabLibrary>(newValue);
        }
    }
}
