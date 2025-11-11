using SNEngine.Debugging;
using SNEngine.Localization.UI;
using SNEngine.Polling;
using SNEngine.SaveSystem.Models;
using SNEngine.Utils;
using System.Collections;
using System.Collections.Generic;
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
                    var save = await saveLoadService.Load(saveName);
                    var view = _pool.GetFreeElement();
                    view.gameObject.SetActive(true);
                    view.SetData(save);
                    _cacheSaves.Add(save);
                    

                }
                catch (System.Exception ex)
                {
                    NovelGameDebug.LogError($"Error getting save {saveName} Error: {ex.Message}");
                    continue;
                }

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