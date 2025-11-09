using UnityEngine;

namespace SNEngine.Editor.Windows.Models
{
    [System.Serializable]
    public class YamlSyntaxStyle
    {
        public string CommentColor { get; set; } = "#6AC46A";
        public string KeyColor { get; set; } = "#C678DD";
        public string KeywordColor { get; set; } = "#E5C07B";
        public string StringColor { get; set; } = "#61AFEF";
        public string BackgroundColor { get; set; } = "#282A2C";

    }
}