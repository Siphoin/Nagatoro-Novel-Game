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

        public Character ReferenceCharacter => _referenceCharacter;
        public float Damage => _damage;
        public float Health => _health;
        public float Mana => _mana;
    }
}
