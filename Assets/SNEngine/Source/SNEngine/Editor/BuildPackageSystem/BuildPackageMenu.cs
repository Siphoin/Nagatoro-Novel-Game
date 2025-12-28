using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using System.Diagnostics;

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
            try
            {
                AssetDatabase.StartAssetEditing();
                await RunCppCleanup();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                AssetDatabase.StopAssetEditing();
                UnityEngine.Debug.LogError(e.Message);
            }
        }

        private static async Task RunCppCleanup()
        {
            string root = GetProjectRoot();
            string fullPath = Path.GetFullPath(Path.Combine(root, CLEANER_EXE_REL_PATH));
            string workDir = Path.GetDirectoryName(fullPath);

            if (!File.Exists(fullPath)) return;

            ProcessStartInfo si = new ProcessStartInfo
            {
                FileName = fullPath,
                Arguments = "\"" + root + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workDir
            };

            using (Process p = Process.Start(si))
            {
                if (p != null) await Task.Run(() => p.WaitForExit());
            }
        }

        [MenuItem(MENU_2)]
        public static void Step2()
        {
            DialogueCreatorEditor.CreateNewDialogueAsset();
        }

        [MenuItem(MENU_3)]
        public static void Step3()
        {
            CharacterCreatorWindow.CreateBlankCharacter();
        }

        [MenuItem(MENU_4)]
        public static void Step4_Build()
        {
            string exportPath = EditorUtility.OpenFolderPanel("Save Package", "", "");
            if (string.IsNullOrEmpty(exportPath)) return;

            string packagePath = Path.Combine(exportPath, "SNEngine.unitypackage");
            string[] assets = { "Assets/SNEngine", "Assets/WebGLTemplates" };
            AssetDatabase.ExportPackage(assets, packagePath, ExportPackageOptions.Recurse);
        }

        [MenuItem(MENU_5)]
        public static void Step5_Restore() => RestoreGitState();

        private static string GetProjectRoot() => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

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