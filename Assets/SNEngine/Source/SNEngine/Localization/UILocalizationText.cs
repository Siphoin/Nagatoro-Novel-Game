using Cysharp.Threading.Tasks;
using SiphoinUnityHelpers.XNodeExtensions.Attributes;
using SNEngine.Services;
using System.Collections;
using TMPro;
using UnityEngine;
namespace SNEngine.Localization
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UILocalizationText : MonoBehaviour
    {
        [SerializeField] private string _key;
        [SerializeField, ReadOnly(ReadOnlyMode.Always)] private TextMeshProUGUI _component;

        private LanguageService LanguageService => NovelGame.Instance.GetService<LanguageService>();

        private void OnEnable()
        {
            LanguageService.OnLanguageLoaded += OnLanguageLoaded;

            if (LanguageService.LanguageIsLoaded)
            {
                OnLanguageLoaded(LanguageService.CurrentLanguageCode);
            }
        }

        private void OnDisable()
        {
            LanguageService.OnLanguageLoaded -= OnLanguageLoaded;
        }

        private void OnLanguageLoaded(string languageCode)
        {
            _component.text = LanguageService.TransliteUI(_key);
        }

        private void OnValidate()
        {
            if (!_component)
            {
                _component = GetComponent<TextMeshProUGUI>();
            }
        }
    }
}