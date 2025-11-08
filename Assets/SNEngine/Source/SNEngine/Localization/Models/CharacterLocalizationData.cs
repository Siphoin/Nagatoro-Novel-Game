using System;

namespace SNEngine.Localization.Models
{
    [Serializable]
    public class CharacterLocalizationData
    {
        public string GUID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
