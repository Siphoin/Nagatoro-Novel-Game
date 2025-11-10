using Cysharp.Threading.Tasks;
using SNEngine.Debugging;
using SNEngine.IO;
using System;
using System.IO;
using UnityEngine;
using SharpYaml.Serialization;
using System.Collections.Generic;
using System.Linq;

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

            if (!NovelFile.Exists(TEMPLATE_SOURCE_PATH))
            {
                string error = $"[{nameof(UITemplateLanguageWorkerEditor)}] Template file not found: {TEMPLATE_SOURCE_PATH}";
                NovelGameDebug.LogError(error);
                result.Message = error;
                result.State = LanguageWorkerState.Error;
                return result;
            }

            Dictionary<string, string> templateData = null;
            Dictionary<string, string> existingData = null;

            try
            {
                string templateYaml = await NovelFile.ReadAllTextAsync(TEMPLATE_SOURCE_PATH);
                Serializer deserializer = new Serializer();
                templateData = deserializer.Deserialize<Dictionary<string, string>>(templateYaml);
            }
            catch (Exception ex)
            {
                string error = $"[{nameof(UITemplateLanguageWorkerEditor)}] Failed to read/deserialize template file: {ex.Message}";
                NovelGameDebug.LogError(error);
                result.Message = error;
                result.State = LanguageWorkerState.Error;
                return result;
            }

            if (NovelFile.Exists(fullPath))
            {
                try
                {
                    string existingYaml = await NovelFile.ReadAllTextAsync(fullPath);
                    Serializer deserializer = new Serializer();
                    existingData = deserializer.Deserialize<Dictionary<string, string>>(existingYaml);
                }
                catch (Exception ex)
                {
                    string error = $"[{nameof(UITemplateLanguageWorkerEditor)}] Failed to read existing UI file: {ex.Message}";
                    NovelGameDebug.LogError(error);
                    result.Message = error;
                    result.State = LanguageWorkerState.Error;
                }
            }

            Dictionary<string, string> mergedData = MergeUIData(existingData, templateData);

            try
            {
                Serializer serializer = new Serializer();
                string outputData = serializer.Serialize(mergedData);
                await NovelFile.WriteAllTextAsync(fullPath, outputData);
            }
            catch (Exception ex)
            {
                string error = $"[{nameof(UITemplateLanguageWorkerEditor)}] Failed to write UI data: {ex.Message}";
                NovelGameDebug.LogError(error);
                result.Message = error;
                result.State = LanguageWorkerState.Error;
                return result;
            }

            return result;
        }

        private Dictionary<string, string> MergeUIData(
            Dictionary<string, string> existing,
            Dictionary<string, string> template)
        {
            if (template == null)
                return existing ?? new Dictionary<string, string>();

            if (existing == null)
                return template;

            Dictionary<string, string> merged = new Dictionary<string, string>();

            foreach (var kvp in template)
            {
                merged[kvp.Key] = kvp.Value;
            }

            foreach (var kvp in existing)
            {
                if (merged.ContainsKey(kvp.Key))
                {
                    merged[kvp.Key] = kvp.Value;
                }
            }

            return merged;
        }
    }
}