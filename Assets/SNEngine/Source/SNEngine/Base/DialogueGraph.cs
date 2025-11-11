using SiphoinUnityHelpers.XNodeExtensions;
using SNEngine.DialogSystem;
using SNEngine.Extensions;
using SNEngine.SaveSystem.Models;
using SNEngine.Services;
using UnityEngine;

namespace SNEngine.Graphs
{
    [CreateAssetMenu(menuName = "SNEngine/New Dialogue Graph")]
    public class DialogueGraph : BaseGraph, IDialogue
    {
        public object Name => name;

        public BaseNode CurrentExecuteNode => Queue.Current;

        public override void Execute()
        {
            NovelGame.Instance.GetService<LanguageService>().TransliteGraph(this);
            base.Execute();
        }

        public bool HasNextDialogueOnExit()
        {
            return Queue.HasNextDialogueOnExit();
        }

        public void LoadSave (string nodeGuid)
        {
            NovelGame.Instance.GetService<LanguageService>().TransliteGraph(this);
            JumpToNode(nodeGuid);
        }

       
    }
}
