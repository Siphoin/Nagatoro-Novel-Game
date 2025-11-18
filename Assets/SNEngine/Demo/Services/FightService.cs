using CoreGame.FightSystem;
using CoreGame.FightSystem.Models;
using CoreGame.FightSystem.UI;
using SNEngine;
using SNEngine.CharacterSystem;
using SNEngine.MainMenuSystem;
using SNEngine.Services;
using SNEngine.Utils;
using System.Collections.Generic;
using UnityEngine;
using System;
using CoreGame.FightSystem.AI;
using Object = UnityEngine.Object;
using Cysharp.Threading.Tasks;
using System.Threading;
using DG.Tweening;
using SNEngine.Polling;

namespace CoreGame.Services
{
    [CreateAssetMenu(menuName = "SNEngine/Service/New FightService")]
    public class FightService : ServiceBase
    {
        private CharacterService _characterService;
        private BackgroundService _backgroundService;
        private Dictionary<Character, CharacterFightData> _currentStatsCharacters;
        private Dictionary<Character, IFightComponent> _fightComponents;
        private IFightWindow _fightWindow;
        private const string FIGHT_WINDOW_VANILLA_PATH = "UI/FightWindow";
        private const string HEAL_TEXT_VANILLA_PATH = "UI/HealText";
        private const string DAMAGE_TEXT_VANILLA_PATH = "UI/DamageText";
        private const string CRITICAL_DAMAGE_TEXT_VANILLA_PATH = "UI/CriticalDamageText";
        private const float ENEMY_TURN_DELAY = 0.5f;
        private FightTurnOwner _fightTurnOwner = FightTurnOwner.Player;

        private FightCharacter _playerCharacter;
        private FightCharacter _enemyCharacter;
        private AIFighter _aiFighter;
        private PoolMono<HealText> _poolHealText;
        private PoolMono<DamageText> _poolDamageText;
        private PoolMono<CriticalDamageText> _poolCriticalDamageText;

        private bool _isPlayerGuarding;
        private bool _isEnemyGuarding;

        [SerializeField] private float _hitShakeDuration = 0.3f;
        [SerializeField] private float _hitShakeStrength = 10f;
        [SerializeField] private int _hitShakeVibrato = 10;
        [SerializeField] private Color _hitColor = new Color(1f, 0f, 0f, 1f);
        [SerializeField] private float _hitColorDuration = 0.1f;
        [SerializeField] private Ease _hitColorEase = Ease.Linear;

        public event Action<FightResult> OnFightEnded;

        public override void Initialize()
        {
            _characterService = NovelGame.Instance.GetService<CharacterService>();
            _backgroundService = NovelGame.Instance.GetService<BackgroundService>();

            var ui = NovelGame.Instance.GetService<UIService>();

            var input = ResourceLoader.LoadCustomOrVanilla<FightWindow>(FIGHT_WINDOW_VANILLA_PATH);
            var healTextPrefab = ResourceLoader.LoadCustomOrVanilla<HealText>(HEAL_TEXT_VANILLA_PATH);
            var damageTextPrefab = ResourceLoader.LoadCustomOrVanilla<DamageText>(DAMAGE_TEXT_VANILLA_PATH);
            var criticalDamageTextPrefab = ResourceLoader.LoadCustomOrVanilla<CriticalDamageText>(CRITICAL_DAMAGE_TEXT_VANILLA_PATH);
            var containerTexts = new GameObject("Floating Texts");
            containerTexts.AddComponent<RectTransform>();

            if (input == null)
            {
                return;
            }

            var prefab = Object.Instantiate(input);

            prefab.name = input.name;

            _fightWindow = prefab;

            ui.AddElementToUIContainer(prefab.gameObject);

            prefab.gameObject.SetActive(false);
            containerTexts.transform.SetParent(prefab.transform, false);
            _poolHealText = new(healTextPrefab, containerTexts.transform, 9, true);
            _poolDamageText = new(damageTextPrefab, containerTexts.transform, 9, true);
            _poolCriticalDamageText = new(criticalDamageTextPrefab, containerTexts.transform, 3, true);


        }

        public override void ResetState()
        {
            if (_fightWindow != null)
            {
                _fightWindow.OnTurnExecuted -= OnPlayerTurnExecuted;
            }
            HideCharacters();
            ClearupFightComponents();
            _currentStatsCharacters = null;
            _fightWindow.ResetState();
            _aiFighter = null;
            _isPlayerGuarding = false;
            _isEnemyGuarding = false;
        }

        public void TurnFight(FightCharacter playerCharacter, FightCharacter enemyCharacter)
        {
            _playerCharacter = playerCharacter;
            _enemyCharacter = enemyCharacter;

            _currentStatsCharacters = new();
            _fightComponents = new();
            ShowCharacter(playerCharacter.ReferenceCharacter);
            ShowCharacter(enemyCharacter.ReferenceCharacter);
            SetupCharacterForFight(playerCharacter);
            SetupCharacterForFight(enemyCharacter);

            IFightComponent playerComp = _fightComponents[playerCharacter.ReferenceCharacter];
            IFightComponent enemyComp = _fightComponents[enemyCharacter.ReferenceCharacter];

            _fightWindow.SetData(playerComp, enemyComp);

            _aiFighter = new AIFighter(_enemyCharacter, enemyComp, _playerCharacter, playerComp);

            _isPlayerGuarding = false;
            _isEnemyGuarding = false;

            _fightWindow.OnTurnExecuted += OnPlayerTurnExecuted;

            _fightWindow.Show();
            _fightTurnOwner = FightTurnOwner.Player;
        }

        private async void OnPlayerTurnExecuted(PlayerAction action)
        {
            if (_fightTurnOwner != FightTurnOwner.Player)
            {
                return;
            }

            _fightWindow.HidePanelAction();

            await HandlePlayerAction(action);

            if (CheckFightEndConditions())
            {
                return;
            }

            _fightTurnOwner = FightTurnOwner.Enemy;

            ExecuteEnemyTurn().Forget();
        }

        private async UniTask HandlePlayerAction(PlayerAction action)
        {
            IFightComponent enemyComp = _fightComponents[_enemyCharacter.ReferenceCharacter];
            float playerDamage = _playerCharacter.Damage;

            _isPlayerGuarding = false;

            switch (action)
            {
                case PlayerAction.Attack:
                    await HandleAttackAction(enemyComp, playerDamage, _enemyCharacter, _playerCharacter);
                    break;
                case PlayerAction.Guard:
                    _isPlayerGuarding = true;
                    break;
                case PlayerAction.Wait:

                    break;
                case PlayerAction.Skill:

                    break;
            }
        }

        private async UniTask HandleEnemyAction(PlayerAction action)
        {
            IFightComponent playerComp = _fightComponents[_playerCharacter.ReferenceCharacter];
            float enemyDamage = _enemyCharacter.Damage;

            _isEnemyGuarding = false;

            switch (action)
            {
                case PlayerAction.Attack:
                    await HandleAttackAction(playerComp, enemyDamage, _playerCharacter, _enemyCharacter);
                    break;
                case PlayerAction.Guard:
                    _isEnemyGuarding = true;
                    break;
                case PlayerAction.Wait:

                    break;
                case PlayerAction.Skill:

                    break;
            }
        }

        private async UniTask HandleAttackAction(IFightComponent targetComponent, float baseDamage, FightCharacter targetCharacter, FightCharacter attackerCharacter)
        {
            float finalDamage = baseDamage;
            bool isCritical = false;

            if (UnityEngine.Random.Range(0f, 1f) < attackerCharacter.CriticalHitChance)
            {
                finalDamage *= attackerCharacter.CriticalHitMultiplier;
                isCritical = true;
            }

            bool isTargetGuarding = targetCharacter == _playerCharacter ? _isPlayerGuarding : _isEnemyGuarding;

            if (isTargetGuarding)
            {
                float reduction = targetCharacter.GuardReductionPercentage;
                finalDamage *= (1f - reduction);

                if (targetCharacter == _playerCharacter)
                {
                    _isPlayerGuarding = false;
                }
                else
                {
                    _isEnemyGuarding = false;
                }
            }

            targetComponent.HealthComponent.TakeDamage(finalDamage);

            Vector3 worldPosition = _characterService.GetCharacterWorldPosition(targetCharacter.ReferenceCharacter);
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);

            if (isCritical)
            {
                CriticalDamageText criticalDamageText = _poolCriticalDamageText.GetFreeElement();
                criticalDamageText.Show(finalDamage, screenPosition).Forget();
            }
            else
            {
                DamageText damageText = _poolDamageText.GetFreeElement();
                damageText.Show(finalDamage, screenPosition).Forget();
            }

            if (targetComponent.HealthComponent.CurrentHealth > 0)
            {
                if (targetCharacter == _playerCharacter)
                {
                    await UniTask.WhenAll(
                        _backgroundService.ShakePosition(_hitShakeDuration, _hitShakeStrength, _hitShakeVibrato, true),
                        AnimateBackgroundHit()
                    );
                }
                else
                {
                    await UniTask.WhenAll(
                        _characterService.ShakePosition(_enemyCharacter.ReferenceCharacter, _hitShakeDuration, _hitShakeStrength, _hitShakeVibrato, true),
                        AnimateCharacterHit(_enemyCharacter.ReferenceCharacter)
                    );
                }
            }
        }

        private async UniTask AnimateCharacterHit(Character character)
        {
            Color originalColor = Color.white;

            await _characterService.SetColorCharacter(character, _hitColor, _hitColorDuration, _hitColorEase);
            await _characterService.SetColorCharacter(character, originalColor, _hitColorDuration, _hitColorEase);
        }

        private async UniTask AnimateBackgroundHit()
        {
            Color originalColor = Color.white;

            await _backgroundService.SetColor(_hitColor, _hitColorDuration, _hitColorEase);
            await _backgroundService.SetColor(originalColor, _hitColorDuration, _hitColorEase);
        }

        private bool CheckFightEndConditions()
        {
            IFightComponent playerComp = _fightComponents[_playerCharacter.ReferenceCharacter];
            IFightComponent enemyComp = _fightComponents[_enemyCharacter.ReferenceCharacter];

            bool playerDead = playerComp.HealthComponent.CurrentHealth <= 0;
            bool enemyDead = enemyComp.HealthComponent.CurrentHealth <= 0;

            if (playerDead && enemyDead)
            {
                EndFight(FightResult.Tie);
                return true;
            }

            if (playerDead)
            {
                EndFight(FightResult.Defeat);
                return true;
            }

            if (enemyDead)
            {
                EndFight(FightResult.Victory);
                return true;
            }

            return false;
        }

        private void EndFight(FightResult result)
        {
            _fightWindow.OnTurnExecuted -= OnPlayerTurnExecuted;
            _fightWindow.Hide();
            HideCharacters();
            ClearupFightComponents();

            OnFightEnded?.Invoke(result);
        }

        private async UniTask ExecuteEnemyTurn()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(ENEMY_TURN_DELAY), DelayType.DeltaTime, PlayerLoopTiming.Update, CancellationToken.None);

            PlayerAction enemyAction = _aiFighter.DecideAction();

            await HandleEnemyAction(enemyAction);

            if (CheckFightEndConditions())
            {
                return;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(ENEMY_TURN_DELAY), DelayType.DeltaTime, PlayerLoopTiming.Update, CancellationToken.None);

            CompleteEnemyTurn();
        }

        private void CompleteEnemyTurn()
        {
            _fightTurnOwner = FightTurnOwner.Player;
            _fightWindow.ShowPanelAction();
        }

        private void SetupCharacterForFight(FightCharacter character)
        {
            CharacterFightData fightData = new(character);
            _currentStatsCharacters[character.ReferenceCharacter] = fightData;

            ICharacterRenderer characterRenderer = _characterService.GetWorldCharacter(character.ReferenceCharacter);
            IFightComponent fightComponent = characterRenderer.AddComponent<FightComponent>();
            fightComponent.AddComponents();
            fightComponent.HealthComponent.SetData(character.Health);
            fightComponent.ManaComponent.SetData(character.Mana);
            _fightComponents.Add(character.ReferenceCharacter, fightComponent);
        }

        private void ShowCharacter(Character character)
        {
            _characterService.ShowCharacter(character);
        }

        private void HideCharacter(Character character)
        {
            _characterService.HideCharacter(character);
        }

        private void ClearupFightComponents()
        {
            foreach (var component in _fightComponents)
            {
                try
                {
                    FightComponent fightComponent = component.Value as FightComponent;
                    Object.Destroy(fightComponent);
                }
                catch
                {
                    continue;
                }
            }

            _fightComponents.Clear();
            _fightComponents = null;
        }

        private void HideCharacters()
        {
            foreach (var fightComponent in _fightComponents)
            {
                Character character = fightComponent.Key;
                HideCharacter(character);
            }
        }
    }
}