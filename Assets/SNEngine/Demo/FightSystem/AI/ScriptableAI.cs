using SNEngine;

namespace CoreGame.FightSystem.AI
{
    public abstract class ScriptableAI : ScriptableObjectIdentity
    {

        public abstract PlayerAction DecideAction(
            IFightComponent selfComponent,
            IFightComponent targetComponent,
            FightCharacter selfCharacter);
    }
}