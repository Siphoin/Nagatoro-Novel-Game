using Cysharp.Threading.Tasks;
using SharpYaml.Serialization;
using SNEngine.Debugging;
using SNEngine.IO;
using SNEngine.Graphs;
using SNEngine.Localization;
using SNEngine.Localization.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SNEngine.Editor.Language.Workers
{
    [CreateAssetMenu(menuName = "SNEngine/Editor/Language/Workers/Dialogues Language Worker")]
    public class DialoguesLanguageWorkerEditor : LanguageEditorWorker
    {
        private const string ROOT_FOLDER = "dialogues";
        public static string PathSave { get; set; }

        public override async UniTask<LanguageWorkerResult> Work()
        {
            LanguageWorkerResult result = new();

            if (string.IsNullOrEmpty(PathSave))
            {
                string error = $"[{nameof(DialoguesLanguageWorkerEditor)}] PathSave not set";
                NovelGameDebug.LogError(error);
                result.Message = error;
                result.State = LanguageWorkerState.Error;
                return result;
            }

            IEnumerable<DialogueGraph> dialogues = Resources.LoadAll<DialogueGraph>("Dialogues");

            foreach (var dialogue in dialogues)
            {
                string dialoguePath = Path.Combine(PathSave, ROOT_FOLDER);
                if (!NovelDirectory.Exists(dialoguePath))
                    await NovelDirectory.CreateAsync(dialoguePath);

                string filePath = Path.Combine(dialoguePath, $"{dialogue.name}.yaml");
                var currentData = GetNodesFromGraph(dialogue)
                    .ToDictionary(n => n.GUID, n => (object)n.GetValue());

                Dictionary<string, object> existingData = null;
                if (File.Exists(filePath))
                {
                    try
                    {
                        string existingYaml = await NovelFile.ReadAllTextAsync(filePath);
                        Serializer deserializer = new Serializer();
                        existingData = deserializer.Deserialize<Dictionary<string, object>>(existingYaml);
                    }
                    catch (Exception ex)
                    {
                        NovelGameDebug.LogError($"[{nameof(DialoguesLanguageWorkerEditor)}] Failed to read {filePath}: {ex.Message}");
                    }
                }

                var mergedData = MergeNodeData(existingData, currentData);

                Serializer serializer = new Serializer();
                string output = serializer.Serialize(mergedData);
                await NovelFile.WriteAllTextAsync(filePath, output);
            }

            return result;
        }

        private IEnumerable<ILocalizationNode> GetNodesFromGraph(DialogueGraph dialogueGraph)
        {
            return dialogueGraph.AllNodes.Values
                .Where(x => x is ILocalizationNode)
                .Cast<ILocalizationNode>();
        }

        private Dictionary<string, object> MergeNodeData(Dictionary<string, object> existing, Dictionary<string, object> current)
        {
            if (existing == null)
                return current;

            var result = new Dictionary<string, object>();

            foreach (var kvp in current)
            {
                if (existing.TryGetValue(kvp.Key, out var oldValue))
                {
                    result[kvp.Key] = !Equals(oldValue, kvp.Value) ? oldValue : kvp.Value;
                }
                else
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return result;
        }
    }
}
