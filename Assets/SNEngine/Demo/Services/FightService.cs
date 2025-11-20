using CoreGame.FightSystem;
using CoreGame.FightSystem.HealthSystem;
using CoreGame.FightSystem.Models;
using CoreGame.FightSystem.UI;
using SNEngine;
using SNEngine.CharacterSystem;
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
        #region Fields
        private CharacterService _characterService;
        private BackgroundService _backgroundService;
        private Dictionary<Character, IFightComponent> _fightComponents;
        private IFightWindow _fightWindow;
        private IFightComponent _player;
        private IFightComponent _enemy;
        private FightCharacter _playerCharacter;
        private FightCharacter _enemyCharacter;
        private AIFighter _aiFighter;
        private PoolMono<HealText> _poolHealText;
        private PoolMono<DamageText> _poolDamageText;
        private PoolMono<CriticalDamageText> _poolCriticalDamageText;
        private Dictionary<Character, float> _healthBeforeLastAction;

        private Dictionary<Character, bool> _wasLastHitCritical;

        private const string FIGHT_WINDOW_VANILLA_PATH = "UI/FightWindow";
        private const float ENEMY_TURN_DELAY = 0.5f;
        private FightTurnOwner _fightTurnOwner = FightTurnOwner.Player;
        private bool _isPlayerGuarding;
        private bool _isEnemyGuarding;
        [SerializeField] private float _hitShakeDuration = 0.3f;
        [SerializeField] private float _hitShakeStrength = 10f;
        [SerializeField] private int _hitShakeVibrato = 10;
        [SerializeField] private Color _hitColor = new Color(1f, 0f, 0f, 1f);
        [SerializeField] private float _hitColorDuration = 0.1f;
        [SerializeField] private Ease _hitColorEase = Ease.Linear;
        #endregion

        #region Properties
        public IFightComponent Player => _player;
        public IFightComponent Enemy => _enemy;

        public FightCharacter PlayerData { get; private set; }
        public FightCharacter EnemyData { get; private set; }

        public event Action<FightResult> OnFightEnded;
        #endregion

        #region Service Lifecycle
        public override void Initialize()
        {
            _characterService = NovelGame.Instance.GetService<CharacterService>();
            _backgroundService = NovelGame.Instance.GetService<BackgroundService>();
            var ui = NovelGame.Instance.GetService<UIService>();
            var input = ResourceLoader.LoadCustomOrVanilla<FightWindow>(FIGHT_WINDOW_VANILLA_PATH);
            var healTextPrefab = ResourceLoader.LoadCustomOrVanilla<HealText>("UI/HealText");
            var damageTextPrefab = ResourceLoader.LoadCustomOrVanilla<DamageText>("UI/DamageText");
            var criticalDamageTextPrefab = ResourceLoader.LoadCustomOrVanilla<CriticalDamageText>("UI/CriticalDamageText");
            var containerTexts = new GameObject("Floating Texts");
            containerTexts.AddComponent<RectTransform>();

            if (input == null) return;
            var prefab = Object.Instantiate(input);
            prefab.name = input.name;
            _fightWindow = prefab;
            ui.AddElementToUIContainer(prefab.gameObject);
            prefab.gameObject.SetActive(false);
            containerTexts.transform.SetParent(prefab.transform, false);
            _poolHealText = new(healTextPrefab, containerTexts.transform, 9, true);
            _poolDamageText = new(damageTextPrefab, containerTexts.transform, 9, true);
            _poolCriticalDamageText = new(criticalDamageTextPrefab, containerTexts.transform, 9, true);
        }

        public override void ResetState()
        {
            if (_fightWindow != null) _fightWindow.OnTurnExecuted -= OnPlayerTurnExecuted;
            UnsubscribeFromHealthEvents();
            HideCharacters();
            ClearupFightComponents();
            _fightWindow.ResetState();
            _aiFighter = null;
            _player = null;
            _enemy = null;
            PlayerData = null;
            EnemyData = null;
            _isPlayerGuarding = false;
            _isEnemyGuarding = false;
            _healthBeforeLastAction = null;
            _wasLastHitCritical = null;
        }
        #endregion

        #region Fight Setup
        public void TurnFight(FightCharacter playerCharacter, FightCharacter enemyCharacter)
        {
            _playerCharacter = playerCharacter;
            _enemyCharacter = enemyCharacter;
            _fightComponents = new();
            _healthBeforeLastAction = new();
            _wasLastHitCritical = new()
            {
                { playerCharacter.ReferenceCharacter, false },
                { enemyCharacter.ReferenceCharacter, false }
            };
            SetupCharacterForFight(playerCharacter);
            SetupCharacterForFight(enemyCharacter);
            _player = _fightComponents[playerCharacter.ReferenceCharacter];
            _enemy = _fightComponents[enemyCharacter.ReferenceCharacter];
            EnemyData = enemyCharacter;
            PlayerData = playerCharacter;
            _fightWindow.SetData(_player, _enemy, playerCharacter, enemyCharacter);
            _aiFighter = new AIFighter(_enemyCharacter, _enemy, _playerCharacter, _player);
            _isPlayerGuarding = false;
            _isEnemyGuarding = false;
            _fightWindow.OnTurnExecuted += OnPlayerTurnExecuted;
            SubscribeToHealthEvents();
            _fightWindow.Show();
            _fightTurnOwner = FightTurnOwner.Player;
        }

        private void SetupCharacterForFight(FightCharacter character)
        {
            _characterService.ShowCharacter(character.ReferenceCharacter);
            ICharacterRenderer characterRenderer = _characterService.GetWorldCharacter(character.ReferenceCharacter);
            IFightComponent fightComponent = characterRenderer.AddComponent<FightComponent>();
            fightComponent.AddComponents();
            fightComponent.HealthComponent.SetData(character.Health);
            fightComponent.ManaComponent.SetData(character.Mana);
            _fightComponents.Add(character.ReferenceCharacter, fightComponent);
        }

        private void ClearupFightComponents()
        {
            if (_fightComponents != null)
            {
                foreach (var component in _fightComponents.Values)
                {
                    if (component is FightComponent fightComponent)
                    {
                        Object.Destroy(fightComponent);
                    }
                }
                _fightComponents.Clear();
            }
           
        }

        private void HideCharacters()
        {
            if (_fightComponents != null)
            {
                foreach (var character in _fightComponents.Keys)
                {
                    _characterService.HideCharacter(character);
                }
            }
        }
        #endregion

        #region Turn Execution
        private async void OnPlayerTurnExecuted(PlayerAction action)
        {
            if (_fightTurnOwner != FightTurnOwner.Player) return;
            _fightWindow.HidePanelAction();
            SaveHealthBeforeAction();
            await HandlePlayerAction(action);
            if (CheckFightEndConditions()) return;
            _fightTurnOwner = FightTurnOwner.Enemy;
            ExecuteEnemyTurn().Forget();
        }

        private async UniTask HandlePlayerAction(PlayerAction action)
        {
            _isPlayerGuarding = false;
            switch (action)
            {
                case PlayerAction.Attack:
                    await HandleAttackAction(_enemy, _playerCharacter.Damage, _enemyCharacter, _playerCharacter);
                    break;
                case PlayerAction.Guard:
                    _isPlayerGuarding = true;
                    break;
            }
        }

        private async UniTask ExecuteEnemyTurn()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(ENEMY_TURN_DELAY), DelayType.DeltaTime, PlayerLoopTiming.Update, CancellationToken.None);
            SaveHealthBeforeAction();
            PlayerAction enemyAction = _aiFighter.DecideAction();
            await HandleEnemyAction(enemyAction);
            if (CheckFightEndConditions()) return;
            await UniTask.Delay(TimeSpan.FromSeconds(ENEMY_TURN_DELAY), DelayType.DeltaTime, PlayerLoopTiming.Update, CancellationToken.None);
            _fightTurnOwner = FightTurnOwner.Player;
            _fightWindow.ShowPanelAction();
        }

        private async UniTask HandleEnemyAction(PlayerAction action)
        {
            _isEnemyGuarding = false;
            switch (action)
            {
                case PlayerAction.Attack:
                    await HandleAttackAction(_player, _enemyCharacter.Damage, _playerCharacter, _enemyCharacter);
                    break;
                case PlayerAction.Guard:
                    _isEnemyGuarding = true;
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
                if (targetCharacter == _playerCharacter) _isPlayerGuarding = false;
                else _isEnemyGuarding = false;
            }

            _wasLastHitCritical[targetCharacter.ReferenceCharacter] = isCritical;

            targetComponent.HealthComponent.TakeDamage(finalDamage);


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
        #endregion

        #region Health Events & UI
        private void SaveHealthBeforeAction()
        {
            _healthBeforeLastAction[_playerCharacter.ReferenceCharacter] = _player.HealthComponent.CurrentHealth;
            _healthBeforeLastAction[_enemyCharacter.ReferenceCharacter] = _enemy.HealthComponent.CurrentHealth;
        }

        private void SubscribeToHealthEvents()
        {
            if (_player != null) _player.HealthComponent.OnHealthChanged += OnPlayerHealthChanged;
            if (_enemy != null) _enemy.HealthComponent.OnHealthChanged += OnEnemyHealthChanged;
        }

        private void UnsubscribeFromHealthEvents()
        {
            if (_player?.HealthComponent != null) _player.HealthComponent.OnHealthChanged -= OnPlayerHealthChanged;
            if (_enemy?.HealthComponent != null) _enemy.HealthComponent.OnHealthChanged -= OnEnemyHealthChanged;
        }

        private void OnPlayerHealthChanged(float currentHealth, float maxHealth) => HandleHealthChange(_playerCharacter.ReferenceCharacter, currentHealth);

        private void OnEnemyHealthChanged(float currentHealth, float maxHealth) => HandleHealthChange(_enemyCharacter.ReferenceCharacter, currentHealth);

        private void HandleHealthChange(Character character, float currentHealth)
        {
            float healthBefore = _healthBeforeLastAction.GetValueOrDefault(character, currentHealth);
            float delta = currentHealth - healthBefore;

            if (Mathf.Abs(delta) < 0.01f) return;

            Vector3 worldPosition = _characterService.GetCharacterWorldPosition(character);
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);

            if (delta > 0)
            {
                _poolHealText.GetFreeElement().Show(delta, screenPosition).Forget();
            }
            else
            {
                float damage = Mathf.Abs(delta);
                if (_wasLastHitCritical.GetValueOrDefault(character, false))
                {
                    _poolCriticalDamageText.GetFreeElement().Show(damage, screenPosition).Forget();
                    _wasLastHitCritical[character] = false;
                }
                else
                {
                    _poolDamageText.GetFreeElement().Show(damage, screenPosition).Forget();
                }
            }

            _healthBeforeLastAction[character] = currentHealth;
        }

        private async UniTask AnimateCharacterHit(Character character)
        {
            await _characterService.SetColorCharacter(character, _hitColor, _hitColorDuration, _hitColorEase);
            await _characterService.SetColorCharacter(character, Color.white, _hitColorDuration, _hitColorEase);
        }

        private async UniTask AnimateBackgroundHit()
        {
            await _backgroundService.SetColor(_hitColor, _hitColorDuration, _hitColorEase);
            await _backgroundService.SetColor(Color.white, _hitColorDuration, _hitColorEase);
        }
        #endregion

        #region Fight End
        private bool CheckFightEndConditions()
        {
            bool playerDead = _player.HealthComponent.CurrentHealth <= 0;
            bool enemyDead = _enemy.HealthComponent.CurrentHealth <= 0;

            if (playerDead && enemyDead) { EndFight(FightResult.Tie); return true; }
            if (playerDead) { EndFight(FightResult.Defeat); return true; }
            if (enemyDead) { EndFight(FightResult.Victory); return true; }

            return false;
        }

        private void EndFight(FightResult result)
        {
            _fightWindow.OnTurnExecuted -= OnPlayerTurnExecuted;
            UnsubscribeFromHealthEvents();
            _fightWindow.Hide();
            HideCharacters();
            ClearupFightComponents();
            OnFightEnded?.Invoke(result);
        }
        #endregion
    }
}