using UnityEditor;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public static class YamlEditorLauncher
{
    private const string ExePathRelative = "Assets/SNEngine/Source/SNEngine/Editor/Utils/YAMLEditor.exe";

    [MenuItem("SNEngine/Yaml Editor")]
    public static void LaunchYamlEditor()
    {
        string projectPath = Application.dataPath;
        string editorFolder = Directory.GetParent(projectPath).FullName;

        string fullExePath = Path.Combine(editorFolder, ExePathRelative).Replace('/', Path.DirectorySeparatorChar);

        if (!File.Exists(fullExePath))
        {
            UnityEngine.Debug.LogError($"[Yaml Editor] Error: EXE not found at: {fullExePath}");
            EditorUtility.DisplayDialog("Launch Error",
                                        $"File '{ExePathRelative}' not found. Check path.",
                                        "OK");
            return;
        }

        try
        {
            Process.Start(fullExePath);
            UnityEngine.Debug.Log($"[Yaml Editor] Launching: {fullExePath}");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[Yaml Editor] Launch error: {e.Message}");
            EditorUtility.DisplayDialog("Launch Error",
                                        $"Error launching: {e.Message}",
                                        "OK");
        }
    }
}