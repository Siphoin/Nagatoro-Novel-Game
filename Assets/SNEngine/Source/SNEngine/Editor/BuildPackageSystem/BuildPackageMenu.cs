using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace SNEngine.Editor.BuildPackageSystem
{
    public static class BuildPackageMenu
    {
        private const string MENU_CLEAR = "SNEngine/Package/1. Clear garbage for build";
        private const string MENU_BUILD = "SNEngine/Package/2. Build Package";
        private const string MENU_RESTORE = "SNEngine/Package/3. Restore project (Git)";

        private const string DIALOGUES_PATH = "Assets/SNEngine/Source/SNEngine/Resources/Dialogues";
        private const string TEMPLATE_PATH = "Assets/SNEngine/Source/SNEngine/Editor/Templates/DialogueTemplate.asset";
        private const string PYTHON_SCRIPT_REL_PATH = "Assets/SNEngine/Source/SNEngine/Editor/Python/build_cleanup.py";
        private const string START_DIALOGUE_NAME = "_startDialogue.asset";

        [MenuItem(MENU_CLEAR)]
        public static async void ClearGarbage()
        {
            if (!EditorUtility.DisplayDialog("Clear Garbage", "Run Python cleanup and recreate _startDialogue?", "Yes", "Cancel"))
                return;

            try
            {
                AssetDatabase.StartAssetEditing();
                EditorUtility.DisplayProgressBar("Package System", "Python Cleanup...", 0.3f);

                await RunPythonCleanup();

                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
                await WaitUntilReady();

                CreateStartDialogue();
                AssetDatabase.Refresh();

                Debug.Log("<color=cyan>[Package System]</color> Step 1 Complete: Project cleaned, _startDialogue created.");
            }
            catch (Exception e)
            {
                AssetDatabase.StopAssetEditing();
                Debug.LogError($"Clear failed: {e.Message}");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        [MenuItem(MENU_BUILD)]
        public static void BuildPackage()
        {
            if (IsOnMasterBranch())
            {
                EditorUtility.DisplayDialog("Build Package", "Switch branch from master first!", "OK");
                return;
            }

            string exportPath = EditorUtility.OpenFolderPanel("Select folder to save unitypackage", "", "");
            if (string.IsNullOrEmpty(exportPath)) return;

            string packagePath = Path.Combine(exportPath, "SNEngine.unitypackage");

            try
            {
                EditorUtility.DisplayProgressBar("Package System", "Exporting...", 0.5f);
                ExportWorker.ExportPackage(packagePath);
                Debug.Log($"<color=green>[Package System]</color> Step 2 Complete: Package saved at {packagePath}");
            }
            catch (Exception e) { Debug.LogError($"Build failed: {e.Message}"); }
            finally { EditorUtility.ClearProgressBar(); }
        }

        [MenuItem(MENU_RESTORE)]
        public static void RestoreProject()
        {
            if (!EditorUtility.DisplayDialog("Restore Project", "This will run 'git checkout .' and 'git clean -fd'. Continue?", "Yes", "Cancel"))
                return;

            try
            {
                EditorUtility.DisplayProgressBar("Package System", "Restoring Git state...", 0.5f);
                RestoreGitState();
                Debug.Log("<color=orange>[Package System]</color> Step 3 Complete: Project restored to Git state.");
            }
            catch (Exception e) { Debug.LogError($"Restore failed: {e.Message}"); }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private static async Task RunPythonCleanup()
        {
            string scriptPath = Path.Combine(GetProjectRoot(), PYTHON_SCRIPT_REL_PATH);
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{scriptPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WorkingDirectory = GetProjectRoot()
            };
            using (Process process = Process.Start(startInfo))
            {
                string output = await process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();
                if (!string.IsNullOrEmpty(output)) Debug.Log($"[Python]: {output}");
            }
        }

        private static void CreateStartDialogue()
        {
            if (!Directory.Exists(DIALOGUES_PATH)) Directory.CreateDirectory(DIALOGUES_PATH);
            string dest = Path.Combine(DIALOGUES_PATH, START_DIALOGUE_NAME);
            if (File.Exists(TEMPLATE_PATH))
            {
                File.Copy(TEMPLATE_PATH, dest, true);
                AssetDatabase.ImportAsset(dest, ImportAssetOptions.ForceUpdate);
            }
        }

        private static async Task WaitUntilReady()
        {
            while (EditorApplication.isUpdating || EditorApplication.isCompiling) await Task.Delay(200);
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