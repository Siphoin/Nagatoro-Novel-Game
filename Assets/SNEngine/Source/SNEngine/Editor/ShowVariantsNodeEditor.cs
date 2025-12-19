using UnityEditor;
using UnityEngine;
using XNodeEditor;
using SNEngine.SelectVariantsSystem;

namespace SNEngine.Editor
{
    [CustomNodeEditor(typeof(ShowVariantsNode))]
    public class ShowVariantsNodeEditor : NodeEditor
    {
        private GUIStyle _variantBoxStyle;
        private GUIStyle _textInputStyle;

        public override void OnBodyGUI()
        {
            serializedObject.Update();

            // 1. Вход сверху
            DrawPort("_enter");

            // 2. Параметры
            DrawCompactSettings();

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Choice Variants", EditorStyles.boldLabel);

            // 3. Список вариантов
            DrawDynamicVariants();

            // 4. Результат и выход
            DrawOutputResult();
            DrawPort("_exit");

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPort(string fieldName)
        {
            XNode.NodePort port = target.GetPort(fieldName);
            if (port != null) NodeEditorGUILayout.PortField(port);
        }

        private void DrawCompactSettings()
        {
            SerializedProperty animType = serializedObject.FindProperty("_typeAnimation");
            EditorGUILayout.PropertyField(animType);

            GUILayout.Space(5);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            DrawBoolToggle("_hideCharacters", "Hide Characters");
            DrawBoolToggle("_hideDialogWindow", "Hide Dialog Window");
            DrawBoolToggle("_returnCharacterVisible", "Return Char Visible");
            GUILayout.EndVertical();
        }

        private void DrawBoolToggle(string propertyName, string label)
        {
            SerializedProperty prop = serializedObject.FindProperty(propertyName);
            if (prop == null) return;

            EditorGUILayout.BeginHorizontal();
            prop.boolValue = EditorGUILayout.ToggleLeft(label, prop.boolValue, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDynamicVariants()
        {
            SerializedProperty variantsProp = serializedObject.FindProperty("_variants");

            if (_variantBoxStyle == null)
            {
                _variantBoxStyle = new GUIStyle(EditorStyles.helpBox);
                _variantBoxStyle.padding = new RectOffset(5, 5, 5, 5);

                _textInputStyle = new GUIStyle(EditorStyles.textArea);
                _textInputStyle.wordWrap = true;
                _textInputStyle.richText = false;
                // Убираем стандартную рамку TextArea, чтобы выглядело чище
                _textInputStyle.normal.background = null;
                _textInputStyle.focused.background = null;
            }

            for (int i = 0; i < variantsProp.arraySize; i++)
            {
                SerializedProperty element = variantsProp.GetArrayElementAtIndex(i);

                GUILayout.BeginVertical(_variantBoxStyle);

                EditorGUILayout.BeginHorizontal();
                GUI.color = Color.gray;
                EditorGUILayout.LabelField($"#{i + 1}", EditorStyles.miniLabel, GUILayout.Width(20));
                GUI.color = Color.white;

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(18), GUILayout.Height(14)))
                {
                    variantsProp.DeleteArrayElementAtIndex(i);
                    serializedObject.ApplyModifiedProperties();
                    return;
                }
                EditorGUILayout.EndHorizontal();

                // ВАЖНО: Просто рисуем TextArea без указания высоты. 
                // GUILayout сам расширит helpBox под текст.
                element.stringValue = EditorGUILayout.TextArea(
                    element.stringValue,
                    _textInputStyle,
                    GUILayout.ExpandHeight(false)
                );

                GUILayout.EndVertical();
                GUILayout.Space(4);
            }

            if (GUILayout.Button("+ Add New Choice", GUILayout.Height(22)))
            {
                variantsProp.arraySize++;
            }
        }

        private void DrawOutputResult()
        {
            GUILayout.Space(8);
            XNode.NodePort resultPort = target.GetOutputPort("_selectedIndex");
            if (resultPort != null)
            {
                NodeEditorGUILayout.PortField(new GUIContent("Result Selection"), resultPort);
            }
        }

        public override Color GetTint()
        {
            return new Color(0.15f, 0.18f, 0.22f);
        }
    }
}