using SiphoinUnityHelpers.XNodeExtensions.AsyncNodes;
using Cysharp.Threading.Tasks;
using UnityEngine;
using SNEngine.SaveSystem;
using SNEngine;
using CoreGame.Services;
using XNode;
using CoreGame.FightSystem.UI;
namespace CoreGame.FightSystem
{
    public class FightNode : AsyncNode, ISaveProgressNode
    {
        private const float DELAY_FINISH_FIGHT = 0.2f;
        [SerializeField] private FightCharacter _playerCharacter;
        [SerializeField] private FightCharacter _enemyCharacter;
        [Output(ShowBackingValue.Never), SerializeField, Header("Victory - 0 Defeat - 1 Tie - 2")] private int _result;
        private bool _finished;
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
            fightService.TurnFight(_playerCharacter, _enemyCharacter);
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
            return null;
        }

        public void ResetSaveBehaviour()
        {
            _finished = false;
        }

        public void SetDataFromSave(object data)
        {
            
        }

        public override object GetValue(NodePort port)
        {
            return _result;
        }
    }
}