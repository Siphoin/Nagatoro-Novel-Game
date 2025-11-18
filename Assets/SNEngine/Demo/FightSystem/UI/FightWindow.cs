using System;
using System.Collections;
using UnityEngine;
using CoreGame.FightSystem;
using DG.Tweening;

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

        [Header("Health Bar Animation Settings")]
        [SerializeField, Min(0)] private float _durationChangeHealth = 0.3f;
        [SerializeField] private Ease _easeHealthAnimation = Ease.InBounce;

        [Header("Show Animation Settings")]
        [SerializeField, Min(0)] private float _durationShow = 0.5f;
        [SerializeField] private Ease _easeShowAnimation = Ease.OutBack;

        [Tooltip("Смещение по Y для панели действий (снизу)")]
        [SerializeField] private float _panelActionOffsetY = -500f;

        [Tooltip("Смещение по Y для полосок здоровья (сверху)")]
        [SerializeField] private float _healthBarOffsetY = 300f;

        public event Action<PlayerAction> OnTurnExecuted;

        private Vector2 _initialPanelActionPosition;
        private Vector2 _initialHealthPlayerPosition;
        private Vector2 _initialHealthEnemyPosition;

        private RectTransform _playerHealthParentRT;
        private RectTransform _enemyHealthParentRT;

        private void Awake()
        {
            _playerHealthParentRT = _healthPlayer.transform.parent.GetComponent<RectTransform>();
            _enemyHealthParentRT = _healthEnemy.transform.parent.GetComponent<RectTransform>();

            _initialPanelActionPosition = _panelAction.anchoredPosition;
            _initialHealthPlayerPosition = _playerHealthParentRT.anchoredPosition;
            _initialHealthEnemyPosition = _enemyHealthParentRT.anchoredPosition;

            _attackButton.AddListener(() => OnClickButtonAction(PlayerAction.Attack));
            _guardButton.AddListener(() => OnClickButtonAction(PlayerAction.Guard));
            _waitButton.AddListener(() => OnClickButtonAction(PlayerAction.Wait));

            _skillButton.AddListener(OnClickSkillButton);
        }

        private void OnClickButtonAction(PlayerAction action)
        {
            OnTurnExecuted?.Invoke(action);
        }

        private void OnClickSkillButton()
        {

        }

        public void ResetState()
        {
        }

        public void Show()
        {
            _panelAction.gameObject.SetActive(true);
            gameObject.SetActive(true);

            RectTransform panelRT = _panelAction.GetComponent<RectTransform>();

            panelRT.anchoredPosition = _initialPanelActionPosition + new Vector2(0, _panelActionOffsetY);

            panelRT.DOAnchorPos(_initialPanelActionPosition, _durationShow)
                    .SetEase(_easeShowAnimation)
                    .SetLink(gameObject);


            _playerHealthParentRT.anchoredPosition = _initialHealthPlayerPosition + new Vector2(0, _healthBarOffsetY);
            _playerHealthParentRT.DOAnchorPos(_initialHealthPlayerPosition, _durationShow)
                            .SetEase(_easeShowAnimation)
                            .SetLink(gameObject);

            _enemyHealthParentRT.anchoredPosition = _initialHealthEnemyPosition + new Vector2(0, _healthBarOffsetY);
            _enemyHealthParentRT.DOAnchorPos(_initialHealthEnemyPosition, _durationShow)
                            .SetEase(_easeShowAnimation)
                            .SetLink(gameObject);
        }

        public void Hide()
        {
            _panelAction.GetComponent<RectTransform>().DOKill();
            _playerHealthParentRT.DOKill();
            _enemyHealthParentRT.DOKill();

            gameObject.SetActive(false);
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
            _panelAction.GetComponent<RectTransform>().DOKill();
            _playerHealthParentRT.DOKill();
            _enemyHealthParentRT.DOKill();
        }

        public void SetData(IFightComponent fightComponentPlayer, IFightComponent fightComponentEnemy)
        {
            fightComponentEnemy.HealthComponent.OnHealthChanged += OnHealthChangedEnemy;
            fightComponentPlayer.HealthComponent.OnHealthChanged += OnHealthChangedPlayer;
            _healthEnemy.MaxValue = fightComponentEnemy.HealthComponent.MaxHealth;
            _healthEnemy.SetValueSmoothly(fightComponentEnemy.HealthComponent.CurrentHealth, _durationChangeHealth, _easeHealthAnimation);
            _healthPlayer.SetValueSmoothly(fightComponentPlayer.HealthComponent.CurrentHealth, _durationChangeHealth, _easeHealthAnimation);
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