using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreGame.FightSystem.Models
{
    [Serializable]
    public class CharacterFightData
    {
        public string GUID {  get; set; }
        public float CurrentHealth { get; set; }
        public float CurrentMana { get; set; }
        public CharacterFightData ()
        {

        }
        public CharacterFightData (FightCharacter fightCharacter)
        {
            GUID = fightCharacter.ReferenceCharacter.GUID;
            CurrentHealth = fightCharacter.Health;
            CurrentMana = fightCharacter.Mana;
        }
    }
}
