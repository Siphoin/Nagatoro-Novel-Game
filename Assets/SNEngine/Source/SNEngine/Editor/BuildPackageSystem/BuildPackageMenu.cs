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
            // Check if we're on the master branch using PowerShell
            if (IsOnMasterBranch())
            {
                EditorUtility.DisplayDialog("Build Package", 
                    "You are currently on the master branch. Please switch to a different branch before building the package.", 
                    "OK");
                return;
            }

            // Ask user to select folder for the unitypackage
            string exportPath = EditorUtility.OpenFolderPanel("Select folder to save unitypackage", "", "");
            if (string.IsNullOrEmpty(exportPath))
            {
                Debug.Log("Package build cancelled by user.");
                return;
            }

            string packagePath = Path.Combine(exportPath, "SNEngine.unitypackage");

            try
            {
                // Store current git state
                string gitState = GetGitState();

                // Delete Custom folder and all characters
                DeleteCustomResources();
                DeleteAllCharacters();
                DeleteStreamingAssetsFolder();
                
                // Create start dialogue
                CreateStartDialogue();
                
                // Delete Demo folder
                DeleteDemoFolder();

                // Export the package
                ExportPackage(packagePath);

                Debug.Log($"Package successfully exported to: {packagePath}");
                
                // Restore git state to revert all changes
                RestoreGitState(gitState);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during package build: {e.Message}");
            }
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
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    string currentBranch = output.Trim();
                    return currentBranch.Equals("master", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not check git branch: {e.Message}");
                return false; // Assume not on master if we can't check
            }
        }

        private static void DeleteCustomResources()
        {
            if (Directory.Exists(CUSTOM_RESOURCES_PATH))
            {
                Directory.Delete(CUSTOM_RESOURCES_PATH, true);
                AssetDatabase.DeleteAsset("Assets/SNEngine/Source/SNEngine/Resources/Custom");
                
                Debug.Log($"[BuildPackage] Custom resources deleted from: {CUSTOM_RESOURCES_PATH}");
            }
        }

        private static void DeleteAllCharacters()
        {
            if (Directory.Exists(CHARACTERS_PATH))
            {
                // Delete all character assets in the folder
                string[] characterFiles = Directory.GetFiles(CHARACTERS_PATH, "*.asset");
                
                foreach (string characterFile in characterFiles)
                {
                    string relativePath = "Assets" + characterFile.Substring(Application.dataPath.Length);
                    AssetDatabase.DeleteAsset(relativePath);
                }
                
                Debug.Log($"[BuildPackage] All characters deleted from: {CHARACTERS_PATH}");
            }
        }

        private static void DeleteDemoFolder()
        {
            if (Directory.Exists(DEMO_PATH))
            {
                Directory.Delete(DEMO_PATH, true);
                AssetDatabase.DeleteAsset(DEMO_PATH);
                
                Debug.Log($"[BuildPackage] Demo folder deleted from: {DEMO_PATH}");
            }
        }
        
        private static void DeleteStreamingAssetsFolder()
        {
            if (Directory.Exists(STREAMING_ASSETS_PATH))
            {
                Directory.Delete(STREAMING_ASSETS_PATH, true);
                AssetDatabase.DeleteAsset(STREAMING_ASSETS_PATH);
                
                Debug.Log($"[BuildPackage] StreamingAssets folder deleted from: {STREAMING_ASSETS_PATH}");
            }
        }

        private static void CreateStartDialogue()
        {
            if (!Directory.Exists(DIALOGUES_PATH))
            {
                Directory.CreateDirectory(DIALOGUES_PATH);
            }

            string startDialoguePath = Path.Combine(DIALOGUES_PATH, START_DIALOGUE_NAME);

            // Check if template exists
            if (!File.Exists(TEMPLATE_PATH))
            {
                Debug.LogError($"[BuildPackage] Template not found at path: {TEMPLATE_PATH}");
                return;
            }

            // Copy template to create start dialogue
            File.Copy(TEMPLATE_PATH, startDialoguePath, true);
            
            // Import the new asset so Unity recognizes it
            AssetDatabase.ImportAsset(startDialoguePath, ImportAssetOptions.ForceUpdate);
            
            Debug.Log($"[BuildPackage] Created start dialogue at: {startDialoguePath}");
        }
        
        private static void ExportPackage(string packagePath)
        {
            // Define the assets to be included in the package
            string[] assets = {
                "Assets/SNEngine",
            };

            // Ensure the directory exists
            string directory = Path.GetDirectoryName(packagePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Export the package
            AssetDatabase.ExportPackage(assets, packagePath, 
                ExportPackageOptions.Recurse | ExportPackageOptions.Interactive);
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
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    return output.Trim();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not get git state: {e.Message}");
                return string.Empty;
            }
        }
        
        private static void RestoreGitState(string previousState)
        {
            try
            {
                // Use git checkout to restore all changes
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-Command \"git checkout . && git clean -fd\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    Debug.Log($"[BuildPackage] Git state restored. Output: {output}");
                }
                
                // Refresh the asset database
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not restore git state: {e.Message}");
            }
        }
    }
}