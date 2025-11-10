using SNEngine.Services;
using SNEngine.UserDataSystem.Models;
using SNEngine.IO;
using UnityEngine;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using SNEngine.Debugging;

namespace SNEngine.Services
{
    [CreateAssetMenu(menuName = "SNEngine/Services/User Data Service")]
    public class UserDataService : ServiceBase, IService, IResetable
    {
        private const string DATA_FOLDER_NAME = "UserData";
        private const string FILE_NAME = "userdata.json";

        private UserData _data;

        public UserData Data => _data;

        public bool DataIsLoaded => _data != null;

        public override async void Initialize()
        {
            _data = await LoadAsync();
        }

        private string GetFolderPath()
        {
            return Path.Combine(NovelDirectory.PersistentDataPath, DATA_FOLDER_NAME);
        }

        private string GetFilePath()
        {
            return Path.Combine(GetFolderPath(), FILE_NAME);
        }

        public async UniTask<UserData> LoadAsync()
        {
            string folderPath = GetFolderPath();
            string filePath = GetFilePath();

            try
            {
                if (!NovelDirectory.Exists(folderPath))
                {
                    await NovelDirectory.CreateAsync(folderPath);
                    NovelGameDebug.Log($"[UserDataService] Created directory: {folderPath}");
                }

                string json = await NovelFile.ReadAllTextAsync(filePath);
                _data = JsonConvert.DeserializeObject<UserData>(json);

                NovelGameDebug.Log($"[UserDataService] Loaded successfully from: {filePath}");
            }
            catch (FileNotFoundException)
            {
                _data = new UserData();
                NovelGameDebug.LogWarning($"[UserDataService] File not found at: {filePath}. Creating default data and saving.");
                await SaveAsync();
            }
            catch (Exception ex)
            {
                NovelGameDebug.LogError($"[UserDataService] Failed to load/deserialize data: {ex.Message}. Creating default data and attempting to save.");
                _data = new UserData();
                await SaveAsync();
            }

            _data ??= new UserData();
            return _data;
        }

        public UniTask SaveAsync()
        {
            string folderPath = GetFolderPath();
            string filePath = GetFilePath();

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

                string json = JsonConvert.SerializeObject(_data, formatting);

                NovelGameDebug.Log($"[UserDataService] Saving to: {filePath} (Format: {formatting})");
                return NovelFile.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                NovelGameDebug.LogError($"[UserDataService] Failed to save data: {ex.Message}");
                return UniTask.CompletedTask;
            }
        }
    }
}