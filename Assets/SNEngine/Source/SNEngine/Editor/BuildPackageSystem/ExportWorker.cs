using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SNEngine.Editor.BuildPackageSystem
{
    public static class ExportWorker
    {
        public static void ExportPackage(string packagePath)
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
                
            Debug.Log($"[ExportWorker] Package exported to: {packagePath}");
        }
    }
}