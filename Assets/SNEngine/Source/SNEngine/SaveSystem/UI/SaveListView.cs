using SNEngine.Debugging;
using SNEngine.Localization.UI;
using SNEngine.Polling;
using SNEngine.SaveSystem.Models;
using SNEngine.Services;
using SNEngine.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SNEngine.SaveSystem.UI
{
    public class SaveListView : MonoBehaviour, ISaveListView
    {
        [SerializeField] private RectTransform _containerSaves;
        private PoolMono<SaveView> _pool;
        private List<PreloadSave> _cacheSaves = new();

        private async void OnEnable()
        {
            foreach (var save in _cacheSaves)
            {
                save.Dispose();
            }

            _cacheSaves.Clear();

            for (int i = 0; i < _containerSaves.childCount; i++)
            {
                var child = _containerSaves.GetChild(i).gameObject;
                child.gameObject.SetActive(false);
            }

            if (_pool is null)
            {
                var prefab = ResourceLoader.LoadCustomOrVanilla<SaveView>("UI/saveView");
                _pool = new(prefab, _containerSaves, 9, true);
            }

            var saveLoadService = NovelGame.Instance.GetService<SaveLoadService>();
            var savesDirectories = await saveLoadService.GetAllAvailableSaves();
            foreach (var saveName in savesDirectories)
            {
                try
                {
                    var save = await saveLoadService.LoadPreloadSave(saveName);
                    var view = _pool.GetFreeElement();
                    view.gameObject.SetActive(true);
                    view.SetData(save);
                    view.OnSelect -= OnSaveSelected;
                    view.OnSelect += OnSaveSelected;
                    _cacheSaves.Add(save);
                    

                }
                catch (System.Exception ex)
                {
                    NovelGameDebug.LogError($"Error getting save {saveName} Error: {ex.Message}");
                    continue;
                }

            }
        }

        private async void OnSaveSelected(string saveName)
        {
            var targetSave = _cacheSaves.FirstOrDefault(x => x.SaveName == saveName);
            var saveLoadService = NovelGame.Instance.GetService<SaveLoadService>();
            SaveData saveData = await saveLoadService.LoadSave(saveName);

            if (targetSave != null)
            {
                NovelGame.Instance.GetService<DialogueService>().ToDialogue(saveData);
                NovelGame.Instance.GetService<MainMenuService>().Hide();
                NovelGame.Instance.GetService<SaveListViewService>().Hide();
            }
        }

        private void OnDisable()
        {
            foreach (var save in _cacheSaves)
            {
                save.Dispose();
            }

            _cacheSaves.Clear();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }
    }
}