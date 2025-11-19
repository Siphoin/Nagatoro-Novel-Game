using TMPro;
using UnityEngine;

namespace SNEngine.Animations.TextEffects
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public abstract class TextEffect : MonoBehaviour
    {
        private TextMeshProUGUI textMesh;
        [SerializeField] private PrinterText _printerText;

        protected bool AllTextWrited => _printerText.AllTextWrited;

        protected TextMeshProUGUI Component => textMesh;

        private void Awake()
        {
            textMesh = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            if (_printerText != null)
                _printerText.OnTextForceCompleted += HandleComplete;
        }

        private void OnDisable()
        {
            if (_printerText != null)
                _printerText.OnTextForceCompleted -= HandleComplete;
        }

        private void HandleComplete() => TextForceCompleted(textMesh);
        protected abstract void TextForceCompleted(TextMeshProUGUI textMesh);
    }
}
