#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace SiphoinUnityHelpers.Editor
{
    public class PrefabSelectorWindow : EditorWindow
    {
        private Action<GameObject> _onSelect;
        private string _searchQuery = "";
        private Vector2 _scrollPos;
        private List<string> _allPrefabPaths = new List<string>();

        public static void Open(Action<GameObject> onSelect)
        {
            var window = GetWindow<PrefabSelectorWindow>(true, "Prefab Selector", true);
            window._onSelect = onSelect;
            window.minSize = new Vector2(350, 450);
            window.RefreshCache();
            window.ShowAuxWindow();
        }

        private void RefreshCache()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            _allPrefabPaths = guids.Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !path.Contains("/Editor/"))
                .OrderBy(Path.GetFileName)
                .ToList();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawSearchBar();
            EditorGUILayout.Space(5);
            DrawPrefabList();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(2);
            EditorGUILayout.LabelField("Prefab Selector", EditorStyles.boldLabel);
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

        private void DrawPrefabList()
        {
            var filteredPaths = _allPrefabPaths
                .Where(p => string.IsNullOrEmpty(_searchQuery) || Path.GetFileName(p).IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            if (filteredPaths.Count == 0)
            {
                EditorGUILayout.HelpBox("No prefabs found matching search.", MessageType.Info);
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            foreach (var path in filteredPaths)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);

                string directoryPath = Path.GetDirectoryName(path);
                string relativePath = directoryPath.Replace("Assets/", "").Replace("Assets\\", "");
                if (string.IsNullOrEmpty(relativePath)) relativePath = "Root";

                float rowHeight = 48f;
                Rect rect = EditorGUILayout.BeginHorizontal(GUI.skin.button, GUILayout.Height(rowHeight));

                if (rect.Contains(Event.current.mousePosition))
                {
                    EditorGUI.DrawRect(rect, new Color(1, 1, 1, 0.05f));
                }

                GUILayout.Space(8);

                EditorGUILayout.BeginVertical(GUILayout.Width(32), GUILayout.Height(rowHeight));
                GUILayout.FlexibleSpace();

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Texture icon = AssetPreview.GetAssetPreview(prefab);
                if (icon == null) icon = EditorGUIUtility.IconContent("Prefab Icon").image;

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
                    _onSelect?.Invoke(prefab);
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