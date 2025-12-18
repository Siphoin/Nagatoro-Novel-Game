using SNEngine.Serialisation;
using UnityEngine;

namespace SiphoinUnityHelpers.XNodeExtensions.Varitables.Set
{
    public class SetMaterialNode : SetVaritableNode<Material> 
    {
        protected override void OnSetTargetValueChanged(VaritableNode<Material> targetNode, Material newValue)
        {
            SNEngineSerialization.AddAssetToLibrary<MaterialLibrary>(newValue);
        }
    }
}
