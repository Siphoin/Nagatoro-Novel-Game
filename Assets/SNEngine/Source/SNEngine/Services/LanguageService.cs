using Cysharp.Threading.Tasks;
using SharpYaml.Serialization;
using SNEngine.CharacterSystem;
using SNEngine.Debugging;
using SNEngine.Graphs;
using SNEngine.IO;
using SNEngine.Localization;
using SNEngine.Localization.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SNEngine.Services
{
    [CreateAssetMenu(menuName = "SNEngine/Services/Language Service")]
    public class LanguageService : ServiceBase
    {
        private Dictionary<string, CharacterLocalizationData> _chatacterLocalizeData;
        private Dictionary<string, NodeLocalizationData> _nodesLocalizeData;
        private Dictionary<string, string> _uiLocalizeData;
        private LanguageMetaData _metaData;
        private Texture2D _flag;
        private Dictionary<string, object> _originalNodeValues;
        private DialogueGraph _currentGraph;
        public event Action<string> OnLanguageLoaded;
#if UNITY_EDITOR
        [SerializeField] private string _testLang = "ru";
#endif

        public bool LanguageIsLoaded
        {
            get
            {
                return _metaData != null;
            }
        }
        public string CurrentLanguageCode { get; private set; }
        public LanguageMetaData MetaData => _metaData;

        public Texture2D Flag => _flag;

        private void OnEnable()
        {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif

            _metaData = null;
            _flag = null;
            _originalNodeValues = null;
            _nodesLocalizeData = null;
            _uiLocalizeData = null;

        }

#if UNITY_EDITOR

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.ExitingEditMode)
            {
                RestoreOriginalNodeValuesOnExit();
            }
        }

        private void RestoreOriginalNodeValuesOnExit()
        {
            if (_originalNodeValues == null || _currentGraph == null) return;

            var nodes = _currentGraph.AllNodes
                .Select(x => x.Value)
                .OfType<ILocalizationNode>();

            foreach (var node in nodes)
            {
                if (_originalNodeValues.TryGetValue(node.GUID, out var originalValue))
                {
                    node.SetValue(originalValue);
                }
            }

            NovelGameDebug.Log("Restored original node values due to exiting PlayMode/EditMode.");
            _originalNodeValues.Clear();
            _currentGraph = null;
        }
#endif

        public async UniTask LoadLanguage(string codeLanguage)
        {
            CurrentLanguageCode = null;

            string langPath = Path.Combine(NovelDirectory.StreamingAssetsPath, "Language", codeLanguage);

            if (!NovelDirectory.Exists(langPath))
            {
                NovelGameDebug.LogError($"Language folder not found: {langPath}");
                return;
            }

            await LoadCharactersAsync(langPath);
            await LoadFlagAsync(langPath);
            await LoadMetadataAsync(langPath);
            await LoadDialoguesAsync(langPath);
            await LoadUIAsync(langPath);

            CurrentLanguageCode = codeLanguage;
            OnLanguageLoaded?.Invoke(codeLanguage);
            NovelGameDebug.Log($"Language {codeLanguage} loaded successfully!");
        }

        private async UniTask LoadCharactersAsync(string langPath)
        {
            string charactersPath = Path.Combine(langPath, "characters.yaml");
            _chatacterLocalizeData = new Dictionary<string, CharacterLocalizationData>();

            if (!NovelFile.Exists(charactersPath)) return;

            try
            {
                string yamlText = await NovelFile.ReadAllTextAsync(charactersPath);
                Serializer deserializer = new Serializer();
                var charactersList = deserializer.Deserialize<List<CharacterLocalizationData>>(yamlText);

                foreach (var c in charactersList)
                    _chatacterLocalizeData[c.GUID] = c;

                NovelGameDebug.Log($"Loaded {_chatacterLocalizeData.Count} characters");
            }
            catch (Exception ex)
            {
                NovelGameDebug.LogError($"Failed to load characters.yaml: {ex.Message}");
            }
        }

        private async UniTask LoadFlagAsync(string langPath)
        {
            string flagPath = Path.Combine(langPath, "flag.png");
            _flag = null;

            if (!NovelFile.Exists(flagPath)) return;

            try
            {
                byte[] flagBytes = await NovelFile.ReadAllBytesAsync(flagPath);
                _flag = new Texture2D(2, 2);
                _flag.LoadImage(flagBytes);
            }
            catch (Exception ex)
            {
                NovelGameDebug.LogError($"Failed to load flag.png: {ex.Message}");
            }
        }

        private async UniTask LoadMetadataAsync(string langPath)
        {
            string metadataPath = Path.Combine(langPath, "metadata.yaml");
            _metaData = null;

            if (!NovelFile.Exists(metadataPath)) return;

            try
            {
                string yamlText = await NovelFile.ReadAllTextAsync(metadataPath);
                Serializer deserializer = new Serializer();
                _metaData = deserializer.Deserialize<LanguageMetaData>(yamlText);
            }
            catch (Exception ex)
            {
                NovelGameDebug.LogError($"Failed to load metadata.yaml: {ex.Message}");
            }
        }

        private async UniTask LoadDialoguesAsync(string langPath)
        {
            string dialogiesPath = Path.Combine(langPath, "dialogues");
            Debug.Log(dialogiesPath);
            _nodesLocalizeData = new Dictionary<string, NodeLocalizationData>();

            if (!NovelDirectory.Exists(dialogiesPath)) return;

            string[] dialogueFiles = await NovelDirectory.GetFilesAsync(dialogiesPath, "*.yaml");
            Serializer deserializer = new Serializer();

            int totalNodes = 0;
            int filesProcessed = 0;

            foreach (var file in dialogueFiles)
            {
                try
                {
                    string yamlText = await NovelFile.ReadAllTextAsync(file);
                    var dict = deserializer.Deserialize<Dictionary<string, object>>(yamlText);

                    int nodesInFile = 0;
                    foreach (var kvp in dict)
                    {
                        NodeLocalizationData nodeData = new NodeLocalizationData
                        {
                            GUID = kvp.Key,
                            Value = kvp.Value
                        };
                        _nodesLocalizeData.Add(kvp.Key, nodeData);
                        totalNodes++;
                        nodesInFile++;
                    }

                    filesProcessed++;
                    NovelGameDebug.Log($"Loaded {nodesInFile} nodes from {Path.GetFileName(file)}");
                }
                catch (Exception ex)
                {
                    NovelGameDebug.LogError($"Failed to load dialogue {file}: {ex.Message}");
                }
            }


            NovelGameDebug.Log($"Loaded {_nodesLocalizeData.Count} nodes across {dialogueFiles.Length} dialogue files (total nodes counted: {totalNodes})");
        }


        private async UniTask LoadUIAsync(string langPath)
        {
            string uiPath = Path.Combine(langPath, "ui.yaml");
            _uiLocalizeData = new Dictionary<string, string>();

            if (!NovelFile.Exists(uiPath)) return;

            try
            {
                string yamlText = await NovelFile.ReadAllTextAsync(uiPath);
                Serializer deserializer = new Serializer();
                var uiDictionary = deserializer.Deserialize<Dictionary<string, string>>(yamlText);
                _uiLocalizeData = uiDictionary ?? new Dictionary<string, string>();

                NovelGameDebug.Log($"Loaded {_uiLocalizeData.Count} UI localization entries from ui.yaml");
            }
            catch (Exception ex)
            {
                NovelGameDebug.LogError($"Failed to load ui.yaml: {ex.Message}");
            }
        }

        public async UniTask<Dictionary<string, PreloadLanguageData>> GetAvailableLanguagesAsync()
        {
            Dictionary<string, PreloadLanguageData> availableLanguages = new Dictionary<string, PreloadLanguageData>();
            string langRootPath = Path.Combine(NovelDirectory.StreamingAssetsPath, "Language");

            if (!NovelDirectory.Exists(langRootPath))
            {
                NovelGameDebug.LogError($"Language root folder not found: {langRootPath}");
                return availableLanguages;
            }

            string[] languageFolders = await NovelDirectory.GetDirectoriesAsync(langRootPath);
            Serializer deserializer = new Serializer();

            foreach (var langPath in languageFolders)
            {
                string codeLanguage = Path.GetFileName(langPath);
                string metadataPath = Path.Combine(langPath, "metadata.yaml");
                string flagPath = Path.Combine(langPath, "flag.png");

                if (!NovelFile.Exists(metadataPath))
                {
                    NovelGameDebug.LogWarning($"Metadata file not found for language: {codeLanguage}");
                    continue;
                }

                try
                {
                    string yamlText = await NovelFile.ReadAllTextAsync(metadataPath);
                    LanguageMetaData metaData = deserializer.Deserialize<LanguageMetaData>(yamlText);

                    availableLanguages.Add(codeLanguage, new PreloadLanguageData
                    {
                        CodeLanguage = codeLanguage,
                        MetaData = metaData,
                        PathFlag = NovelFile.GetAbsolutePath(flagPath)
                    });
                }
                catch (Exception ex)
                {
                    NovelGameDebug.LogError($"Failed to read or deserialize metadata for language {codeLanguage}: {ex.Message}");
                }
            }

            return availableLanguages;
        }


        public void TransliteGraph(DialogueGraph graph)
        {
            if (!LanguageIsLoaded)
            {
                NovelGameDebug.LogError("language not loaded");
                return;
            }
            _currentGraph = graph;
            _originalNodeValues = new Dictionary<string, object>();

            IEnumerable<ILocalizationNode> nodes = _currentGraph.AllNodes
                .Select(x => x.Value)
                .OfType<ILocalizationNode>();

            foreach (var node in nodes)
            {
                _originalNodeValues[node.GUID] = node.GetValue();

                if (_nodesLocalizeData.TryGetValue(node.GUID, out var localize))
                {
                    node.SetValue(localize.Value);
                }

                else
                {
                    NovelGameDebug.LogError($"localize data for node {node.GUID} not found");
                }
            }

#if UNITY_EDITOR
            graph.OnEndExecute -= RestoreOriginalValues;
            graph.OnEndExecute += RestoreOriginalValues;
#endif
        }

        public string TransliteNameCharacter(Character character)
        {
            string name = character.OriginalName;
            if (!LanguageIsLoaded)
            {
                NovelGameDebug.LogError("language not loaded");
                return name;
            }
            if (_chatacterLocalizeData.TryGetValue(character.GUID, out var localize))
            {
                name = localize.Name;
            }

            return name;
        }


        public string TransliteUI(string key)
        {
            if (!LanguageIsLoaded)
            {
                NovelGameDebug.LogError("language not loaded");
                return key;
            }

            if (_uiLocalizeData.TryGetValue(key, out var localizedText))
            {
                return localizedText;
            }
            return key;
        }


#if UNITY_EDITOR
        private void RestoreOriginalValues()
        {
            if (_originalNodeValues == null) return;

            IEnumerable<ILocalizationNode> nodes = _currentGraph.AllNodes
                .Select(x => x.Value)
                .OfType<ILocalizationNode>();

            foreach (var node in nodes)
            {
                if (_originalNodeValues.TryGetValue(node.GUID, out var originalValue))
                {
                    node.SetValue(originalValue);
                }
            }

            NovelGameDebug.Log("Restored original node values after graph execution.");
        }
#endif

    }
}