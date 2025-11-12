using System;
using System.Collections.Generic;

namespace SNEngine.SaveSystem.Models
{
    [Serializable]
    public class SaveData
    {
        public DateTime DateSave { get; set; }
        public string DialogueGUID { get; set; }
        public Dictionary<string, object> Varitables { get; set; }
        public Dictionary<string, object> GlobalVaritables { get; set; }
        public Dictionary<string, object> NodesData { get; set; }
        public string CurrentNode { get; set; }

    }
}
