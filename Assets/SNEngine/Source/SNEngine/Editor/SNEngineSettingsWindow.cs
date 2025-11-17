using UnityEditor;
using UnityEngine;

namespace SNEngine.Editor
{
    public class SNEngineSettingsWindow : EditorWindow
    {
        [MenuItem("SNEngine/Settings")]
        public static void ShowWindow()
        {
            GetWindow<SNEngineSettingsWindow>("SNEngine Settings");
        }

        private void OnGUI()
        {
            GUILayout.Label("SNEngine Editor Preferences", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            bool showGuid = EditorGUILayout.Toggle("Show Node GUID", SNEngineEditorSettings.ShowNodeGuidInInspector);

            if (EditorGUI.EndChangeCheck())
            {
                SNEngineEditorSettings.ShowNodeGuidInInspector = showGuid;

                UnityEditor.SceneView.RepaintAll();
                EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
                foreach (EditorWindow window in windows)
                {
                    window.Repaint();
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("This setting controls whether the unique GUID is displayed in the Node Inspector for debugging purposes.", MessageType.Info);
        }
    }

}