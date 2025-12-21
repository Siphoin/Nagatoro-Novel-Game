using System;
using System.Collections.Generic;

namespace SNEngine.SaveSystem.Models
{
    [Serializable]
    public class SaveData
    {
        public DateTime DateSave { get; set; }
        public string DialogueGUID { get; set; }
        public Dictionary<string, object> Variables { get; set; }
        public Dictionary<string, object> GlobalVariables { get; set; }
        public Dictionary<string, object> NodesData { get; set; }
        public string CurrentNode { get; set; }

    }
}
