using System.Collections.Generic;
using UnityEngine;

namespace XNode {
    /// <summary> Utility class to manage node highlighting </summary>
    public static class NodeHighlighter {
        private static HashSet<Node> highlightedNodes = new HashSet<Node>();
        private static Dictionary<Node, Color> customHighlightColors = new Dictionary<Node, Color>();

        /// <summary> Highlight a specific node with the default highlight color </summary>
        public static void HighlightNode(Node node) {
            if (node != null) {
                highlightedNodes.Add(node);
                if (customHighlightColors.ContainsKey(node)) {
                    customHighlightColors.Remove(node);
                }
            }
        }

        /// <summary> Highlight a specific node with a custom highlight color </summary>
        public static void HighlightNode(Node node, Color highlightColor) {
            if (node != null) {
                highlightedNodes.Add(node);
                customHighlightColors[node] = highlightColor;
            }
        }

        /// <summary> Remove highlighting from a specific node </summary>
        public static void RemoveHighlight(Node node) {
            if (node != null) {
                highlightedNodes.Remove(node);
                if (customHighlightColors.ContainsKey(node)) {
                    customHighlightColors.Remove(node);
                }
            }
        }

        /// <summary> Check if a node is currently highlighted </summary>
        public static bool IsNodeHighlighted(Node node) {
            return node != null && highlightedNodes.Contains(node);
        }

        /// <summary> Get the highlight color for a node (custom if set, otherwise default) </summary>
        public static Color GetHighlightColor(Node node) {
            if (node != null && customHighlightColors.ContainsKey(node)) {
                return customHighlightColors[node];
            }
            // Return default highlight color - this will be handled by the editor
            return GetDefaultHighlightColor(); // Return the default editor highlight color
        }

        /// <summary> Get the default highlight color from editor preferences </summary>
        public static Color GetDefaultHighlightColor() {
#if UNITY_EDITOR
            return UnityEditor.EditorGUIUtility.isProSkin ?
                new Color32(255, 255, 255, 255) :
                new Color32(0, 0, 0, 255);
#else
            return Color.white;
#endif
        }

        /// <summary> Set the default highlight color to use the editor's preference </summary>
        public static void UseDefaultHighlightColor(Node node) {
            if (node != null && customHighlightColors.ContainsKey(node)) {
                customHighlightColors.Remove(node);
            }
        }

        /// <summary> Remove highlighting from all nodes </summary>
        public static void ClearAllHighlights() {
            highlightedNodes.Clear();
            customHighlightColors.Clear();
        }

        /// <summary> Get all currently highlighted nodes </summary>
        public static HashSet<Node> GetHighlightedNodes() {
            return new HashSet<Node>(highlightedNodes);
        }
    }
}