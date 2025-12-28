using SNEngine.Services;
using SNEngine.Source.SNEngine.MessageMenu;
using SNEngine.Utils;
using UnityEngine;

namespace SNEngine.Source.SNEngine.Services
{
    [CreateAssetMenu(menuName = "SNEngine/Services/Message Menu Window Service")]

    public class MessageMenuWindowService : ServiceBase, IShowable, IHidden
    {
        private IMessageMenuWindow _messageMenu;
        private const string MESSAGE_MENU_WINDOW_VANILLA_PATH = "UI/MessageMenuWindow";

        public override void Initialize()
        {
            var messageMenu = ResourceLoader.LoadCustomOrVanilla<MessageMenuWindow>(MESSAGE_MENU_WINDOW_VANILLA_PATH);

            if (messageMenu == null) return;
            
            var ui = NovelGame.Instance.GetService<UIService>();

            var prefab = Instantiate(messageMenu);
            
            _messageMenu = messageMenu;
            prefab.name = messageMenu.name;
            
            ui.AddElementToUIContainer(prefab.gameObject);
            
            prefab.gameObject.SetActive(false);
        }


        public void Show()
        {
            _messageMenu.Show();
        }

        public void Hide()
        {
            _messageMenu.Hide();
        }
    }
}