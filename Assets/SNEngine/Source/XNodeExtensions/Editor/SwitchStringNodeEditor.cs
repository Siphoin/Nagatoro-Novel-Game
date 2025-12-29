using SiphoinUnityHelpers.XNodeExtensions.NodesControlExecutes.Switch;
using UnityEditor;

namespace SiphoinUnityHelpers.XNodeExtensions.Editor
{
    [CustomNodeEditor(typeof(SwitchStringNode))]
    public class SwitchStringNodeEditor : BaseSwitchNodeEditor<string>
    {
        protected override string GetPortNameFromProperty(SerializedProperty prop)
        {
            return "cases " + prop.propertyPath.Split('[', ']')[1];
        }
    }
}