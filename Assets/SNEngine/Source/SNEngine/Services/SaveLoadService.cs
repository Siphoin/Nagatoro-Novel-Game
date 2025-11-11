using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using SNEngine.Debugging;
using SNEngine.IO;
using SNEngine.Services;
using SNEngine.SaveSystem.Models;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SNEngine.Graphs;

namespace SNEngine.SaveSystem
{
    [CreateAssetMenu(menuName = "SNEngine/Services/Save Load Service")]
    public class SaveLoadService : ServiceBase, IService
    {
        private const string SAVE_FOLDER_NAME = "saves";
        private const string SAVE_FILE_NAME = "progress.json";
        private const string PREVIEW_FILE_NAME = "preview.png";
        private const int PREVIEW_IMAGE_SIZE = 1512;

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
    }
}