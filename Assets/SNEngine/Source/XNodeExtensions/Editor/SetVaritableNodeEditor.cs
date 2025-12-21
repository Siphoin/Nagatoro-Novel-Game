#if UNITY_EDITOR
using XNodeEditor;
using SiphoinUnityHelpers.XNodeExtensions.Variables.Set;
using SiphoinUnityHelpers.XNodeExtensions.Variables;

namespace SiphoinUnityHelpers.XNodeExtensions.Editor
{
    [CustomNodeEditor(typeof(SetVariableNode<>))]
    public class SetVaritableNodeEditor : NodeEditor
    {
        public override void OnBodyGUI()
        {
            XNodeEditorHelpers.DrawSetVaritableBody(this, serializedObject);
        }
    }

    // Базовые типы
    [CustomNodeEditor(typeof(SetIntNode))] public class SetIntNodeEditor : SetVaritableNodeEditor { }
    [CustomNodeEditor(typeof(SetUintNode))] public class SetUintNodeEditor : SetVaritableNodeEditor { }
    [CustomNodeEditor(typeof(SetStringNode))] public class SetStringNodeEditor : SetVaritableNodeEditor { }
    [CustomNodeEditor(typeof(SetFloatNode))] public class SetFloatNodeEditor : SetVaritableNodeEditor { }
    [CustomNodeEditor(typeof(SetDoubleNode))] public class SetDoubleNodeEditor : SetVaritableNodeEditor { }
    [CustomNodeEditor(typeof(SetBoolNode))] public class SetBoolNodeEditor : SetVaritableNodeEditor { }
    [CustomNodeEditor(typeof(SetColorNode))] public class SetColorNodeEditor : SetVaritableNodeEditor { }
    [CustomNodeEditor(typeof(SetVector2Node))] public class SetVector2NodeEditor : SetVaritableNodeEditor { }
    [CustomNodeEditor(typeof(SetQuaternionNode))] public class SetQuaternionNodeEditor : SetVaritableNodeEditor { }

    // Unity-типы
    [CustomNodeEditor(typeof(SetTransformNode))] public class SetTransformNodeEditor : SetVaritableNodeEditor { }
    [CustomNodeEditor(typeof(SetUlongNode))] public class SetUlongNodeEditor : SetVaritableNodeEditor { }


    // Коллекции
    [CustomNodeEditor(typeof(IntNode))] public class SetIntCollectionNodeEditor : SetVaritableNodeEditor { }
    [CustomNodeEditor(typeof(UintNode))] public class SetUintCollectionNodeEditor : SetVaritableNodeEditor { }
    [CustomNodeEditor(typeof(StringNode))] public class SetStringCollectionNodeEditor : SetVaritableNodeEditor { }
    [CustomNodeEditor(typeof(FloatNode))] public class SetFloatCollectionNodeEditor : SetVaritableNodeEditor { }
    [CustomNodeEditor(typeof(DoubleNode))] public class SetDoubleCollectionNodeEditor : SetVaritableNodeEditor { }
    [CustomNodeEditor(typeof(BoolNode))] public class SetBoolCollectionNodeEditor : SetVaritableNodeEditor { }
    [CustomNodeEditor(typeof(ColorNode))] public class SetColorCollectionNodeEditor : SetVaritableNodeEditor { }
    [CustomNodeEditor(typeof(Vector2Node))] public class SetVector2CollectionNodeEditor : SetVaritableNodeEditor { }
    [CustomNodeEditor(typeof(QuaternionNode))] public class SetQuaternionCollectionNodeEditor : SetVaritableNodeEditor { }
}
#endif