using SNEngine.Services;
using SNEngine.Source.SNEngine.Services;
using UnityEngine;
using UnityEngine.UI;

namespace SNEngine.Source.SNEngine.MessageMenu
{
    public class MessageMenuWindow : MonoBehaviour, IMessageMenuWindow
    {
        [SerializeField] private Button _dialogueHistoryButton;
        [SerializeField] private Button _closeButton;

        private OpenMessageWindowButtonService _messageWindowButtonService;
        private InputService _inputService;
        
        private Button[] _buttons;

        private void OnEnable()
        {
            _messageWindowButtonService = NovelGame.Instance.GetService<OpenMessageWindowButtonService>();
            _inputService = NovelGame.Instance.GetService<InputService>();

            _buttons = new Button[]
            {
                _dialogueHistoryButton,
                _closeButton
            };

            foreach (var button in _buttons)
            {
                button.onClick.RemoveAllListeners();
            }

            _dialogueHistoryButton.onClick.AddListener(CloseMenu);
            _inputService.SetActiveInput(false);
        }

        private void CloseMenu()
        {
            _messageWindowButtonService.Hide();
        }

        public void ResetState()
        {
            throw new System.NotImplementedException();
        }

        public void Show()
        {
            throw new System.NotImplementedException();
        }

        public void Hide()
        {
            throw new System.NotImplementedException();
        }
    }
}