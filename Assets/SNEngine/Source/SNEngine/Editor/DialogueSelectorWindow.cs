#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using SNEngine.Graphs;

namespace SNEngine.Editor
{
    public class DialogueSelectorWindow : EditorWindow
    {
        private Action<DialogueGraph> _onSelect;
        private string _searchQuery = "";
        private Vector2 _scrollPos;
        private List<string> _dialoguePaths = new List<string>();

        public static void Open(Action<DialogueGraph> onSelect)
        {
            var window = GetWindow<DialogueSelectorWindow>(true, "Dialogue Selector", true);
            window._onSelect = onSelect;
            window.minSize = new Vector2(350, 450);
            window.RefreshCache();
            window.ShowAuxWindow();
        }

        private void RefreshCache()
        {
            string[] guids = AssetDatabase.FindAssets("t:DialogueGraph");
            _dialoguePaths = guids.Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(Path.GetFileName)
                .ToList();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawSearchBar();
            EditorGUILayout.Space(5);
            DrawDialogueList();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(2);
            EditorGUILayout.LabelField("Dialogue Selector", EditorStyles.boldLabel);
            GUILayout.Space(2);
            EditorGUILayout.EndVertical();
        }

        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            _searchQuery = EditorGUILayout.TextField(new GUIContent("", EditorGUIUtility.FindTexture("Search Icon")), _searchQuery, GUILayout.Height(20));
            if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20)))
            {
                _searchQuery = "";
            }
            if (GUILayout.Button(EditorGUIUtility.IconContent("Refresh"), GUILayout.Width(25), GUILayout.Height(20)))
            {
                RefreshCache();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDialogueList()
        {
            var filteredPaths = _dialoguePaths
                .Where(p => string.IsNullOrEmpty(_searchQuery) || Path.GetFileName(p).IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            foreach (var path in filteredPaths)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                string directoryPath = Path.GetDirectoryName(path);
                string relativePath = directoryPath.Replace("Assets/SNEngine/Source/SNEngine/Resources/", "")
                                                   .Replace("Assets/", "");

                float rowHeight = 48f;
                Rect rect = EditorGUILayout.BeginHorizontal(GUI.skin.button, GUILayout.Height(rowHeight));

                if (rect.Contains(Event.current.mousePosition))
                {
                    EditorGUI.DrawRect(rect, new Color(1, 1, 1, 0.05f));
                }

                GUILayout.Space(8);

                EditorGUILayout.BeginVertical(GUILayout.Width(32), GUILayout.Height(rowHeight));
                GUILayout.FlexibleSpace();

                Texture icon = AssetDatabase.GetCachedIcon(path);
                Rect iconRect = GUILayoutUtility.GetRect(26, 26);
                if (icon != null) GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();

                GUILayout.Space(4);

                EditorGUILayout.BeginVertical(GUILayout.Height(rowHeight));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(fileName, EditorStyles.boldLabel);

                GUIStyle pathStyle = new GUIStyle(EditorStyles.miniLabel);
                pathStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
                EditorGUILayout.LabelField(relativePath, pathStyle);

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(GUILayout.Width(75), GUILayout.Height(rowHeight));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Select", GUILayout.Height(26)))
                {
                    DialogueGraph dialogue = AssetDatabase.LoadAssetAtPath<DialogueGraph>(path);
                    _onSelect?.Invoke(dialogue);
                    Close();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();

                GUILayout.Space(4);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif