#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using SNEngine.Graphs;

namespace SiphoinUnityHelpers.XNodeExtensions.Editor
{
    public class VaritableSelectorWindow : EditorWindow
    {
        private enum Category { Local, Global }

        private BaseGraph _targetGraph;
        private Action<VaritableNode> _onSelect;
        private Type _requiredType;

        private Category _currentCategory = Category.Local;
        private string _searchQuery = "";
        private Vector2 _scrollPos;

        private List<VaritableNode> _localNodes = new List<VaritableNode>();
        private List<VaritableNode> _globalNodes = new List<VaritableNode>();

        public static void Open(BaseGraph graph, Type requiredType, Action<VaritableNode> onSelect)
        {
            var window = GetWindow<VaritableSelectorWindow>(true, "Variable Selector", true);
            window._targetGraph = graph;
            window._requiredType = requiredType;
            window._onSelect = onSelect;
            window.minSize = new Vector2(350, 450);
            window.RefreshCache();
            window.ShowAuxWindow();
        }

        private void RefreshCache()
        {
            _localNodes.Clear();
            _globalNodes.Clear();

            if (_targetGraph != null)
            {
                _localNodes.AddRange(_targetGraph.nodes.OfType<VaritableNode>().Where(IsCompatibleType));
            }

            var globalContainer = Resources.Load<VaritableContainerGraph>("VaritableContainerGraph");
            if (globalContainer != null && globalContainer != _targetGraph)
            {
                _globalNodes.AddRange(globalContainer.nodes.OfType<VaritableNode>().Where(IsCompatibleType));
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawCategoryToggles();
            DrawSearchBar();

            EditorGUILayout.Space(5);
            DrawVariableList();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(2);
            EditorGUILayout.LabelField("Variable Selector", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Type: {(_requiredType != null ? _requiredType.Name : "Any")}", EditorStyles.miniLabel);
            GUILayout.Space(2);
            EditorGUILayout.EndVertical();
        }

        private void DrawCategoryToggles()
        {
            GUILayout.Space(2);
            _currentCategory = (Category)GUILayout.Toolbar((int)_currentCategory, new string[] { "Local", "Global" });
            GUILayout.Space(2);
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
            var sourceList = _currentCategory == Category.Local ? _localNodes : _globalNodes;

            var nodes = sourceList
                .Where(n => string.IsNullOrEmpty(_searchQuery) || n.Name.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(n => n.Name)
                .ToList();

            if (nodes.Count == 0)
            {
                EditorGUILayout.HelpBox($"No {(_currentCategory == Category.Local ? "local" : "global")} variables found.", MessageType.Info);
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            foreach (var node in nodes)
            {
                float rowHeight = 48f;
                Rect rect = EditorGUILayout.BeginHorizontal(GUI.skin.button, GUILayout.Height(rowHeight));

                if (rect.Contains(Event.current.mousePosition))
                {
                    EditorGUI.DrawRect(rect, new Color(1, 1, 1, 0.05f));
                }

                Rect colorStrip = new Rect(rect.x, rect.y + 1, 4, rect.height - 2);
                EditorGUI.DrawRect(colorStrip, node.Color);

                GUILayout.Space(8);

                EditorGUILayout.BeginVertical(GUILayout.Width(32), GUILayout.Height(rowHeight));
                GUILayout.FlexibleSpace();

                GUIContent iconContent = EditorGUIUtility.ObjectContent(null, node.GetType());
                Texture nodeIcon = iconContent.image;

                if (nodeIcon == null)
                {
                    nodeIcon = EditorGUIUtility.IconContent("cs Script Icon").image;
                }

                Rect iconRect = GUILayoutUtility.GetRect(26, 26);
                if (nodeIcon != null)
                {
                    GUI.DrawTexture(iconRect, nodeIcon, ScaleMode.ScaleToFit);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();

                GUILayout.Space(4);

                EditorGUILayout.BeginVertical(GUILayout.Height(rowHeight));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(node.Name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(_currentCategory == Category.Global ? $"Container: {node.graph.name}" : node.GetType().Name, EditorStyles.miniLabel);
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