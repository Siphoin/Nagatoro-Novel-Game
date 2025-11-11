using Cysharp.Threading.Tasks;
using SNEngine;
using SNEngine.Graphs;
using SNEngine.SaveSystem;
using SNEngine.SaveSystem.Models;
using SNEngine.Services;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.SNEngine.Source.SNEngine.SaveSystem
{
    public class TestSaveGame : MonoBehaviour
    {
        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.F2))
            {
                var dialogueService = NovelGame.Instance.GetService<DialogueService>();
                var globalVaritablesService = NovelGame.Instance.GetService<VaritablesContainerService>();
                DialogueGraph dialogueGraph = dialogueService.CurrentDialogue as DialogueGraph;
                var nodeGuid = dialogueGraph.CurrentExecuteNode.GUID;
                var varitables = dialogueGraph.Varitables;
                var globalVaritables = globalVaritablesService.GlobalVaritables;
                Dictionary<string, object> varitablesData = new();
                Dictionary<string, object> globalVaritablesData = new();
                foreach (var varitable in varitables)
                {
                    var guid = varitable.Value.GUID;
                    var valueNode = varitable.Value.GetCurrentValue();
                    varitablesData.Add(guid, valueNode);
                }

                foreach (var varitable in globalVaritables)
                {
                    var guid = varitable.Value.GUID;
                    var valueNode = varitable.Value.GetCurrentValue();
                    globalVaritablesData.Add(guid, valueNode);
                }

                SaveData saveData = new()
                {
                    CurrentNode = nodeGuid,
                    Varitables = varitablesData,
                    GlobalVaritables = globalVaritablesData,
                    DialogueGUID = dialogueGraph.GUID,
                };

                NovelGame.Instance.GetService<SaveLoadService>().Save("autosave", saveData).Forget();
            }
        }
    }
}