#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

namespace SiphoinUnityHelpers.XNodeExtensions.Editor
{
    public class VaritableSelectorWindow : EditorWindow
    {
        private BaseGraph _targetGraph;
        private Action<VaritableNode> _onSelect;
        private Type _requiredType;

        private string _searchQuery = "";
        private Vector2 _scrollPos;

        public static void Open(BaseGraph graph, Type requiredType, Action<VaritableNode> onSelect)
        {
            var window = GetWindow<VaritableSelectorWindow>(true, "Variable Selector", true);
            window._targetGraph = graph;
            window._requiredType = requiredType;
            window._onSelect = onSelect;
            window.minSize = new Vector2(350, 450);
            window.ShowAuxWindow();
        }

        private void OnGUI()
        {
            if (_targetGraph == null) return;

            DrawHeader();
            DrawSearchBar();

            EditorGUILayout.Space(5);
            DrawVariableList();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(2);
            EditorGUILayout.LabelField("Select Target Variable", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Filter: {(_requiredType != null ? _requiredType.Name : "All")}", EditorStyles.miniLabel);
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
            EditorGUILayout.EndHorizontal();
        }

        private void DrawVariableList()
        {
            var nodes = _targetGraph.nodes
                .OfType<VaritableNode>()
                .Where(IsCompatibleType)
                .Where(n => string.IsNullOrEmpty(_searchQuery) || n.Name.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            if (nodes.Count == 0)
            {
                EditorGUILayout.HelpBox("No variables found.", MessageType.Info);
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            foreach (var node in nodes)
            {
                float rowHeight = 48f;
                Rect rect = EditorGUILayout.BeginHorizontal(GUI.skin.button, GUILayout.Height(rowHeight));

                if (rect.Contains(Event.current.mousePosition))
                {
                    EditorGUI.DrawRect(rect, new Color(1, 1, 1, 0.03f));
                }

                Rect colorStrip = new Rect(rect.x, rect.y + 1, 4, rect.height - 2);
                EditorGUI.DrawRect(colorStrip, node.Color);

                GUILayout.Space(8);

                Texture scriptIcon = EditorGUIUtility.IconContent("cs Script Icon").image;

                EditorGUILayout.BeginVertical(GUILayout.Width(32), GUILayout.Height(rowHeight));
                GUILayout.FlexibleSpace();
                Rect iconRect = GUILayoutUtility.GetRect(28, 28);
                GUI.DrawTexture(iconRect, scriptIcon, ScaleMode.ScaleToFit);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();

                GUILayout.Space(4);

                EditorGUILayout.BeginVertical(GUILayout.Height(rowHeight));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(node.Name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(node.GetType().Name, EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(GUILayout.Width(75), GUILayout.Height(rowHeight));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Select", GUILayout.Height(26)))
                {
                    _onSelect?.Invoke(node);
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

        private bool IsCompatibleType(VaritableNode node)
        {
            if (_requiredType == null) return true;

            Type nodeType = node.GetType();
            while (nodeType != null && nodeType != typeof(object))
            {
                if (nodeType.IsGenericType)
                {
                    Type def = nodeType.GetGenericTypeDefinition();
                    if (def == typeof(VaritableNode<>) || def == typeof(VaritableCollectionNode<>))
                    {
                        Type nodeVarType = nodeType.GetGenericArguments()[0];
                        return _requiredType.IsAssignableFrom(nodeVarType);
                    }
                }
                nodeType = nodeType.BaseType;
            }
            return false;
        }
    }
}
#endif