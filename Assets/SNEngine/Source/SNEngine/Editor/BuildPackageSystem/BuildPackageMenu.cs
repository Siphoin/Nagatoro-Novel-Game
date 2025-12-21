using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using SNEngine.Editor;

namespace SNEngine.Editor.BuildPackageSystem
{
    public static class BuildPackageMenu
    {
        private const string MENU_1 = "SNEngine/Package/1. Run Python Cleanup";
        private const string MENU_2 = "SNEngine/Package/2. Create Blank Dialogue";
        private const string MENU_3 = "SNEngine/Package/3. Create Blank Character";
        private const string MENU_4 = "SNEngine/Package/4. Build Package";
        private const string MENU_5 = "SNEngine/Package/5. Restore Project (Git)";

        private const string PYTHON_SCRIPT_REL_PATH = "Assets/SNEngine/Source/SNEngine/Editor/Python/build_cleanup.py";
        private const string CHAR_SAVE_PATH = "Assets/SNEngine/Source/SNEngine/Resources/Characters";
        private const string BLANK_CHAR_NAME = "Blank Character";

        [MenuItem(MENU_1)]
        public static async void Step1_Cleanup()
        {
            if (!ValidateBranch()) return;
            if (!EditorUtility.DisplayDialog("Step 1", "Clean project?", "Yes", "Cancel")) return;

            try
            {
                AssetDatabase.StartAssetEditing();
                await RunPythonCleanup();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
                Debug.Log("<color=cyan>[Package]</color> Cleanup complete.");
            }
            catch (Exception e)
            {
                AssetDatabase.StopAssetEditing();
                Debug.LogError(e.Message);
            }
        }

        [MenuItem(MENU_2)]
        public static void Step2_CreateDialogue()
        {
            if (!ValidateBranch()) return;
            DialogueCreatorEditor.CreateNewDialogueAssetFromName("_startDialogue.asset");
            AssetDatabase.Refresh();
        }

        [MenuItem(MENU_3)]
        public static void Step3_CreateCharacter()
        {
            if (!ValidateBranch()) return;
            if (!Directory.Exists(CHAR_SAVE_PATH)) Directory.CreateDirectory(CHAR_SAVE_PATH);

            string assetPath = Path.Combine(CHAR_SAVE_PATH, $"{BLANK_CHAR_NAME}.asset");
            var character = ScriptableObject.CreateInstance<SNEngine.CharacterSystem.Character>();
            character.Editor_SetName(BLANK_CHAR_NAME);

            AssetDatabase.CreateAsset(character, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem(MENU_4)]
        public static void Step4_Build()
        {
            if (!ValidateBranch()) return;
            string exportPath = EditorUtility.OpenFolderPanel("Save Package", "", "");
            if (string.IsNullOrEmpty(exportPath)) return;

            string packagePath = Path.Combine(exportPath, "SNEngine.unitypackage");

            string[] assets = { "Assets/SNEngine", "Assets/WebGLTemplates" };
            AssetDatabase.ExportPackage(assets, packagePath, ExportPackageOptions.Recurse);
            Debug.Log("<color=green>[Package]</color> Exported.");
        }

        [MenuItem(MENU_5)]
        public static void Step5_Restore()
        {
            if (!ValidateBranch()) return;
            RestoreGitState();
        }

        private static bool ValidateBranch()
        {
            if (IsOnMasterBranch())
            {
                EditorUtility.DisplayDialog("Blocked", "Master branch blocked.", "OK");
                return false;
            }
            return true;
        }

        private static async Task RunPythonCleanup()
        {
            string scriptPath = Path.Combine(GetProjectRoot(), PYTHON_SCRIPT_REL_PATH);
            ProcessStartInfo si = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{scriptPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WorkingDirectory = GetProjectRoot()
            };
            using (Process p = Process.Start(si))
            {
                await p.StandardOutput.ReadToEndAsync();
                p.WaitForExit();
            }
        }

        private static string GetProjectRoot() => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static bool IsOnMasterBranch()
        {
            ProcessStartInfo si = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-Command \"git branch --show-current\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WorkingDirectory = GetProjectRoot()
            };
            using (Process p = Process.Start(si))
            {
                string branch = p.StandardOutput.ReadToEnd().Trim();
                return branch.Equals("master", StringComparison.OrdinalIgnoreCase) || branch.Equals("main", StringComparison.OrdinalIgnoreCase);
            }
        }

        private static void RestoreGitState()
        {
            ProcessStartInfo si = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-Command \"git checkout .; git clean -fd\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = GetProjectRoot()
            };
            using (Process p = Process.Start(si)) { p.WaitForExit(); }
            AssetDatabase.Refresh();
        }
    }
}