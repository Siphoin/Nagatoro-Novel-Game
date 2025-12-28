using SiphoinUnityHelpers.XNodeExtensions.Editor;
using SiphoinUnityHelpers.XNodeExtensions.NodesControlExecutes.Switch;
using UnityEngine;

namespace SiphoinUnityHelpers.XNodeExtensions.NodesControlExecutes.Editors
{
    [CustomNodeEditor(typeof(SwitchIntegerNode))]
    public class SwitchIntNodeEditor : SwitchNodeEditorBase<int> { }

    [CustomNodeEditor(typeof(SwitchLongNode))]
    public class SwitchLongNodeEditor : SwitchNodeEditorBase<long> { }

    [CustomNodeEditor(typeof(SwitchUintNode))]
    public class SwitchUIntNodeEditor : SwitchNodeEditorBase<uint> { }

    [CustomNodeEditor(typeof(SwitchUlongNode))]
    public class SwitchULongNodeEditor : SwitchNodeEditorBase<ulong> { }

    [CustomNodeEditor(typeof(SwitchFloatNode))]
    public class SwitchFloatNodeEditor : SwitchNodeEditorBase<float> { }

    [CustomNodeEditor(typeof(SwitchDoubleNode))]
    public class SwitchDoubleNodeEditor : SwitchNodeEditorBase<double> { }

    [CustomNodeEditor(typeof(SwitchStringNode))]
    public class SwitchStringNodeEditor : SwitchNodeEditorBase<string> { }
}