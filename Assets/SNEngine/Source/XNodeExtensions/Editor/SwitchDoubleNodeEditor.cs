using SiphoinUnityHelpers.XNodeExtensions.NodesControlExecutes.Switch;
using UnityEditor;

namespace SiphoinUnityHelpers.XNodeExtensions.Editor
{
    [CustomNodeEditor(typeof(SwitchDoubleNode))]
    public class SwitchDoubleNodeEditor : BaseSwitchNodeEditor<double>
    {
        protected override string GetPortNameFromProperty(SerializedProperty prop)
        {
            return "cases " + prop.propertyPath.Split('[', ']')[1];
        }
    }
}