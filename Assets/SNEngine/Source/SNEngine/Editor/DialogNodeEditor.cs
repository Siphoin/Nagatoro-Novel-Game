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
                if (tag.name == "_character" || tag.name == "_text" || tag.name == "m_Script") continue;
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

            SerializedProperty textProp = serializedObject.FindProperty("_text");
            if (textProp == null) return;

            EditorGUILayout.LabelField("Dialogue Text", EditorStyles.boldLabel);

            float nodeWidth = 200;
            if (NodeEditorWindow.current != null && NodeEditorWindow.current.nodeSizes.ContainsKey(target))
            {
                nodeWidth = NodeEditorWindow.current.nodeSizes[target].x;
            }

            float availableWidth = nodeWidth - 30;
            float contentHeight = _wrappedTextStyle.CalcHeight(new GUIContent(textProp.stringValue), availableWidth);
            float finalHeight = Mathf.Max(60f, contentHeight + 15f);

            GUILayout.BeginVertical(EditorStyles.helpBox);

            Event e = Event.current;
            if (e.isKey && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
            {
                if (GUI.GetNameOfFocusedControl() == "DialogueField")
                {
                    e.Use();
                }
            }

            GUI.SetNextControlName("DialogueField");
            string rawText = EditorGUILayout.TextArea(
                textProp.stringValue,
                _wrappedTextStyle,
                GUILayout.Height(finalHeight)
            );

            textProp.stringValue = rawText.Replace("\n", "").Replace("\r", "");

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