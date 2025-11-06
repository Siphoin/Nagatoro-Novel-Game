using Cysharp.Threading.Tasks;
using SNEngine.Debugging;
using SNEngine.DialogSystem;
using SNEngine.Graphs;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SNEngine.Services
{
    [CreateAssetMenu(menuName = "SNEngine/Services/Dialogue Service")]
    internal class DialogueService : ServiceBase, IService
    {
        private const int TIME_OUT_WAIT_TO_NEW_RENDERER = 35;

        private IDialogue _currentDialogue;

        private IDialogue _startDialogue;

        private IOldRenderDialogue _oldRenderDialogueService;

        public event Action<IDialogue> OnEndDialogue;

        private MonoBehaviour _frameDetector;

        public override void Initialize()
        {
            _oldRenderDialogueService = NovelGame.Instance.GetService<RenderOldDialogueService>();

            _startDialogue = Resources.Load<DialogueGraph>($"Dialogues/{nameof(_startDialogue)}");

            var frameDetector = Resources.Load<Dialog_FrameDetector>("System/Dialog_FrameDetector");

            var prefabFrameDetector = Instantiate(frameDetector);

            prefabFrameDetector.name = frameDetector.name;

            DontDestroyOnLoad(prefabFrameDetector);

            _frameDetector = prefabFrameDetector;

            JumpToStartDialogue();
            
        }

        public void JumpToStartDialogue()
        {
            JumpToDialogue(_startDialogue);
        }

        public void JumpToDialogue(IDialogue dialogue)
        {
            if (dialogue is null)
            {
                NovelGameDebug.LogError("dialogue argument is null. Check your graph");
            }

            _currentDialogue?.Stop();

            _currentDialogue = dialogue;

            _currentDialogue.OnEndExecute += OnEndExecute;

            NovelGameDebug.Log($"Jump To Dialogue: {_currentDialogue.Name}");

            _currentDialogue.Execute();
        }

        private void OnEndExecute()
        {
            _currentDialogue.OnEndExecute -= OnEndExecute;

            OnEndDialogue?.Invoke(_currentDialogue);

            ClearScreen().Forget();
        }

        private async UniTask ClearScreen()
        {
            _oldRenderDialogueService.UpdateRender();

            NovelGame.Instance.ResetStateServices();

            await UniTask.WaitForEndOfFrame(_frameDetector);

            await UniTask.Delay(TIME_OUT_WAIT_TO_NEW_RENDERER);

            _oldRenderDialogueService.Clear();
        }
    }
}
