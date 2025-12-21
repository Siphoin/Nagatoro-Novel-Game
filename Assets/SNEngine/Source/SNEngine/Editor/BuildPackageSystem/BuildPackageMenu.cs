using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace SNEngine.Editor.BuildPackageSystem
{
    public static class BuildPackageMenu
    {
        private const string MENU_PATH = "SNEngine/Build Packages";
        private const string CUSTOM_RESOURCES_PATH = "Assets/SNEngine/Source/SNEngine/Resources/Custom";
        private const string CHARACTERS_PATH = "Assets/SNEngine/Source/SNEngine/Resources/Characters";
        private const string DEMO_PATH = "Assets/SNEngine/Demo";
        private const string STREAMING_ASSETS_PATH = "Assets/StreamingAssets";
        private const string DIALOGUES_PATH = "Assets/SNEngine/Source/SNEngine/Resources/Dialogues";
        private const string TEMPLATE_PATH = "Assets/SNEngine/Source/SNEngine/Editor/Templates/DialogueTemplate.asset";
        private const string START_DIALOGUE_NAME = "_startDialogue.asset";

        [MenuItem(MENU_PATH)]
        public static void BuildPackage()
        {
            if (IsOnMasterBranch())
            {
                EditorUtility.DisplayDialog("Build Package",
                    "You are currently on the master branch. Please switch to a different branch before building the package.",
                    "OK");
                return;
            }

            string exportPath = EditorUtility.OpenFolderPanel("Select folder to save unitypackage", "", "");
            if (string.IsNullOrEmpty(exportPath))
            {
                Debug.Log("Package build cancelled by user.");
                return;
            }

            string packagePath = Path.Combine(exportPath, "SNEngine.unitypackage");

            try
            {
                string gitState = GetGitState();

                AssetDatabase.SaveAssets();

                DeleteAssetSafe(CUSTOM_RESOURCES_PATH);
                DeleteAssetSafe(STREAMING_ASSETS_PATH);
                DeleteAssetSafe(DEMO_PATH);

                ClearFolder(CHARACTERS_PATH);
                ClearFolder(DIALOGUES_PATH);

                CreateStartDialogue();

                AssetDatabase.Refresh();

                ExportWorker.ExportPackage(packagePath);

                Debug.Log($"Package successfully exported to: {packagePath}");

                RestoreGitState(gitState);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during package build: {e.Message}");
            }
        }

        private static string GetProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static bool IsOnMasterBranch()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-Command \"git branch --show-current\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = GetProjectRoot()
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return output.Trim().Equals("master", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
            }
        }

        private static void DeleteAssetSafe(string path)
        {
            if (AssetDatabase.IsValidFolder(path) || File.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        private static void ClearFolder(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] subFolders = AssetDatabase.GetSubFolders(folderPath);
            foreach (string folder in subFolders)
            {
                AssetDatabase.DeleteAsset(folder);
            }

            string[] files = Directory.GetFiles(Path.GetFullPath(Path.Combine(Application.dataPath, "..", folderPath)));
            foreach (string file in files)
            {
                string assetPath = folderPath + "/" + Path.GetFileName(file);
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
            }
        }

        private static void CreateStartDialogue()
        {
            string startDialoguePath = Path.Combine(DIALOGUES_PATH, START_DIALOGUE_NAME);

            if (!File.Exists(TEMPLATE_PATH))
            {
                Debug.LogError($"[BuildPackage] Template not found: {TEMPLATE_PATH}");
                return;
            }

            File.Copy(TEMPLATE_PATH, startDialoguePath, true);
            AssetDatabase.ImportAsset(startDialoguePath, ImportAssetOptions.ForceUpdate);
        }

        private static string GetGitState()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-Command \"git status --porcelain\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = GetProjectRoot()
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return output.Trim();
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void RestoreGitState(string previousState)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-Command \"git checkout .; git clean -fd\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = GetProjectRoot()
                };

                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                }

                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not restore git state: {e.Message}");
            }
        }
    }
}