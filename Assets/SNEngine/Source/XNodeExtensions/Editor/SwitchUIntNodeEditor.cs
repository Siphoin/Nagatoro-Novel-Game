using SiphoinUnityHelpers.XNodeExtensions.NodesControlExecutes.Switch;
using UnityEditor;

namespace SiphoinUnityHelpers.XNodeExtensions.Editor
{
    [CustomNodeEditor(typeof(SwitchUIntNode))]
    public class SwitchUIntNodeEditor : BaseSwitchNodeEditor<uint>
    {
        protected override string GetPortNameFromProperty(SerializedProperty prop)
        {
            return "cases " + prop.propertyPath.Split('[', ']')[1];
        }
    }
}