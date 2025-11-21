using CoreGame.FightSystem;
using CoreGame.FightSystem.Models;
using UnityEngine;

namespace CoreGame.FightSystem.AI
{
    [CreateAssetMenu(menuName = "CoreGame/Fight System/AI/NagatoroSuccubus")]
    public class NagatoroSuccubusAI : ScriptableAI
    {
        public override PlayerAction DecideAction(
            IFightComponent selfComponent,
            IFightComponent targetComponent,
            FightCharacter selfCharacter)
        {
            float selfHealthRatio = selfComponent.HealthComponent.CurrentHealth / selfComponent.HealthComponent.MaxHealth;
            float targetHealthRatio = targetComponent.HealthComponent.CurrentHealth / targetComponent.HealthComponent.MaxHealth;

            float selfDamage = selfCharacter.Damage;

            if (selfHealthRatio <= 0.3f)
            {
                return PlayerAction.Guard;
            }

            if (targetHealthRatio * targetComponent.HealthComponent.MaxHealth <= selfDamage)
            {
                return PlayerAction.Attack;
            }

            int choice = Random.Range(0, 3);

            if (choice == 0)
            {
                return PlayerAction.Attack;
            }
            else if (choice == 1)
            {
                return PlayerAction.Guard;
            }
            else
            {
                return PlayerAction.Wait;
            }
        }
    }
}