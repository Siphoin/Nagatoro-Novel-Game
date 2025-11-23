using CoreGame.FightSystem.Models;
using CoreGame.FightSystem.UI;
using CoreGame.Services;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SiphoinUnityHelpers.XNodeExtensions.AsyncNodes;
using SNEngine;
using SNEngine.SaveSystem;
using System;
using UnityEngine;
using XNode;
namespace CoreGame.FightSystem
{
    public class FightNode : AsyncNode, ISaveProgressNode
    {
        private const float DELAY_FINISH_FIGHT = 0.2f;
        [SerializeField] private FightCharacter _playerCharacter;
        [SerializeField] private FightCharacter _enemyCharacter;
        [Output(ShowBackingValue.Never), SerializeField, Header("Victory - 0 Defeat - 1 Tie - 2")] private int _result;
        private bool _finished;
        private FightServiceSaveData _saveData;

        public override async void Execute()
        {
            if (!_playerCharacter)
            {
                Debug.LogError($"fight player character not seted to node {GUID}");
                return;
            }

            if (!_enemyCharacter)
            {
                Debug.LogError($"fight enemy character not seted to node {GUID}");
                return;
            }
            base.Execute();

            var fightService = NovelGame.Instance.GetService<FightService>();
            fightService.OnFightEnded += OnFightEnded;
            fightService.TurnFight(_playerCharacter, _enemyCharacter, _saveData);
        }

        private async void OnFightEnded(FightResult result)
        {
            var fightService = NovelGame.Instance.GetService<FightService>();
            fightService.OnFightEnded -= OnFightEnded;
            _result = (int)result;
            await UniTask.WaitForSeconds(FightWindow.ANIMATION_DURATION_HIDE + DELAY_FINISH_FIGHT);
            StopTask();
        }

        public override bool CanSkip()
        {
            return _finished;
        }

        public object GetDataForSave()
        {
            return NovelGame.Instance.GetService<FightService>().GetSaveData();
        }

        public void ResetSaveBehaviour()
        {
            _saveData = null;
            _finished = false;
        }

        public void SetDataFromSave(object data)
        {
            if (data is JObject jObject)
            {
                try
                {
                    FightServiceSaveData saveData = jObject.ToObject<FightServiceSaveData>();
                    if (saveData != null)
                    {
                        _saveData = saveData;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Deserialization error: {e.Message}");
                }
            }
            else if (data is FightServiceSaveData directSaveData)
            {
                _saveData = directSaveData;
            }
        }

        public override object GetValue(NodePort port)
        {
            return _result;
        }
    }
}