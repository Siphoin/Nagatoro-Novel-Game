using UnityEditor;
using UnityEngine;
using XNode;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using SiphoinUnityHelpers.XNodeExtensions;

public static class DialogueCreatorEditor
{
    private const string TemplatePath = "Assets/SNEngine/Source/SNEngine/Editor/Templates/DialogueTemplate.asset";
    private const string TargetFolderPath = "Assets/SNEngine/Source/SNEngine/Resources/Dialogues";
    private const string MenuItemPath = "SNEngine/New Dialogue";
    private const string RegenerateMethodName = "ResetGUID";
    private const string NodeEditorWindowTypeName = "XNodeEditor.NodeEditorWindow";

    [MenuItem(MenuItemPath)]
    public static void CreateNewDialogueAsset()
    {
        string fullTemplatePath = Path.Combine(Application.dataPath, TemplatePath.Replace("Assets/", ""));

        if (!File.Exists(fullTemplatePath))
        {
            Debug.LogError($"[DialogueCreator] Template not found at path: {TemplatePath} (Checked: {fullTemplatePath})");
            return;
        }

        if (!Directory.Exists(TargetFolderPath))
        {
            Directory.CreateDirectory(TargetFolderPath);
        }

        string newAssetName = GetUniqueDialogueAssetName();
        string newAssetPath = Path.Combine(TargetFolderPath, newAssetName);

        if (AssetDatabase.CopyAsset(TemplatePath, newAssetPath))
        {
            AssetDatabase.Refresh();

            NodeGraph dialogueGraph = AssetDatabase.LoadAssetAtPath<NodeGraph>(newAssetPath);

            if (dialogueGraph != null)
            {
                RegenerateAllNodeGuids(dialogueGraph);

                EditorUtility.SetDirty(dialogueGraph);
                AssetDatabase.SaveAssets();

                OpenGraphInEditor(dialogueGraph);

                Selection.activeObject = dialogueGraph;
            }
            else
            {
                Debug.LogError($"[DialogueCreator] Failed to load copied asset: {newAssetPath}");
            }
        }
        else
        {
            Debug.LogError($"[DialogueCreator] Failed to copy asset from {TemplatePath} to {newAssetPath}");
        }
    }

    private static void OpenGraphInEditor(NodeGraph graph)
    {
        var nodeEditorWindowType = typeof(EditorWindow).Assembly.GetType(NodeEditorWindowTypeName);

        if (nodeEditorWindowType == null)
        {
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            nodeEditorWindowType = assemblies
                .Select(a => a.GetType(NodeEditorWindowTypeName))
                .FirstOrDefault(t => t != null);
        }

        if (nodeEditorWindowType == null)
        {
            Debug.LogError($"[DialogueCreator] Failed to find type {NodeEditorWindowTypeName}. Ensure XNodeEditor is imported.");
            return;
        }

        var openMethod = nodeEditorWindowType.GetMethod("Open", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        if (openMethod == null)
        {
            openMethod = nodeEditorWindowType.GetMethod("Init", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        }

        if (openMethod != null)
        {
            openMethod.Invoke(null, new object[] { graph });
        }
        else
        {
            Debug.LogError($"[DialogueCreator] Neither 'Open' nor 'Init' method found in {NodeEditorWindowTypeName} to open the graph.");
        }
    }

    private static string GetUniqueDialogueAssetName()
    {
        string targetPath = Path.Combine(Application.dataPath, TargetFolderPath.Replace("Assets/", ""));

        if (!Directory.Exists(targetPath))
        {
            return "_startDialogue.asset";
        }

        string[] existingFiles = Directory.GetFiles(targetPath, "*.asset");

        if (!existingFiles.Any(f => Path.GetFileName(f).Equals("_startDialogue.asset", System.StringComparison.OrdinalIgnoreCase)))
        {
            return "_startDialogue.asset";
        }


        int maxNumber = 0;
        var regex = new Regex(@"dialogue_(\d+)\.asset");

        foreach (string file in existingFiles)
        {
            string fileName = Path.GetFileName(file);
            Match match = regex.Match(fileName);

            if (match.Success)
            {
                if (int.TryParse(match.Groups[1].Value, out int number))
                {
                    if (number > maxNumber)
                    {
                        maxNumber = number;
                    }
                }
            }
        }

        return $"dialogue_{maxNumber + 1}.asset";
    }

    private static void RegenerateAllNodeGuids(NodeGraph graph)
    {
        foreach (Node node in graph.nodes)
        {
            if (node is BaseNode baseNode)
            {
                var regenerateMethod = typeof(BaseNode).GetMethod(RegenerateMethodName,
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (regenerateMethod != null)
                {
                    regenerateMethod.Invoke(baseNode, null);
                    EditorUtility.SetDirty(baseNode);
                }
            }
        }
    }
}