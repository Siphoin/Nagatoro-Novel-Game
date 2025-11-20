using CoreGame.Services;
using SNEngine;
using SNEngine.Polling;
using System.Collections;
using UnityEngine;

namespace CoreGame.FightSystem.UI
{
    public class AbilityWindow : MonoBehaviour
    {
        [SerializeField] private AbilityText _prefab;
        [SerializeField] private RectTransform _containerAbility;
        private PoolMono<AbilityText> _pool;

        private void Awake()
        {
            _pool = new(_prefab, _containerAbility, 5, true);
        }

        private void OnEnable()
        {
            ShowAbilites();
        }

        private void ShowAbilites ()
        {
            for (int i = 0; i < _containerAbility.childCount; i++)
            {
                var child = _containerAbility.GetChild(i);
                child.gameObject.SetActive(false);
            }

            var fightService = NovelGame.Instance.GetService<FightService>();
            if (fightService.Player is null)
            {
                return;
            }
            var abilites = fightService.PlayerData.Abilities;

            foreach (var ability in abilites)
            {
                var abilityView = _pool.GetFreeElement();
                abilityView.SetAbility(ability);
                abilityView.gameObject.SetActive(true);
            }
        }

    }
}