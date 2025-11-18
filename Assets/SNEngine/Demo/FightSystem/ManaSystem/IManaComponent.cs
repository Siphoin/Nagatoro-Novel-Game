namespace CoreGame.FightSystem.ManaSystem
{
    public interface IManaComponent
    {
        public float CurrentMana { get; }
        public float MaxMana { get; }
        bool TrySpend(float cost);
        void Restore(float amount);
    }
}