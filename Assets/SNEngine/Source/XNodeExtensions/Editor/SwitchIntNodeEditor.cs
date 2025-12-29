using SiphoinUnityHelpers.XNodeExtensions.NodesControlExecutes.Switch;
using UnityEditor;

namespace SiphoinUnityHelpers.XNodeExtensions.Editor
{
    [CustomNodeEditor(typeof(SwitchIntNode))]
    public partial class SwitchIntNodeEditor : BaseSwitchNodeEditor<int>
    {
        protected override string GetPortNameFromProperty(SerializedProperty prop)
        {
            return "case " + prop.intValue.ToString();
        }
    }
}