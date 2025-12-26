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
        private const string MENU_1 = "SNEngine/Package/1. Run C++ Cleanup";
        private const string MENU_2 = "SNEngine/Package/2. Create Blank Dialogue";
        private const string MENU_3 = "SNEngine/Package/3. Create Blank Character";
        private const string MENU_4 = "SNEngine/Package/4. Build Package";
        private const string MENU_5 = "SNEngine/Package/5. Restore Project (Git)";

        private const string CLEANER_EXE_REL_PATH = "Assets/SNEngine/Source/SNEngine/Editor/Utils/SNEngine_Cleaner.exe";

        [MenuItem(MENU_1)]
        public static async void Step1_Cleanup()
        {
            if (!ValidateBranch()) return;
            if (!EditorUtility.DisplayDialog("Step 1", "Clean project using C++ utility?", "Yes", "Cancel")) return;

            try
            {
                AssetDatabase.StartAssetEditing();
                await RunCppCleanup();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
                Debug.Log("<color=cyan>[Package]</color> C++ Cleanup complete.");
            }
            catch (Exception e)
            {
                AssetDatabase.StopAssetEditing();
                Debug.LogError($"Cleanup failed: {e.Message}");
            }
        }

        private static async Task RunCppCleanup()
        {
            string root = GetProjectRoot();
            string exePath = Path.Combine(root, CLEANER_EXE_REL_PATH);

            Debug.Log($"<color=orange>[Cleaner]</color> Attempting to run: {exePath}");
            Debug.Log($"<color=orange>[Cleaner]</color> Project Root passed: {root}");

            if (!File.Exists(exePath))
            {
                Debug.LogError($"<color=red>[Cleaner]</color> EXE NOT FOUND at: {exePath}");
                return;
            }

            ProcessStartInfo si = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"\"{root}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(exePath),
                RedirectStandardError = true
            };

            try
            {
                using (Process p = Process.Start(si))
                {
                    if (p == null)
                    {
                        Debug.LogError("<color=red>[Cleaner]</color> Failed to start process (p is null).");
                        return;
                    }

                    await Task.Run(() => p.WaitForExit());

                    if (p.ExitCode != 0)
                    {
                        Debug.LogWarning($"<color=yellow>[Cleaner]</color> Process finished with code: {p.ExitCode}. Check if cleanup_list.txt exists next to exe.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"<color=red>[Cleaner]</color> Critical Error: {ex.Message}");
            }
        }

        [MenuItem(MENU_2)] public static void Step2() => AssetDatabase.Refresh();
        [MenuItem(MENU_3)] public static void Step3() => Debug.Log("Logic");

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

        private static bool ValidateBranch() => !IsOnMasterBranch();

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
            try
            {
                using (Process p = Process.Start(si))
                {
                    string branch = p.StandardOutput.ReadToEnd().Trim();
                    return branch.Equals("master", StringComparison.OrdinalIgnoreCase) || branch.Equals("main", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { return false; }
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