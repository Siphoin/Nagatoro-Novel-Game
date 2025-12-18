#if UNITY_EDITOR
using XNodeEditor;
using SNEngine.GlobalVaritables;

namespace SiphoinUnityHelpers.XNodeExtensions.Editor
{
    [CustomNodeEditor(typeof(GetVaritableValueNode<>))]
    public class GetGlobalVaritableNodeEditor : NodeEditor
    {
        public override void OnBodyGUI()
        {
            XNodeEditorHelpers.DrawGetGlobalVaritableBody(this, serializedObject);
        }
    }

    // Явные редакторы для конкретных типов Get-нод
    [CustomNodeEditor(typeof(GetGlobalIntNode))] public class GetGlobalIntNodeEditor : GetGlobalVaritableNodeEditor { }
    /*Z
    [CustomNodeEditor(typeof(GetGlobalUintNode))] public class GetGlobalUintNodeEditor : GetGlobalVaritableNodeEditor { }
    [CustomNodeEditor(typeof(GetGlobalStringNode))] public class GetGlobalStringNodeEditor : GetGlobalVaritableNodeEditor { }
    [CustomNodeEditor(typeof(GetGlobalFloatNode))] public class GetGlobalFloatNodeEditor : GetGlobalVaritableNodeEditor { }
    [CustomNodeEditor(typeof(GetGlobalDoubleNode))] public class GetGlobalDoubleNodeEditor : GetGlobalVaritableNodeEditor { }
    [CustomNodeEditor(typeof(GetGlobalBoolNode))] public class GetGlobalBoolNodeEditor : GetGlobalVaritableNodeEditor { }
    [CustomNodeEditor(typeof(GetGlobalColorNode))] public class GetGlobalColorNodeEditor : GetGlobalVaritableNodeEditor { }
    [CustomNodeEditor(typeof(GetGlobalVector2Node))] public class GetGlobalVector2NodeEditor : GetGlobalVaritableNodeEditor { }
    [CustomNodeEditor(typeof(GetGlobalQuaternionNode))] public class GetGlobalQuaternionNodeEditor : GetGlobalVaritableNodeEditor { }
    */
}
#endif