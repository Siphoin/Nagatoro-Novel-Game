using CoreGame.FightSystem.HealthSystem;
using CoreGame.FightSystem.ManaSystem;

namespace CoreGame.FightSystem
{
    public interface IFightComponent
    {
        IHealthComponent HealthComponent { get; }
        IManaComponent ManaComponent { get; }
        void AddComponents();

    }
}