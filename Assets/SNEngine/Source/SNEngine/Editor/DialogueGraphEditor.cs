using SNEngine.Graphs;
using UnityEngine;
using XNodeEditor;
using UnityEditor;
using SiphoinUnityHelpers.XNodeExtensions;

namespace SNEngine.Editor
{
    [CustomNodeGraphEditor(typeof(DialogueGraph))]
    public class DialogueGraphEditor : NodeGraphEditor
    {
        public override void OnGUI()
        {
            DrawToolbarButtons();
            base.OnGUI();
        }

        private void DrawToolbarButtons()
        {
            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Add Node", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                Vector2 graphPos = NodeEditorWindow.current.WindowToGridPosition(Event.current.mousePosition);

                NodeSelectorWindow.Open((nodeType, position) => {
                    CreateNode(nodeType, position);
                }, graphPos);
            }

            if (GUILayout.Button("Search Nodes", EditorStyles.toolbarButton, GUILayout.Width(120)))
            {
                NodeSearchWindow.Open(target as BaseGraph);
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);
            EditorGUILayout.EndHorizontal();
        }
    }
}