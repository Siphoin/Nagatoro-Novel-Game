using Cysharp.Threading.Tasks;
using SiphoinUnityHelpers.XNodeExtensions;
using SNEngine.Debugging;
using SNEngine.Graphs;
using SNEngine.SaveSystem.Models;
using SNEngine.Services;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using SaveData = SNEngine.SaveSystem.Models.SaveData;

namespace SNEngine.SaveSystem
{
    [CreateAssetMenu(menuName = "SNEngine/Services/Save Load Service")]
    public class SaveLoadService : ServiceBase, IService
    {
        private const int PREVIEW_IMAGE_SIZE = 1512;
        private const int WEBGL_PREVIEW_SIZE = 256;

        private ISaveLoadProvider _provider;
        private Dictionary<string, object> _originalVaritableValues;
        private DialogueGraph _currentGraph;

        public override void Initialize()
        {
            base.Initialize();

#if UNITY_WEBGL
            _provider = new PlayerPrefsSaveLoadProvider();
            NovelGameDebug.Log("[SaveLoadService] Initialized with PlayerPrefsSaveLoadProvider for WebGL.");
#else
            _provider = new FileSaveLoadProvider();
            NovelGameDebug.Log("[SaveLoadService] Initialized with FileSaveLoadProvider for FileSystem.");
#endif
        }

        private UniTask Save(string saveName, SaveData data)
        {
            if (_provider == null)
            {
                NovelGameDebug.LogError("[SaveLoadService] Provider is not initialized.");
                return UniTask.CompletedTask;
            }

            UniTask<Texture2D> captureTask = CameraSaver.CaptureScreenAndCropAsync(PREVIEW_IMAGE_SIZE);

            return captureTask.ContinueWith(originalTexture =>
            {
                Texture2D textureToSave = originalTexture;

                if (_provider is PlayerPrefsSaveLoadProvider && originalTexture != null)
                {
                    textureToSave = ResizeTexture(originalTexture, WEBGL_PREVIEW_SIZE, WEBGL_PREVIEW_SIZE);
                    Object.Destroy(originalTexture);
                }

                return _provider.SaveAsync(saveName, data, textureToSave);
            });
        }

        public UniTask<PreloadSave> LoadPreloadSave(string saveName)
        {
            if (_provider == null)
            {
                NovelGameDebug.LogError("[SaveLoadService] Provider is not initialized.");
                return UniTask.FromResult<PreloadSave>(null);
            }
            return _provider.LoadPreloadSaveAsync(saveName);
        }

        public UniTask<SaveData> LoadSave(string saveName)
        {
            if (_provider == null)
            {
                NovelGameDebug.LogError("[SaveLoadService] Provider is not initialized.");
                return UniTask.FromResult<SaveData>(null);
            }
            return _provider.LoadSaveAsync(saveName);
        }

        public UniTask<IEnumerable<string>> GetAllAvailableSaves()
        {
            if (_provider == null)
            {
                NovelGameDebug.LogError("[SaveLoadService] Provider is not initialized.");
                return UniTask.FromResult(Enumerable.Empty<string>());
            }
            return _provider.GetAllAvailableSavesAsync();
        }

        private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
        {
            RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
            rt.filterMode = FilterMode.Bilinear;
            RenderTexture.active = rt;

            Graphics.Blit(source, rt);

            Texture2D newTexture = new Texture2D(newWidth, newHeight);
            newTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
            newTexture.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            return newTexture;
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

        public async UniTask SaveCurrentState(string saveName)
        {
            var dialogueService = NovelGame.Instance.GetService<DialogueService>();
            var globalVaritablesService = NovelGame.Instance.GetService<VaritablesContainerService>();

            if (dialogueService.CurrentDialogue is not DialogueGraph dialogueGraph)
            {
                return;
            }

            var nodeGuid = dialogueGraph.CurrentExecuteNode.GUID;
            var varitables = dialogueGraph.Varitables;
            var globalVaritables = globalVaritablesService.GlobalVaritables;

            Dictionary<string, object> varitablesData = new();
            foreach (var varitable in varitables)
            {
                var guid = varitable.Value.GUID;
                var valueNode = varitable.Value.GetCurrentValue();
                varitablesData.Add(guid, valueNode);
            }

            Dictionary<string, object> globalVaritablesData = new();
            foreach (var varitable in globalVaritables)
            {
                var guid = varitable.Value.GUID;
                var valueNode = varitable.Value.GetCurrentValue();
                globalVaritablesData.Add(guid, valueNode);
            }

            var nodesData = ExtractSaveDataFromGraph(dialogueGraph);

            SaveData saveData = new()
            {
                CurrentNode = nodeGuid,
                Varitables = varitablesData,
                GlobalVaritables = globalVaritablesData,
                DialogueGUID = dialogueGraph.GUID,
                DateSave = System.DateTime.Now,
                NodesData = nodesData,
            };

            await Save(saveName, saveData);
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