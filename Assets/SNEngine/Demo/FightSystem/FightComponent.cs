using CoreGame.FightSystem.HealthSystem;
using CoreGame.FightSystem.ManaSystem;
using UnityEngine;

namespace CoreGame.FightSystem
{
    public class FightComponent : MonoBehaviour, IFightComponent
    {
        private HealthComponent _healthComponent;
        private ManaComponent _manaComponent;

        public IHealthComponent HealthComponent => _healthComponent;
        public IManaComponent ManaComponent => _manaComponent;

        public void AddComponents()
        {
            _healthComponent = gameObject.AddComponent<HealthComponent>();
            _manaComponent = gameObject.AddComponent<ManaComponent>();
        }
    }
}