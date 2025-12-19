#if UNITY_EDITOR
using SiphoinUnityHelpers.XNodeExtensions.Editor;
using SNEngine.DialogSystem;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace SNEngine.Editor
{
    [CustomNodeEditor(typeof(DialogNode))]
    public class DialogNodeEditor : NodeEditor
    {
        private GUIStyle _wrappedTextStyle;
        private GUIStyle _textAreaBoxStyle;

        public override void OnBodyGUI()
        {
            serializedObject.Update();
            DialogNode node = target as DialogNode;

            foreach (var tag in NodeEditorGUILayout.GetFilteredFields(serializedObject))
            {
                if (tag.name == "_character" || tag.name == "_text") continue;
                NodeEditorGUILayout.PropertyField(serializedObject.FindProperty(tag.name));
            }

            GUILayout.Space(5);

            DrawDynamicTextArea();

            GUILayout.Space(10);

            DrawCharacterSelector(node);

            serializedObject.ApplyModifiedProperties();
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

            if (_textAreaBoxStyle == null)
            {
                _textAreaBoxStyle = new GUIStyle(EditorStyles.helpBox);
            }

            SerializedProperty textProp = serializedObject.FindProperty("_text");
            if (textProp == null) return;

            EditorGUILayout.LabelField("Dialogue Text", EditorStyles.boldLabel);

            float charWidth = 7f;
            float nodeWidth = 200f;
            int charsPerLine = Mathf.Max(1, Mathf.FloorToInt(nodeWidth / charWidth));
            int lineCount = Mathf.Max(3, (textProp.stringValue.Length / charsPerLine) + 1);
            float calculatedHeight = lineCount * 18f;

            GUILayout.BeginVertical(_textAreaBoxStyle);

            textProp.stringValue = EditorGUILayout.TextArea(
                textProp.stringValue,
                _wrappedTextStyle,
                GUILayout.MinHeight(calculatedHeight)
            );

            GUILayout.EndVertical();
        }

        private void DrawCharacterSelector(DialogNode node)
        {
            string charName = node.Character != null ? node.Character.name : "Select Character";
            GUIContent content = new GUIContent(charName);

            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = node.Character != null ? new Color(0.4f, 0.75f, 0.45f) : new Color(0.75f, 0.4f, 0.4f);

            if (GUILayout.Button(content, GUILayout.Height(32)))
            {
                CharacterSelectorWindow.Open((selected) => {
                    var so = new SerializedObject(target);
                    var prop = so.FindProperty("_character");
                    if (prop != null)
                    {
                        prop.objectReferenceValue = selected;
                        so.ApplyModifiedProperties();
                    }
                });
            }
            GUI.backgroundColor = prevBg;
        }
    }
}
#endif