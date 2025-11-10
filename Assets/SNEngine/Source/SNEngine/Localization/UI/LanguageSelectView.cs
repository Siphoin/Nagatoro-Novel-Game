using SNEngine.Extensions;
using SNEngine.Localization.Models;
using SNEngine.UI;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SNEngine.Localization.UI
{
    public class LanguageSelectView : MonoBehaviour
    {
        [SerializeField] private Image _imageLanguage;
        [SerializeField] private TextMeshProUGUI _textNameLanguage;
        [SerializeField] private Button _button;
        [SerializeField] private UIEventRelay _imageClickRelay;
        [SerializeField] private UIEventRelay _textClickRelay;
        private string _codeLanguage;
        public event Action<string> OnSelect;

        private void OnEnable()
        {
            _button.onClick.AddListener(Select);
            if (_imageClickRelay != null)
            {
                _imageClickRelay.OnClick += Select;
            }
            if (_textClickRelay != null)
            {
                _textClickRelay.OnClick += Select;
            }
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(Select);
            if (_imageClickRelay != null)
            {
                _imageClickRelay.OnClick -= Select;
            }
            if (_textClickRelay != null)
            {
                _textClickRelay.OnClick -= Select;
            }
        }

        private void Select()
        {
            if (!string.IsNullOrEmpty(_codeLanguage))
            {
                OnSelect?.Invoke(_codeLanguage);
            }
        }

        public void SetData(LanguageMetaData languageMetaData, Sprite icon, string code)
        {
            _textNameLanguage.text = languageMetaData.NameLanguage;
            _codeLanguage = code;
            _imageLanguage.sprite = icon;
            _imageLanguage.SetAdaptiveSize();
        }
    }
}