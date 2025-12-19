#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace SiphoinUnityHelpers.Editor
{
    public class VideoStreamingSelectorWindow : EditorWindow
    {
        private Action<string> _onSelect;
        private string _searchQuery = "";
        private Vector2 _scrollPos;
        private List<string> _relativePaths = new List<string>();

        private Texture2D _fallbackIcon;
        private const string FALLBACK_ICON_PATH = "Assets/SNEngine/Source/SNEngine/Editor/Sprites/video_editor_icon.png";
        private readonly string[] _extensions = { ".mp4", ".webm", ".mov", ".avi" };

        public static void Open(Action<string> onSelect)
        {
            var window = GetWindow<VideoStreamingSelectorWindow>(true, "Video Selector", true);
            window._onSelect = onSelect;
            window.minSize = new Vector2(400, 500);
            window.RefreshCache();
            window.ShowAuxWindow();
        }

        private void OnEnable()
        {
            LoadResources();
        }

        private void LoadResources()
        {
            if (_fallbackIcon == null)
            {
                _fallbackIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(FALLBACK_ICON_PATH);
            }
        }

        private void RefreshCache()
        {
            LoadResources();
            _relativePaths.Clear();

            string fullPath = Application.streamingAssetsPath;
            if (!Directory.Exists(fullPath)) return;

            var files = Directory.GetFiles(fullPath, "*.*", SearchOption.AllDirectories)
                .Where(f => _extensions.Contains(Path.GetExtension(f).ToLower()));

            foreach (var file in files)
            {
                string rel = file.Replace(fullPath, "").Replace("\\", "/");
                if (rel.StartsWith("/")) rel = rel.Substring(1);
                _relativePaths.Add(rel);
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawSearchBar();
            EditorGUILayout.Space(5);
            DrawVideoList();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(2);
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

        private void DrawVideoList()
        {
            var filtered = _relativePaths
                .Where(p => string.IsNullOrEmpty(_searchQuery) || Path.GetFileName(p).IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            if (filtered.Count == 0)
            {
                EditorGUILayout.HelpBox("No video files found in StreamingAssets.", MessageType.Info);
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            foreach (var relPath in filtered)
            {
                string fileName = Path.GetFileName(relPath);
                float rowHeight = 48f;
                Rect rect = EditorGUILayout.BeginHorizontal(GUI.skin.button, GUILayout.Height(rowHeight));

                if (rect.Contains(Event.current.mousePosition))
                {
                    EditorGUI.DrawRect(rect, new Color(1, 1, 1, 0.05f));
                }

                GUILayout.Space(8);

                EditorGUILayout.BeginVertical(GUILayout.Width(32), GUILayout.Height(rowHeight));
                GUILayout.FlexibleSpace();

                string assetPath = "Assets/StreamingAssets/" + relPath;
                VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(assetPath);
                Texture icon = null;

                if (clip != null)
                {
                    icon = AssetPreview.GetAssetPreview(clip);
                }

                if (icon == null)
                {
                    icon = _fallbackIcon != null ? _fallbackIcon : EditorGUIUtility.IconContent("VideoClip Icon").image;
                }

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
                EditorGUILayout.LabelField(relPath, pathStyle);

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(GUILayout.Width(75), GUILayout.Height(rowHeight));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Select", GUILayout.Height(26)))
                {
                    _onSelect?.Invoke(relPath);
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