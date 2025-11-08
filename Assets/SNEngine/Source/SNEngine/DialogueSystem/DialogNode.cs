using SNEngine.CharacterSystem;
using SNEngine.Debugging;
using SNEngine.Localization;
using SNEngine.Services;
using UnityEngine;

namespace SNEngine.DialogSystem
{
    public class DialogNode : PrinterTextNode, IDialogNode
    {
        [Space]

        [SerializeField] private Character _character;

        public Character Character => _character;

        public override void Execute()
        {
            base.Execute();

            var serviceDialogs = NovelGame.Instance.GetService<DialogueUIService>();

            serviceDialogs.ShowDialog(this);
        }
    }

}