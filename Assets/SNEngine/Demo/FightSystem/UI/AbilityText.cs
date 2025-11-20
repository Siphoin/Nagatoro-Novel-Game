using CoreGame.FightSystem.Abilities;
using System;
using UnityEngine.EventSystems;
using UnityEngine;
using SNEngine.Localization;
using SNEngine;

namespace CoreGame.FightSystem.UI
{
    public class AbilityText : ClickableText
    {
        private const string ABILITY_DESC_KEY_PREFIX = "ability_";
        private const string ABILITY_DESC_KEY_SUFFIX = "_name";

        public event Action<ScriptableAbility> OnHover;
        public event Action<ScriptableAbility> OnExitHover;
        [SerializeField] private UILocalizationText _localizeComponent;

        public ScriptableAbility Ability { get; private set; }

        public void SetAbility(ScriptableAbility ability)
        {
            Ability = ability;
            _localizeComponent.ChangeKey(
                $"{ABILITY_DESC_KEY_PREFIX}{ability.GUID}{ABILITY_DESC_KEY_SUFFIX}"
            );

            if (_localizeComponent.NotCanTranslite)
            {
                Component.text = ability.NameAbility;
            }
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            OnHover?.Invoke(Ability);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            OnExitHover?.Invoke(Ability);
        }

        private void OnEnable()
        {

        }
    }
}