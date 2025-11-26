using SNEngine.Audio.Models;
using System;

namespace SNEngine.UserDataSystem.Models
{
    [Serializable]
    public class UserData
    {
        public string CurrentLanguage { get; set; } = string.Empty;
        public AudioData AudioData { get; set; } = new();
    }
}
