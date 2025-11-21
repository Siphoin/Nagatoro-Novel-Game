using System;

namespace CoreGame.FightSystem.AI
{
    [Serializable]
    public class AIEntity
    {
        public ScriptableAI ReferenceAI { get; private set; }

        public AIEntity(ScriptableAI referenceAI)
        {
            ReferenceAI = referenceAI;
        }

        public PlayerAction DecideAction(
            IFightComponent selfComponent,
            IFightComponent targetComponent,
            FightCharacter selfCharacter)
        {
            return ReferenceAI?.DecideAction(selfComponent, targetComponent, selfCharacter) ?? PlayerAction.Wait;
        }
    }
}