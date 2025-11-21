using CoreGame.FightSystem;
using CoreGame.FightSystem.Abilities;
using CoreGame.FightSystem.Models;
using FightSystem.Abilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CoreGame.FightSystem.AI
{
    [CreateAssetMenu(menuName = "CoreGame/Fight System/AI/IntermediateBoss")]
    public class IntermediateBossAI : ScriptableAI
    {
        [SerializeField, Range(0f, 1f)] private float _lowHPThreshold = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _healThresholdLowHP = 0.3f;
        [SerializeField] private int _signatureSkillCost = 3;
        [SerializeField] private AbilityType _signatureSkillType = AbilityType.Attack;
        [SerializeField] private float _attackWeightHighHP = 70f;
        [SerializeField] private float _attackWeightLowHP = 40f;
        [SerializeField] private float _guardWeightLowHP = 40f;

        public override AIDecision DecideAction(
            IFightComponent selfComponent,
            IFightComponent targetComponent,
            FightCharacter selfCharacter,
            IReadOnlyList<AbilityEntity> availableAbilities,
            float currentEnergy)
        {
            float selfHealthRatio = selfComponent.HealthComponent.CurrentHealth / selfComponent.HealthComponent.MaxHealth;
            float targetHealth = targetComponent.HealthComponent.CurrentHealth;
            float selfDamage = selfCharacter.Damage;

            if (targetHealth <= selfDamage)
            {
                return AIDecision.Simple(PlayerAction.Attack);
            }

            if (selfHealthRatio <= _lowHPThreshold)
            {
                if (selfHealthRatio <= _healThresholdLowHP)
                {
                    var healAbility = availableAbilities.FirstOrDefault(e =>
                        e.ReferenceAbility.GetAbilityType() == AbilityType.Heal &&
                        e.CurrentCooldown == 0 &&
                        e.ReferenceAbility.Cost <= currentEnergy);

                    if (healAbility != null)
                    {
                        return new AIDecision(PlayerAction.UseSkill, healAbility.ReferenceAbility);
                    }
                }

                return LowHPDecision(availableAbilities, currentEnergy);
            }
            else
            {
                var signatureSkill = availableAbilities.FirstOrDefault(e =>
                    e.ReferenceAbility.GetAbilityType() == _signatureSkillType &&
                    e.ReferenceAbility.Cost == _signatureSkillCost &&
                    e.CurrentCooldown == 0 &&
                    e.ReferenceAbility.Cost <= currentEnergy);

                if (signatureSkill != null)
                {
                    return new AIDecision(PlayerAction.UseSkill, signatureSkill.ReferenceAbility);
                }

                return HighHPDecision(availableAbilities, currentEnergy);
            }
        }

        private AIDecision HighHPDecision(IReadOnlyList<AbilityEntity> availableAbilities, float currentEnergy)
        {
            float totalWeight = _attackWeightHighHP + 10f;

            float roll = Random.Range(0f, totalWeight);

            if (roll < _attackWeightHighHP)
            {
                return AIDecision.Simple(PlayerAction.Attack);
            }
            else
            {
                return AIDecision.Simple(PlayerAction.Wait);
            }
        }

        private AIDecision LowHPDecision(IReadOnlyList<AbilityEntity> availableAbilities, float currentEnergy)
        {
            float totalWeight = _attackWeightLowHP + _guardWeightLowHP + 10f;

            float roll = Random.Range(0f, totalWeight);

            if (roll < _attackWeightLowHP)
            {
                return AIDecision.Simple(PlayerAction.Attack);
            }
            else if (roll < _attackWeightLowHP + _guardWeightLowHP)
            {
                return AIDecision.Simple(PlayerAction.Guard);
            }
            else
            {
                return AIDecision.Simple(PlayerAction.Wait);
            }
        }
    }
}