using SiphoinUnityHelpers.XNodeExtensions.NodesControlExecutes.Switch;
using UnityEditor;

namespace SiphoinUnityHelpers.XNodeExtensions.Editor
{
    [CustomNodeEditor(typeof(SwitchLongNode))]
    public class SwitchLongNodeEditor : BaseSwitchNodeEditor<long>
    {
        protected override string GetPortNameFromProperty(SerializedProperty prop)
        {
            return "cases " + prop.propertyPath.Split('[', ']')[1];
        }
    }
}