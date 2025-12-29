using SiphoinUnityHelpers.XNodeExtensions.NodesControlExecutes.Switch;
using UnityEditor;

namespace SiphoinUnityHelpers.XNodeExtensions.Editor
{
    public partial class SwitchIntNodeEditor
    {
        [CustomNodeEditor(typeof(SwitchFloatNode))]
        public class SwitchFloatNodeEditor : BaseSwitchNodeEditor<float>
        {
            protected override string GetPortNameFromProperty(SerializedProperty prop)
            {
                return "cases " + prop.propertyPath.Split('[', ']')[1];
            }
        }
    }
}