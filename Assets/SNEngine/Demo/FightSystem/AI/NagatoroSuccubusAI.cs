using CoreGame.FightSystem;
using CoreGame.FightSystem.Abilities;
using CoreGame.FightSystem.Models;
using FightSystem.Abilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CoreGame.FightSystem.AI
{

    [CreateAssetMenu(menuName = "CoreGame/Fight System/AI/NagatoroSuccubus")]
    public class NagatoroSuccubusAI : ScriptableAI
    {
        [SerializeField] private List<AIPatternPhase> _phases = new List<AIPatternPhase>();
        [SerializeField] private float _executeDamageMultiplier = 1.5f;

        public override AIDecision DecideAction(
            IFightComponent selfComponent,
            IFightComponent targetComponent,
            FightCharacter selfCharacter,
            IReadOnlyList<AbilityEntity> availableAbilities,
            float currentEnergy)
        {
            float selfHealthRatio = selfComponent.HealthComponent.CurrentHealth / selfComponent.HealthComponent.MaxHealth;
            float targetHealth = targetComponent.HealthComponent.CurrentHealth;
            float modifiedDamage = selfCharacter.Damage * GetCurrentPhase(selfHealthRatio).DamageMultiplier;
            AIPatternPhase currentPhase = GetCurrentPhase(selfHealthRatio);

            if (CheckExecute(targetHealth, modifiedDamage))
            {
                return AIDecision.Simple(PlayerAction.Attack);
            }

            AIDecision healDecision = CheckUrgentHeal(selfHealthRatio, currentPhase.UrgentHealThreshold, availableAbilities, currentEnergy);
            if (healDecision.Action != PlayerAction.Wait)
            {
                return healDecision;
            }

            if (CheckEnergyWait(currentPhase, availableAbilities, currentEnergy))
            {
                return AIDecision.Simple(PlayerAction.Wait);
            }

            return WeightedDecision(currentPhase, availableAbilities, currentEnergy);
        }

        private AIPatternPhase GetCurrentPhase(float selfHealthRatio)
        {
            foreach (var phase in _phases.OrderByDescending(p => p.HealthThreshold))
            {
                if (selfHealthRatio <= phase.HealthThreshold)
                {
                    return phase;
                }
            }
            return _phases.LastOrDefault() ?? new AIPatternPhase();
        }

        private bool CheckExecute(float targetHealth, float baseDamage)
        {
            return targetHealth <= baseDamage * _executeDamageMultiplier;
        }

        private AIDecision CheckUrgentHeal(float selfHealthRatio, float healThreshold, IReadOnlyList<AbilityEntity> availableAbilities, float currentEnergy)
        {
            if (selfHealthRatio <= healThreshold)
            {
                var healAbility = availableAbilities.FirstOrDefault(e =>
                    e.ReferenceAbility.GetAbilityType() == AbilityType.Heal &&
                    e.CurrentCooldown == 0 &&
                    e.ReferenceAbility.Cost <= currentEnergy);

                if (healAbility != null)
                {
                    return new AIDecision(PlayerAction.UseSkill, healAbility.ReferenceAbility);
                }

                return AIDecision.Simple(PlayerAction.Guard);
            }
            return AIDecision.Simple(PlayerAction.Wait);
        }

        private bool CheckEnergyWait(AIPatternPhase phase, IReadOnlyList<AbilityEntity> availableAbilities, float currentEnergy)
        {
            var highCostSkill = availableAbilities.FirstOrDefault(e =>
                (e.ReferenceAbility.GetAbilityType() == phase.PreferredSkillType || e.ReferenceAbility.GetAbilityType() == phase.SecondarySkillType) &&
                e.CurrentCooldown == 0 &&
                e.ReferenceAbility.Cost > currentEnergy &&
                e.ReferenceAbility.Cost >= phase.EnergyWaitCostMin);

            return highCostSkill != null;
        }

        private AIDecision WeightedDecision(AIPatternPhase phase, IReadOnlyList<AbilityEntity> availableAbilities, float currentEnergy)
        {
            var weights = phase.Weights;
            float totalWeight = weights.AttackWeight + weights.GuardWeight + weights.WaitWeight;
            float skillWeight = 0;

            var preferredSkill = availableAbilities.FirstOrDefault(e =>
                e.ReferenceAbility.GetAbilityType() == phase.PreferredSkillType &&
                e.CurrentCooldown == 0 &&
                e.ReferenceAbility.Cost <= currentEnergy);

            if (preferredSkill != null)
            {
                skillWeight = weights.SkillWeight;
                totalWeight += skillWeight;
            }

            float roll = Random.Range(0f, totalWeight);

            if (roll < weights.AttackWeight)
            {
                return AIDecision.Simple(PlayerAction.Attack);
            }
            else if (roll < weights.AttackWeight + weights.GuardWeight)
            {
                return AIDecision.Simple(PlayerAction.Guard);
            }
            else if (preferredSkill != null && roll < weights.AttackWeight + weights.GuardWeight + skillWeight)
            {
                return new AIDecision(PlayerAction.UseSkill, preferredSkill.ReferenceAbility);
            }
            else
            {
                return AIDecision.Simple(PlayerAction.Wait);
            }
        }
    }
}