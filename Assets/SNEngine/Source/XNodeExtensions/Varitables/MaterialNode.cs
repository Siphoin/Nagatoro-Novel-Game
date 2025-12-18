using SNEngine.Serialisation;
using UnityEngine;

namespace SiphoinUnityHelpers.XNodeExtensions.Varitables
{
    public class MaterialNode : UnityVaritableNode<Material, MaterialLibrary>
    {
        protected override void OnValueChanged(Material oldValue, Material newValue)
        {
            SNEngineSerialization.AddAssetToLibrary<MaterialLibrary>(newValue);
        }
    }
}
