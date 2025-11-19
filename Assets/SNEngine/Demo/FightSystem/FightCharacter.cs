using SNEngine;
using SNEngine.CharacterSystem;
using UnityEngine;

namespace CoreGame.FightSystem
{
    [CreateAssetMenu(menuName = "CoreGame/Fight System/New Fight Character")]
    public class FightCharacter : ScriptableObjectIdentity
    {
        [SerializeField] private Character _referenceCharacter;
        [SerializeField, Min(1)] private float _damage = 1;
        [SerializeField, Min(1)] private float _health = 100;
        [SerializeField, Min(1)] private float _mana = 100;
        [SerializeField, Range(0, 1)] private float _guardReductionPercentage = 0.5f;
        [SerializeField, Range(0, 1)] private float _criticalHitChance = 0.1f;
        [SerializeField, Min(1)] private float _criticalHitMultiplier = 1.5f;
        [SerializeField, Min(1)] private int _energyPoint = 5;

        public Character ReferenceCharacter => _referenceCharacter;
        public float Damage => _damage;
        public float Health => _health;
        public float Mana => _mana;
        public float GuardReductionPercentage => _guardReductionPercentage;
        public float CriticalHitChance => _criticalHitChance;
        public float CriticalHitMultiplier => _criticalHitMultiplier;

        public int EnergyPoint => _energyPoint;
    }
}