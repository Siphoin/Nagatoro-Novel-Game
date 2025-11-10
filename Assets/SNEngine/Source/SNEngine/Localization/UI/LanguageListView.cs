using Cysharp.Threading.Tasks;
using SNEngine.Debugging;
using SNEngine.IO;
using SNEngine.Polling;
using SNEngine.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SNEngine.Localization.UI
{
    public class LanguageListView : MonoBehaviour, ILanguageListView
    {
        [SerializeField] private RectTransform _containerLanguages;
        private PoolMono<LanguageSelectView> _pool;
        private List<Sprite> _spritesFlags = new();
        private List<Texture2D> _texturesFlags = new();

        private async void OnEnable()
        {
            foreach (var flag in _spritesFlags)
            {
                Destroy(flag);
            }
            _spritesFlags.Clear();

            foreach (var texture in _texturesFlags)
            {
                Destroy(texture);
            }
            _texturesFlags.Clear();

            if (_pool is null)
            {
                var prefab = Resources.Load<LanguageSelectView>("UI/selectLanguageView");
                _pool = new(prefab, _containerLanguages, 9, true);
            }
            for (int i = 0; i < _containerLanguages.childCount; i++)
            {
                var child = _containerLanguages.GetChild(i).gameObject;
                child.SetActive(false);
            }

            var languageService = NovelGame.Instance.GetService<LanguageService>();
            var data = await languageService.GetAvailableLanguagesAsync();

            foreach (var languageData in data)
            {
                var languageCode = languageData.Key;
                var langData = languageData.Value;

                var flagSprite = await LoadFlagTextureAsync(langData.PathFlag);

                if (flagSprite == null) continue;

                var view = _pool.GetFreeElement();
                view.gameObject.SetActive(true);

                _spritesFlags.Add(flagSprite);

                view.SetData(langData.MetaData, flagSprite, languageCode);
                view.OnSelect -= OnLanguageSelected;
                view.OnSelect += OnLanguageSelected;
            }
        }

        private async UniTask<Sprite> LoadFlagTextureAsync(string absolutePath)
        {
            if (!NovelFile.Exists(absolutePath))
            {
                NovelGameDebug.LogError($"Flag file not found: {absolutePath}");
                return null;
            }

            try
            {
                byte[] flagBytes = await NovelFile.ReadAllBytesAsync(absolutePath);

                Texture2D flagTexture = new Texture2D(2, 2);
                flagTexture.LoadImage(flagBytes);
                flagTexture.name = Path.GetFileName(absolutePath);
                _texturesFlags.Add(flagTexture);

                Sprite sprite = Sprite.Create(
                    flagTexture,
                    new Rect(0, 0, flagTexture.width, flagTexture.height),
                    Vector2.one * 0.5f,
                    100.0f
                );

                return sprite;
            }
            catch (Exception ex)
            {
                NovelGameDebug.LogError($"Failed to load flag texture from {absolutePath}: {ex.Message}");
                return null;
            }
        }

        private void OnLanguageSelected(string code)
        {
            var languageService = NovelGame.Instance.GetService<LanguageService>();
            languageService.LoadLanguage(code).Forget();
            var userDataService = NovelGame.Instance.GetService<UserDataService>();
            userDataService.Data.CurrentLanguage = code;
            userDataService.SaveAsync().Forget();
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