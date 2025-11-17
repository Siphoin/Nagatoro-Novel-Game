using UnityEditor;

namespace SNEngine.Editor
{
    public static class SNEngineEditorSettings
    {
        private const string GUID_KEY = "SNEngine.ShowNodeGuid";

        public static bool ShowNodeGuidInInspector
        {
            get => EditorPrefs.GetBool(GUID_KEY, false);
            set => EditorPrefs.SetBool(GUID_KEY, value);
        }
    }

}