using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using SiphoinUnityHelpers.XNodeExtensions;
using SNEngine.Debugging;
using SNEngine.Graphs;
using SNEngine.IO;
using SNEngine.Localization;
using SNEngine.SaveSystem.Models;
using SNEngine.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Overlays;
using UnityEngine;
using SaveData = SNEngine.SaveSystem.Models.SaveData;

namespace SNEngine.SaveSystem
{
    [CreateAssetMenu(menuName = "SNEngine/Services/Save Load Service")]
    public class SaveLoadService : ServiceBase, IService
    {
        private const string SAVE_FOLDER_NAME = "saves";
        private const string SAVE_FILE_NAME = "progress.json";
        private const string PREVIEW_FILE_NAME = "preview.png";
        private const int PREVIEW_IMAGE_SIZE = 1512;

        private Dictionary<string, object> _originalVaritableValues;
        private DialogueGraph _currentGraph;

        public UniTask Save(string saveName, SaveData data)
        {
            string folderPath = GetSaveFolderPath(saveName);
            string saveFilePath = GetSaveFilePath(saveName);
            string previewFilePath = GetPreviewFilePath(saveName);

            try
            {
                if (!NovelDirectory.Exists(folderPath))
                {
                    NovelDirectory.Create(folderPath);
                }

                Formatting formatting =
#if UNITY_EDITOR
                    Formatting.Indented;
#else
                    Formatting.None;
#endif

                string json = JsonConvert.SerializeObject(data, formatting);

                NovelGameDebug.Log($"[SaveLoadService] Saving data to: {saveFilePath}");

                UniTask saveJsonTask = NovelFile.WriteAllTextAsync(saveFilePath, json);

                UniTask cameraTask = CameraSaver.SaveCameraRenderToPNGAsync(PREVIEW_IMAGE_SIZE, previewFilePath);

                return UniTask.WhenAll(saveJsonTask, cameraTask);
            }
            catch (Exception ex)
            {
                NovelGameDebug.LogError($"[SaveLoadService] Failed to save '{saveName}': {ex.Message}");
                return UniTask.CompletedTask;
            }
        }

        public async UniTask<PreloadSave> LoadPreloadSave(string saveName)
        {
            string saveFilePath = GetSaveFilePath(saveName);
            string previewFilePath = GetPreviewFilePath(saveName);

            try
            {
                string json = await NovelFile.ReadAllTextAsync(saveFilePath);
                SaveData saveData = JsonConvert.DeserializeObject<SaveData>(json);

                Texture2D previewTexture = await LoadTextureAsync(previewFilePath);

                NovelGameDebug.Log($"[SaveLoadService] Loaded preload save data: {saveName} from {saveFilePath}");

                return new PreloadSave
                {
                    SaveData = saveData,
                    PreviewTexture = previewTexture,
                    SaveName = saveName
                };
            }
            catch (FileNotFoundException)
            {
                NovelGameDebug.LogError($"[SaveLoadService] Save file not found for: {saveName}");
                return null;
            }
            catch (Exception ex)
            {
                NovelGameDebug.LogError($"[SaveLoadService] Failed to load/deserialize '{saveName}': {ex.Message}");
                return null;
            }
        }

        public async UniTask<SaveData> LoadSave(string saveName)
        {
            string saveFilePath = GetSaveFilePath(saveName);

            try
            {
                string json = await NovelFile.ReadAllTextAsync(saveFilePath);
                SaveData saveData = JsonConvert.DeserializeObject<SaveData>(json);
                NovelGameDebug.Log($"[SaveLoadService] Loaded save: {saveName} from {saveFilePath}");
                return saveData;


            }
            catch (FileNotFoundException)
            {
                NovelGameDebug.LogError($"[SaveLoadService] Save file not found for: {saveName}");
                return null;
            }
            catch (Exception ex)
            {
                NovelGameDebug.LogError($"[SaveLoadService] Failed to load/deserialize '{saveName}': {ex.Message}");
                return null;
            }
        }

        public UniTask<IEnumerable<string>> GetAllAvailableSaves()
        {
            string rootPath = GetRootSaveFolderPath();

            if (!NovelDirectory.Exists(rootPath))
            {
                return UniTask.FromResult(Enumerable.Empty<string>());
            }

            string[] saveFolders = Directory.GetDirectories(rootPath);

            IEnumerable<string> saveNames = saveFolders.Select(Path.GetFileName);

            return UniTask.FromResult(saveNames);
        }

        private async UniTask<Texture2D> LoadTextureAsync(string path)
        {
            if (!NovelFile.Exists(path))
            {
                NovelGameDebug.LogWarning($"[SaveLoadService] Preview file not found at: {path}");
                return null;
            }

            byte[] bytes = await NovelFile.ReadAllBytesAsync(path);

            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(bytes);

            return texture;
        }

        public void LoadDataGraph(DialogueGraph graph, SaveData saveData)
        {
            _currentGraph = graph;
            _originalVaritableValues = new Dictionary<string, object>();

            IEnumerable<ISaveProgressNode> nodes = graph.AllNodes
                .Select(x => x.Value)
                .OfType<ISaveProgressNode>();

            IEnumerable<VaritableNode> varitableNodes = graph.nodes
                .OfType<VaritableNode>();

            var globalVaritables = NovelGame.Instance.GetService<VaritablesContainerService>().GlobalVaritables;

            foreach (var node in nodes)
            {
                var data = saveData.NodesData;

                if (data.TryGetValue(node.GUID, out var savedData))
                {
                    node.SetDataFromSave(savedData);
                }

                else
                {
                    NovelGameDebug.LogError($"save data for node {node.GUID} not founs");
                }
            }

            foreach (var node in varitableNodes)
            {
                _originalVaritableValues[node.GUID] = node.GetStartValue();

                var data = saveData.Varitables;

                if (data.TryGetValue(node.GUID, out var savedData))
                {
                    node.SetValue(savedData);
                }

                else
                {
                    NovelGameDebug.LogError($"save data for node {node.GUID} not found");
                }
            }

            foreach (var node in globalVaritables.Values)
            {
                _originalVaritableValues[node.GUID] = node.GetStartValue();

                var data = saveData.GlobalVaritables;

                if (data.TryGetValue(node.GUID, out var savedData))
                {
                    node.SetValue(savedData);
                }

                else
                {
                    NovelGameDebug.LogError($"save data for node {node.GUID} not found");
                }
        }

#if UNITY_EDITOR
            graph.OnEndExecute -= RestoreOriginalVaritableValues;
            graph.OnEndExecute += RestoreOriginalVaritableValues;
#endif

        }

        public void ResetDataGraph(DialogueGraph graph)
        {

            IEnumerable<ISaveProgressNode> nodes = graph.AllNodes
                .Select(x => x.Value)
                .OfType<ISaveProgressNode>();

            foreach (var node in nodes)
            {
                node.ResetSaveBehaviour();
            }

        }

        public Dictionary<string, object> ExtractSaveDataFromGraph(DialogueGraph graph)
        {
            Dictionary<string, object> result = new();
            IEnumerable<ISaveProgressNode> nodes = graph.AllNodes
                .Select(x => x.Value)
                .OfType<ISaveProgressNode>();

            foreach (var node in nodes)
            {
                var key = node.GUID;
                var value = node.GetDataForSave();
                result.Add(key, value);
            }

            return result;
        }

        private string GetRootSaveFolderPath()
        {
            return Path.Combine(NovelDirectory.PersistentDataPath, SAVE_FOLDER_NAME);
        }

        private string GetSaveFolderPath(string saveName)
        {
            return Path.Combine(GetRootSaveFolderPath(), saveName);
        }

        private string GetSaveFilePath(string saveName)
        {
            return Path.Combine(GetSaveFolderPath(saveName), SAVE_FILE_NAME);
        }

        private string GetPreviewFilePath(string saveName)
        {
            return Path.Combine(GetSaveFolderPath(saveName), PREVIEW_FILE_NAME);
        }

#if UNITY_EDITOR
        private void RestoreOriginalVaritableValues()
        {
            if (_originalVaritableValues == null || _currentGraph == null) return;

            IEnumerable<VaritableNode> varitableNodes = _currentGraph.AllNodes
                .Select(x => x.Value)
                .OfType<VaritableNode>();

            foreach (var node in varitableNodes)
            {
                if (_originalVaritableValues.TryGetValue(node.GUID, out var originalValue))
                {
                    node.SetValue(originalValue);
                }
            }

            NovelGameDebug.Log("Restored original varitable values after graph execution from save.");
            _originalVaritableValues.Clear();
            _currentGraph = null;
        }
#endif
    }
}