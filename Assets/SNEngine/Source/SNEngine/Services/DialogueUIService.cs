using SNEngine.DialogSystem;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SNEngine.Services
{
    [CreateAssetMenu(menuName = "SNEngine/Services/Dialogue UI Service")]
    public class DialogueUIService : ServiceBase, IResetable, IPrinterText, IPrinterTalkingCharacter
    {
        private IDialogWindow _dialogWindow;


        public override void Initialize()
        {
            var dialogWindow = Resources.Load<DialogWindow>("UI/dialogue");

            var dialogWindowPrefab = Object.Instantiate(dialogWindow);

            dialogWindowPrefab.name = dialogWindow.name;

            Object.DontDestroyOnLoad(dialogWindowPrefab);

            _dialogWindow = dialogWindowPrefab;

            var uiService = NovelGame.Instance.GetService<UIService>();

            uiService.AddElementToUIContainer(dialogWindowPrefab.gameObject);

            ResetState();

        }

        public void ShowDialog (IDialogNode dialogNode)
        {
            _dialogWindow.SetData(dialogNode);

            _dialogWindow.Show();

            _dialogWindow.StartOutputDialog();
        }

        public void HideDialog ()
        {
            _dialogWindow.Hide();
        }

        public override void ResetState()
        {
           _dialogWindow.ResetState();
        }

        #region Font
        public void SetFontDialog(TMP_FontAsset font)
        {
            _dialogWindow.SetFontDialog(font);
        }

        public void ResetFont()
        {
            _dialogWindow?.ResetFont();
        }

        public void SetFontTextTalkingCharacter(TMP_FontAsset font)
        {
            _dialogWindow.SetFontTextTalkingCharacter(font);
        }

        #endregion
    }
}
