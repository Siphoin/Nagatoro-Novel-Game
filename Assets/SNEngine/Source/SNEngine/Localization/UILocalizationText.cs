using SiphoinUnityHelpers.XNodeExtensions.Attributes;
using SNEngine.Services;
using TMPro;
using UnityEngine;
namespace SNEngine.Localization
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UILocalizationText : MonoBehaviour
    {
        [SerializeField] private string _key;
        [SerializeField, ReadOnly(ReadOnlyMode.Always)] private TextMeshProUGUI _component;
        [SerializeField] private bool _autoLocalize = true;
        [SerializeField] private bool _showKey = true;

        public bool NotCanTranslite {  get; private set; }

        private LanguageService LanguageService => NovelGame.Instance.GetService<LanguageService>();

        private void OnEnable()
        {
            LanguageService.OnLanguageLoaded += OnLanguageLoaded;
            if (_autoLocalize)
            {
                if (LanguageService.LanguageIsLoaded)
                {
                    OnLanguageLoaded(LanguageService.CurrentLanguageCode);
                }

                else
                {
                    if (_key.StartsWith("%") && !string.IsNullOrWhiteSpace(_key))
                    {
                        _component.text = LocalizationConstants.GetValue(_key);
                    }
                }
            }
        }

        private void OnDisable()
        {
            LanguageService.OnLanguageLoaded -= OnLanguageLoaded;
        }

        private void OnLanguageLoaded(string languageCode)
        {
            Translite();
        }

        private void Translite()
        {
            if (_key.StartsWith("%") && !string.IsNullOrWhiteSpace(_key))
            {
                _component.text = LocalizationConstants.GetValue(_key);
            }
            else
            {
                string result = LanguageService.TransliteUI(_key);

                if (result != _key && !_showKey)
                {
                    _component.text = result;
                }

                else if (_showKey)
                {
                    _component.text = result;
                }

                NotCanTranslite = !LanguageService.LanguageIsLoaded;
            }
        }

        private void OnValidate()
        {
            if (!_component)
            {
                _component = GetComponent<TextMeshProUGUI>();
            }
        }

        public void ChangeKey (string key)
        {
            _key = key;
            Translite();
        }
    }
}