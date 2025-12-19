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
        private GUIStyle _textAreaBoxStyle;

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

            GUILayout.Space(5);
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
                _wrappedTextStyle.normal.background = null;
                _wrappedTextStyle.focused.background = null;
            }

            SerializedProperty textProp = serializedObject.FindProperty("_text");
            if (textProp == null) return;

            EditorGUILayout.LabelField("Text Content", EditorStyles.boldLabel);

            // ФИКС: Рассчитываем примерную высоту на основе длины строки
            // 150 - примерная ширина ноды, 15 - высота одной строки
            float charWidth = 7f;
            float nodeWidth = 200f;
            int charsPerLine = Mathf.Max(1, Mathf.FloorToInt(nodeWidth / charWidth));
            int lineCount = Mathf.Max(3, (textProp.stringValue.Length / charsPerLine) + 1);
            float calculatedHeight = lineCount * 18f; // 18 пикселей на строку

            GUILayout.BeginVertical(EditorStyles.helpBox);

            // Принудительно задаем высоту через MinHeight
            textProp.stringValue = EditorGUILayout.TextArea(
                textProp.stringValue,
                _wrappedTextStyle,
                GUILayout.MinHeight(calculatedHeight)
            );

            GUILayout.EndVertical();
        }

        public override Color GetTint() => new Color(0.2f, 0.2f, 0.3f);
    }
}
#endif