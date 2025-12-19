#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using XNodeEditor;
using SNEngine.DialogOnScreenSystem;

namespace SNEngine.Editor
{
    [CustomNodeEditor(typeof(DialogOnScreenNode))]
    public class DialogOnScreenNodeEditor : NodeEditor
    {
        private GUIStyle _wrappedTextStyle;

        public override void OnBodyGUI()
        {
            serializedObject.Update();

            DrawPort("_enter");

            foreach (var tag in NodeEditorGUILayout.GetFilteredFields(serializedObject))
            {
                if (tag.name == "_text" || tag.name == "m_Script") continue;
                NodeEditorGUILayout.PropertyField(serializedObject.FindProperty(tag.name));
            }

            DrawDynamicTextArea();

            GUILayout.Space(10);
            DrawPort("_exit");

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPort(string fieldName)
        {
            XNode.NodePort port = target.GetPort(fieldName);
            if (port != null) NodeEditorGUILayout.PortField(port);
        }

        private void DrawDynamicTextArea()
        {
            if (_wrappedTextStyle == null)
            {
                _wrappedTextStyle = new GUIStyle(EditorStyles.textArea);
                _wrappedTextStyle.wordWrap = true;
                _wrappedTextStyle.fontSize = 12;
                _wrappedTextStyle.padding = new RectOffset(8, 8, 8, 8);
            }

            SerializedProperty textProp = serializedObject.FindProperty("_text");
            if (textProp == null) return;

            EditorGUILayout.LabelField("Text Content", EditorStyles.boldLabel);

            float nodeWidth = 200;
            if (NodeEditorWindow.current != null)
            {
                nodeWidth = NodeEditorWindow.current.nodeSizes.ContainsKey(target)
                    ? NodeEditorWindow.current.nodeSizes[target].x
                    : 200;
            }

            float availableWidth = nodeWidth - 30;
            float height = _wrappedTextStyle.CalcHeight(new GUIContent(textProp.stringValue), availableWidth);

            float extraPadding = 20f;
            float finalHeight = Mathf.Max(60f, height + extraPadding);

            GUILayout.BeginVertical(EditorStyles.helpBox);
            textProp.stringValue = EditorGUILayout.TextArea(
                textProp.stringValue,
                _wrappedTextStyle,
                GUILayout.Height(finalHeight)
            );
            GUILayout.EndVertical();
        }

        public override Color GetTint() => new Color(0.2f, 0.2f, 0.3f);
    }
}
#endif