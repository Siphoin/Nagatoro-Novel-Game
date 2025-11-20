using System;
using System.Collections;
using UnityEngine;
using CoreGame.FightSystem;
using DG.Tweening;
using SNEngine.Polling;
using System.Linq;

namespace CoreGame.FightSystem.UI
{
    public class FightWindow : MonoBehaviour, IFightWindow
    {
        [Header("UI Components")]
        [SerializeField] private FillSlider _healthPlayer;
        [SerializeField] private FillSlider _healthEnemy;
        [SerializeField] private ClickableText _attackButton;
        [SerializeField] private ClickableText _waitButton;
        [SerializeField] private ClickableText _guardButton;
        [SerializeField] private ClickableText _skillButton;
        [SerializeField] private RectTransform _panelAction;
        [SerializeField] private EnergyFill _energyFillPrefab;
        [SerializeField] private RectTransform _playerEnergyContainer;
        [SerializeField] private RectTransform _enemyEnergyContainer;
        [SerializeField] private RectTransform _panelListActions;
        [SerializeField] private AbilityWindow _abilityWindow;

        [Header("Health Bar Animation Settings")]
        [SerializeField, Min(0)] private float _durationChangeHealth = 0.3f;
        [SerializeField] private Ease _easeHealthAnimation = Ease.InBounce;

        public const float ANIMATION_DURATION_HIDE = 0.5f;
        [SerializeField] private Ease _easeShowAnimation = Ease.OutBack;

        [SerializeField] private float _panelActionOffsetY = -500f;

        [SerializeField] private float _healthBarOffsetY = 300f;

        public event Action<PlayerAction> OnTurnExecuted;

        private Vector2 _initialPanelActionPosition;
        private Vector2 _initialHealthPlayerPosition;
        private Vector2 _initialHealthEnemyPosition;
        private Vector2 _initialPlayerEnergyPosition;
        private Vector2 _initialEnemyEnergyPosition;

        private RectTransform _playerHealthParentRT;
        private RectTransform _enemyHealthParentRT;

        private PoolMono<EnergyFill> _poolEnergyFillsPlayer;
        private PoolMono<EnergyFill> _poolEnergyFillsEnemy;

        private void Awake()
        {
            _playerHealthParentRT = _healthPlayer.transform.parent.GetComponent<RectTransform>();
            _enemyHealthParentRT = _healthEnemy.transform.parent.GetComponent<RectTransform>();

            _initialPanelActionPosition = _panelAction.anchoredPosition;
            _initialHealthPlayerPosition = _playerHealthParentRT.anchoredPosition;
            _initialHealthEnemyPosition = _enemyHealthParentRT.anchoredPosition;
            _initialPlayerEnergyPosition = _playerEnergyContainer.anchoredPosition;
            _initialEnemyEnergyPosition = _enemyEnergyContainer.anchoredPosition;

            _attackButton.AddListener(() => OnClickButtonAction(PlayerAction.Attack));
            _guardButton.AddListener(() => OnClickButtonAction(PlayerAction.Guard));
            _waitButton.AddListener(() => OnClickButtonAction(PlayerAction.Wait));

            _skillButton.AddListener(OnClickSkillButton);
            CreatePoolEnergyFills(ref _poolEnergyFillsPlayer, _playerEnergyContainer);
            CreatePoolEnergyFills(ref _poolEnergyFillsEnemy, _enemyEnergyContainer);

        }
        private void CreatePoolEnergyFills(ref PoolMono<EnergyFill> poolEnergyFills, RectTransform container)
        {
            poolEnergyFills = new(_energyFillPrefab, container, 10, true);

        }

        private void ShowEnergyPoints(PoolMono<EnergyFill> pool, FightCharacter fightCharacter)
        {
            for (int i = 0; i < fightCharacter.EnergyPoint; i++)
            {
                var fill = pool.Objects.ElementAt(i);
                fill.gameObject.SetActive(true);
                fill.SetStateFull();
            }
        }
        private void OnClickButtonAction(PlayerAction action)
        {
            OnTurnExecuted?.Invoke(action);
        }

        private void OnClickSkillButton()
        {
            _panelListActions.gameObject.SetActive(false);
            _abilityWindow.gameObject.SetActive(true);
        }

        public void ResetState()
        {
        }

        private Tweener AnimateUIElement(RectTransform rectTransform, Vector2 endPosition)
        {
            rectTransform.DOKill();
            return rectTransform.DOAnchorPos(endPosition, ANIMATION_DURATION_HIDE) // Используем константу
                                .SetEase(_easeShowAnimation);
        }

        public void Show()
        {
            _panelAction.gameObject.SetActive(true);
            gameObject.SetActive(true);

            Sequence showSequence = DOTween.Sequence();
            showSequence.SetLink(gameObject);

            _panelAction.anchoredPosition = _initialPanelActionPosition + new Vector2(0, _panelActionOffsetY);
            _playerHealthParentRT.anchoredPosition = _initialHealthPlayerPosition + new Vector2(0, _healthBarOffsetY);
            _enemyHealthParentRT.anchoredPosition = _initialHealthEnemyPosition + new Vector2(0, _healthBarOffsetY);
            _playerEnergyContainer.anchoredPosition = _initialPlayerEnergyPosition + new Vector2(0, _healthBarOffsetY);
            _enemyEnergyContainer.anchoredPosition = _initialEnemyEnergyPosition + new Vector2(0, _healthBarOffsetY);

            Tweener panelTween = AnimateUIElement(_panelAction, _initialPanelActionPosition);
            showSequence.Append(panelTween);

            showSequence.Join(AnimateUIElement(_playerHealthParentRT, _initialHealthPlayerPosition));
            showSequence.Join(AnimateUIElement(_enemyHealthParentRT, _initialHealthEnemyPosition));
            showSequence.Join(AnimateUIElement(_playerEnergyContainer, _initialPlayerEnergyPosition));
            showSequence.Join(AnimateUIElement(_enemyEnergyContainer, _initialEnemyEnergyPosition));

            _abilityWindow.gameObject.SetActive(false);
        }

        public void Hide()
        {
            Sequence hideSequence = DOTween.Sequence();
            hideSequence.SetLink(gameObject);

            Tweener panelTween = AnimateUIElement(_panelAction, _initialPanelActionPosition + new Vector2(0, _panelActionOffsetY));
            hideSequence.Append(panelTween);

            hideSequence.Join(AnimateUIElement(_playerHealthParentRT, _initialHealthPlayerPosition + new Vector2(0, _healthBarOffsetY)));
            hideSequence.Join(AnimateUIElement(_enemyHealthParentRT, _initialHealthEnemyPosition + new Vector2(0, _healthBarOffsetY)));
            hideSequence.Join(AnimateUIElement(_playerEnergyContainer, _initialPlayerEnergyPosition + new Vector2(0, _healthBarOffsetY)));
            hideSequence.Join(AnimateUIElement(_enemyEnergyContainer, _initialEnemyEnergyPosition + new Vector2(0, _healthBarOffsetY)));

            hideSequence.OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        }

        public void HidePanelAction()
        {
            _panelAction.gameObject.SetActive(false);
        }

        public void ShowPanelAction()
        {
            _panelAction.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            _panelAction.DOKill();
            _playerHealthParentRT.DOKill();
            _enemyHealthParentRT.DOKill();
            _playerEnergyContainer.DOKill();
            _enemyEnergyContainer.DOKill();
        }

        public void SetData(IFightComponent fightComponentPlayer, IFightComponent fightComponentEnemy, FightCharacter playerData, FightCharacter enemyData)
        {
            fightComponentEnemy.HealthComponent.OnHealthChanged += OnHealthChangedEnemy;
            fightComponentPlayer.HealthComponent.OnHealthChanged += OnHealthChangedPlayer;
            _healthEnemy.MaxValue = fightComponentEnemy.HealthComponent.MaxHealth;
            _healthEnemy.SetValueSmoothly(fightComponentEnemy.HealthComponent.CurrentHealth, _durationChangeHealth, _easeHealthAnimation);
            _healthPlayer.SetValueSmoothly(fightComponentPlayer.HealthComponent.CurrentHealth, _durationChangeHealth, _easeHealthAnimation);
            ShowEnergyPoints(_poolEnergyFillsEnemy, enemyData);
            ShowEnergyPoints(_poolEnergyFillsPlayer, playerData);
        }

        private void OnHealthChangedPlayer(float current, float max)
        {
            _healthPlayer.SetValueSmoothly(current, _durationChangeHealth, _easeHealthAnimation);
        }

        private void OnHealthChangedEnemy(float current, float max)
        {
            _healthEnemy.SetValueSmoothly(current, _durationChangeHealth, _easeHealthAnimation);
        }
    }
}