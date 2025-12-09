using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using XNode;

namespace SNEngine.Editor
{
    public class DialogueManagerWindow : EditorWindow
    {
        private const string DialoguesResourcePath = "Dialogues";
        private const string TargetFolderPath = "Assets/SNEngine/Source/SNEngine/Resources/Dialogues";
        private string newDialogueName = "NewDialogue";
        private string searchQuery = "";
        private Vector2 scrollPosition;

        [MenuItem("SNEngine/Dialogue Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<DialogueManagerWindow>("Dialogue Manager");
            window.minSize = new Vector2(400, 300);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(5);

            DrawHeader();
            DrawCreationSection();
            DrawListSection();

            EditorGUILayout.Space(10);
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Dialogue Management", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
        }

        private void DrawCreationSection()
        {
            GUI.backgroundColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.LabelField("Create New Dialogue", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name:", GUILayout.Width(45));
            newDialogueName = EditorGUILayout.TextField(newDialogueName);
            if (GUILayout.Button("Create Asset", GUILayout.Width(100), GUILayout.Height(20)))
            {
                CreateNewDialogue();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawListSection()
        {
            EditorGUILayout.LabelField("Existing Dialogues", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            searchQuery = EditorGUILayout.TextField(searchQuery, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(20)))
            {
                searchQuery = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            var allDialogueAssets = Resources.LoadAll<NodeGraph>(DialoguesResourcePath)
                .Where(d => d != null)
                .ToArray();

            var filteredAssets = string.IsNullOrEmpty(searchQuery)
                ? allDialogueAssets
                : allDialogueAssets.Where(d =>
                    d.name.IndexOf(searchQuery, System.StringComparison.OrdinalIgnoreCase) >= 0).ToArray();

            if (filteredAssets.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    string.IsNullOrEmpty(searchQuery)
                        ? "No dialogues found in Resources/Dialogues."
                        : "No dialogues match the search query.", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < filteredAssets.Length; i++)
            {
                DrawDialogueItem(filteredAssets[i]);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawDialogueItem(NodeGraph graph)
        {
            if (graph == null) return;

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            Texture2D icon = AssetPreview.GetMiniThumbnail(graph);
            GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));

            EditorGUILayout.LabelField(graph.name, EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Rename", EditorStyles.miniButton, GUILayout.Width(70), GUILayout.Height(20)))
            {
                Selection.activeObject = graph;
                EditorGUIUtility.PingObject(graph);
                Debug.Log($"[Dialogue Manager] Asset '{graph.name}' selected. Press F2 in the Project window to rename it.");
            }

            if (GUILayout.Button("Open", EditorStyles.miniButton, GUILayout.Width(60), GUILayout.Height(20)))
            {
                OpenGraphInEditor(graph);
            }

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Delete", EditorStyles.miniButton, GUILayout.Width(60), GUILayout.Height(20)))
            {
                if (EditorUtility.DisplayDialog("Confirm Deletion",
                    $"Are you sure you want to delete dialogue '{graph.name}'?", "Yes", "No"))
                {
                    DeleteDialogue(graph);
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private void CreateNewDialogue()
        {
            if (string.IsNullOrWhiteSpace(newDialogueName))
            {
                EditorUtility.DisplayDialog("Error", "Dialogue name cannot be empty.", "OK");
                return;
            }

            string uniqueAssetName = newDialogueName.EndsWith(".asset") ? newDialogueName : $"{newDialogueName}.asset";
            string existingPath = Path.Combine(TargetFolderPath, uniqueAssetName);

            if (File.Exists(existingPath))
            {
                EditorUtility.DisplayDialog("Error", $"Dialogue '{newDialogueName}' already exists.", "OK");
                return;
            }

            DialogueCreatorEditor.CreateNewDialogueAssetFromName(newDialogueName);

            Repaint();
        }

        private void DeleteDialogue(NodeGraph graph)
        {
            string assetPath = AssetDatabase.GetAssetPath(graph);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError($"Could not find asset path for: {graph.name}");
                return;
            }

            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.Refresh();

            Repaint();
        }

        private void OpenGraphInEditor(NodeGraph graph)
        {
            Selection.activeObject = graph;
            DialogueCreatorEditor.OpenGraph(graph);
        }
    }
}