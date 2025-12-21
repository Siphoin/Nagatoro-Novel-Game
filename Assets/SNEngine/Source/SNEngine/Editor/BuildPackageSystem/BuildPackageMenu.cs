using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using SNEngine.Editor; // Доступ к DialogueCreatorEditor и CharacterCreatorWindow

namespace SNEngine.Editor.BuildPackageSystem
{
    public static class BuildPackageMenu
    {
        private const string MENU_1_CLEAN = "SNEngine/Package/1. Run Python Cleanup";
        private const string MENU_2_DIAL = "SNEngine/Package/2. Create Blank Dialogue";
        private const string MENU_3_CHAR = "SNEngine/Package/3. Create Blank Character";
        private const string MENU_4_BUILD = "SNEngine/Package/4. Build Package";
        private const string MENU_5_RESTORE = "SNEngine/Package/5. Restore Project (Git)";

        private const string PYTHON_SCRIPT_REL_PATH = "Assets/SNEngine/Source/SNEngine/Editor/Python/build_cleanup.py";
        private const string CHAR_SAVE_PATH = "Assets/SNEngine/Source/SNEngine/Resources/Characters";
        private const string BLANK_CHAR_NAME = "Blank Character";

        [MenuItem(MENU_1_CLEAN)]
        public static async void Step1_Cleanup()
        {
            if (!EditorUtility.DisplayDialog("Step 1: Cleanup", "Run Python script to delete garbage?", "Yes", "Cancel")) return;

            try
            {
                AssetDatabase.StartAssetEditing();
                EditorUtility.DisplayProgressBar("Package System", "Python Cleanup...", 0.5f);
                await RunPythonCleanup();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
                Debug.Log("<color=cyan>[Package System]</color> Step 1: Cleanup complete.");
            }
            catch (Exception e) { AssetDatabase.StopAssetEditing(); Debug.LogError(e.Message); }
            finally { EditorUtility.ClearProgressBar(); }
        }

        [MenuItem(MENU_2_DIAL)]
        public static void Step2_CreateDialogue()
        {
            EditorUtility.DisplayProgressBar("Package System", "Creating Dialogue...", 0.5f);
            DialogueCreatorEditor.CreateNewDialogueAssetFromName("_startDialogue.asset");
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
            Debug.Log("<color=cyan>[Package System]</color> Step 2: _startDialogue created.");
        }

        [MenuItem(MENU_3_CHAR)]
        public static void Step3_CreateCharacter()
        {
            EditorUtility.DisplayProgressBar("Package System", "Creating Character...", 0.5f);

            if (!Directory.Exists(CHAR_SAVE_PATH)) Directory.CreateDirectory(CHAR_SAVE_PATH);
            string assetPath = Path.Combine(CHAR_SAVE_PATH, $"{BLANK_CHAR_NAME}.asset");

            var character = ScriptableObject.CreateInstance<SNEngine.CharacterSystem.Character>();
            character.Editor_SetName(BLANK_CHAR_NAME);
            character.Editor_SetDescription("Default character for package structure.");

            AssetDatabase.CreateAsset(character, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.ClearProgressBar();
            Debug.Log($"<color=cyan>[Package System]</color> Step 3: {BLANK_CHAR_NAME} created.");
        }

        [MenuItem(MENU_4_BUILD)]
        public static void Step4_Build()
        {
            if (IsOnMasterBranch())
            {
                EditorUtility.DisplayDialog("Build Package", "Switch branch from master!", "OK");
                return;
            }

            string exportPath = EditorUtility.OpenFolderPanel("Save unitypackage", "", "");
            if (string.IsNullOrEmpty(exportPath)) return;

            string packagePath = Path.Combine(exportPath, "SNEngine.unitypackage");
            ExportWorker.ExportPackage(packagePath);
            Debug.Log($"<color=green>[Package System]</color> Step 4: Package exported.");
        }

        [MenuItem(MENU_5_RESTORE)]
        public static void Step5_Restore()
        {
            if (!EditorUtility.DisplayDialog("Step 5: Restore", "Revert all changes via Git?", "Yes", "Cancel")) return;

            EditorUtility.DisplayProgressBar("Package System", "Git Restore...", 0.5f);
            RestoreGitState();
            EditorUtility.ClearProgressBar();
            Debug.Log("<color=orange>[Package System]</color> Step 5: Project restored.");
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
                string output = await p.StandardOutput.ReadToEndAsync();
                p.WaitForExit();
                if (!string.IsNullOrEmpty(output)) Debug.Log($"[Python]: {output}");
            }
        }

        private static string GetProjectRoot() => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static bool IsOnMasterBranch()
        {
            ProcessStartInfo si = new ProcessStartInfo { FileName = "powershell.exe", Arguments = "-Command \"git branch --show-current\"", UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true, WorkingDirectory = GetProjectRoot() };
            using (Process p = Process.Start(si)) { return p.StandardOutput.ReadToEnd().Trim().Equals("master", StringComparison.OrdinalIgnoreCase); }
        }

        private static void RestoreGitState()
        {
            ProcessStartInfo si = new ProcessStartInfo { FileName = "powershell.exe", Arguments = "-Command \"git checkout .; git clean -fd\"", UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = GetProjectRoot() };
            using (Process p = Process.Start(si)) { p.WaitForExit(); }
            AssetDatabase.Refresh();
        }
    }
}