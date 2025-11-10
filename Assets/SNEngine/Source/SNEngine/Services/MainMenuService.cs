using SNEngine.DialogSystem;
using SNEngine.MainMenuSystem;
using UnityEngine;

namespace SNEngine.Services
{
    [CreateAssetMenu(menuName = "SNEngine/Services/Main Menu Service")]
    public class MainMenuService : ServiceBase, IShowable, IHidden
    {
        private IMainMenu _mainMenu;

        private DialogueService _dialogueService;

        public override void Initialize()
        {
            _dialogueService = NovelGame.Instance.GetService<DialogueService>();

            var ui = NovelGame.Instance.GetService<UIService>();

            var input = Resources.Load<MainMenu>("UI/MainMenu");

            var prefab = Object.Instantiate(input);

            prefab.name = input.name;

            _mainMenu = prefab;

            ui.AddElementToUIContainer(prefab.gameObject);

            prefab.gameObject.SetActive(false);

            _dialogueService.OnEndDialogue += OnEndDialogue;
        }

        private void OnEndDialogue(IDialogue dialogue)
        {
            if (!dialogue.HasNextDialogueOnExit())
            {
                Show();
            }
        }

        public void Show()
        {
            _mainMenu.Show();
        }


        public void Hide()
        {
            _mainMenu.Hide();
        }
    }
}
