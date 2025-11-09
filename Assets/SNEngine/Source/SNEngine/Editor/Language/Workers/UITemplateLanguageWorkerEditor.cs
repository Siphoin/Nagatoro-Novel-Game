using Cysharp.Threading.Tasks;
using SNEngine.Debugging;
using SNEngine.IO;
using System;
using System.IO;
using UnityEngine;

namespace SNEngine.Editor.Language.Workers
{
    [CreateAssetMenu(menuName = "SNEngine/Editor/Language/Workers/UI Template Language Worker")]
    public class UITemplateLanguageWorkerEditor : LanguageEditorWorker
    {
        public static string PathSave { get; set; }
        private const string TEMPLATE_SOURCE_PATH = "Assets/SNEngine/Source/SNEngine/Resources/Editor/TextTemplates/ui_template.yaml";
        private const string OUTPUT_FILE_NAME = "ui.yaml";

        public override async UniTask<LanguageWorkerResult> Work()
        {
            LanguageWorkerResult result = new();

            if (string.IsNullOrEmpty(PathSave))
            {
                string error = $"[{nameof(UITemplateLanguageWorkerEditor)}] PathSave not set";
                NovelGameDebug.LogError(error);
                result.Message = error;
                result.State = LanguageWorkerState.Error;
                return result;
            }

            string fullPath = Path.Combine(PathSave, OUTPUT_FILE_NAME);
            string directory = Path.GetDirectoryName(fullPath);

            if (!NovelDirectory.Exists(directory))
            {
                await NovelDirectory.CreateAsync(directory);
            }

            if (!File.Exists(TEMPLATE_SOURCE_PATH))
            {
                string error = $"[{nameof(UITemplateLanguageWorkerEditor)}] Template file not found: {TEMPLATE_SOURCE_PATH}";
                NovelGameDebug.LogError(error);
                result.Message = error;
                result.State = LanguageWorkerState.Error;
                return result;
            }

            try
            {
                byte[] templateBytes = await NovelFile.ReadAllBytesAsync(TEMPLATE_SOURCE_PATH);
                await NovelFile.WriteAllBytesAsync(fullPath, templateBytes);
            }
            catch (Exception ex)
            {
                string error = $"[{nameof(UITemplateLanguageWorkerEditor)}] Failed to copy UI template: {ex.Message}";
                NovelGameDebug.LogError(error);
                result.Message = error;
                result.State = LanguageWorkerState.Error;
                return result;
            }

            return result;
        }
    }
}