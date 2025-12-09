using UnityEditor;
using UnityEngine;

namespace SNEngine.Editor
{
    public class SNEngineSettingsWindow : EditorWindow
    {
        private Vector2 _scrollPosition;

        [MenuItem("SNEngine/Settings")]
        public static void ShowWindow()
        {
            SNEngineSettingsWindow window = GetWindow<SNEngineSettingsWindow>("SNEngine Settings");
            window.minSize = new Vector2(350, 250);
        }

        public void OnGUI()
        {
            GUILayout.Label("SNEngine Global Editor Settings", new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 10, 10)
            });

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawNodeSettings();


            EditorGUILayout.EndScrollView();


            Rect footerRect = new Rect(0, position.height - 20, position.width, 20);
            EditorGUI.LabelField(footerRect, "Version: 1.0", EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawNodeSettings()
        {
            EditorGUILayout.Space(5);

            // Секция XNode Settings
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("XNode/Graph Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUI.BeginChangeCheck();

            bool showGuid = SNEngineEditorSettings.ShowNodeGuidInInspector;
            showGuid = EditorGUILayout.Toggle(new GUIContent("Show Node GUID", "Display the unique identifier (GUID) of the node in the Inspector."), showGuid);

            if (EditorGUI.EndChangeCheck())
            {
                SNEngineEditorSettings.ShowNodeGuidInInspector = showGuid;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }
    }
}