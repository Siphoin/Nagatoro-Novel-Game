using SNEngine.SaveSystem.Models;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SNEngine.SaveSystem.UI
{
    public class SaveView : MonoBehaviour
    {
        [SerializeField] private RawImage _rawImage;
        [SerializeField] private TextMeshProUGUI _textNameSave;
        [SerializeField] private TextMeshProUGUI _textDateSave;
        [SerializeField] private Button _button;
        private string _saveName;

        public event Action<string> OnSelect;

        private void OnEnable()
        {
            _button.onClick.AddListener(Select);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(Select);
        }

        private void Select()
        {
            OnSelect?.Invoke(_saveName);
        }

        public void SetData (PreloadSave data)
        {
            _rawImage.texture = data.PreviewTexture;
            _saveName = data.SaveName;
            _textNameSave.text = data.SaveName;
            _textDateSave.text = data.SaveData.DateSave.ToString("dd.mm:yyyy");
        }
    }
}