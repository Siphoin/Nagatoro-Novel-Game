using SiphoinUnityHelpers.XNodeExtensions.NodesControlExecutes.Switch;
using UnityEditor;

namespace SiphoinUnityHelpers.XNodeExtensions.Editor
{
    [CustomNodeEditor(typeof(SwitchULongNode))]
    public class SwitchULongNodeEditor : BaseSwitchNodeEditor<ulong>
    {
        protected override string GetPortNameFromProperty(SerializedProperty prop)
        {
            return "cases " + prop.propertyPath.Split('[', ']')[1];
        }
    }
}