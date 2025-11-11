using Cysharp.Threading.Tasks;
using SNEngine.Debugging;
using SNEngine.DialogSystem;
using SNEngine.Graphs;
using SNEngine.SaveSystem.Models;
using SNEngine.Utils;
using System;
using System.Linq;
using UnityEngine;

namespace SNEngine.Services
{
    [CreateAssetMenu(menuName = "SNEngine/Services/Dialogue Service")]
    internal class DialogueService : ServiceBase, IService
    {
        private const int TIME_OUT_WAIT_TO_NEW_RENDERER = 35;
        private const string FRAME_DETECTOR_VANILLA_PATH = "System/Dialog_FrameDetector";

        private IDialogue _currentDialogue;
        private IDialogue _startDialogue;
        private IOldRenderDialogue _oldRenderDialogueService;

        public event Action<IDialogue> OnEndDialogue;

        private MonoBehaviour _frameDetector;

        public IDialogue CurrentDialogue => _currentDialogue;

        public override void Initialize()
        {
            _oldRenderDialogueService = NovelGame.Instance.GetService<RenderOldDialogueService>();

            _startDialogue = Resources.Load<DialogueGraph>($"Dialogues/{nameof(_startDialogue)}");

            Dialog_FrameDetector frameDetectorToLoad =
                ResourceLoader.LoadCustomOrVanilla<Dialog_FrameDetector>(FRAME_DETECTOR_VANILLA_PATH);

            if (frameDetectorToLoad == null)
            {
                return;
            }

            string prefabName = frameDetectorToLoad.name;

            var prefabFrameDetector = Instantiate(frameDetectorToLoad);

            prefabFrameDetector.name = prefabName;

            DontDestroyOnLoad(prefabFrameDetector);

            _frameDetector = prefabFrameDetector;

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

        public void ToDialogue(SaveData saveData)
        {
            var dislogues = Resources.LoadAll<DialogueGraph>("Dialogues");
            var targetDialogue = dislogues.FirstOrDefault(x => x.GUID == saveData.DialogueGUID);

            if (targetDialogue != null)
            {
                targetDialogue.LoadSave(saveData.CurrentNode);
            }

            _currentDialogue = targetDialogue;
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