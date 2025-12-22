using System;
using System.Collections.Generic;
using SNEngine.Graphs;
using UnityEditor;

namespace SNEngine.Editor.SNILSystem
{
    public class SNILPostProcessor
    {
        private static Dictionary<string, DialogueGraph> _createdGraphs = new Dictionary<string, DialogueGraph>();
        private static List<JumpToReference> _pendingJumps = new List<JumpToReference>();

        public static void RegisterGraph(string name, DialogueGraph graph)
        {
            if (!_createdGraphs.ContainsKey(name))
            {
                _createdGraphs[name] = graph;
                UnityEngine.Debug.Log($"Registered graph: {name}");
            }
        }

        public static bool IsGraphRegistered(string name)
        {
            return _createdGraphs.ContainsKey(name);
        }

        public static void RegisterJumpToReference(object node, string fieldName, string targetDialogueName)
        {
            _pendingJumps.Add(new JumpToReference
            {
                Node = node,
                FieldName = fieldName,
                TargetDialogueName = targetDialogueName
            });
            UnityEngine.Debug.Log($"Registered pending jump: {targetDialogueName}");
        }

        public static void ProcessAllReferences()
        {
            UnityEngine.Debug.Log($"Processing { _pendingJumps.Count } pending jumps for { _createdGraphs.Count } graphs");
            
            foreach (var jumpRef in _pendingJumps)
            {
                UnityEngine.Debug.Log($"Processing jump: {jumpRef.TargetDialogueName}");
                
                if (_createdGraphs.ContainsKey(jumpRef.TargetDialogueName))
                {
                    var field = jumpRef.Node.GetType().GetField(jumpRef.FieldName,
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Instance);
                    
                    if (field != null)
                    {
                        // Загружаем реальный граф из ассета
                        DialogueGraph realGraph = AssetDatabase.LoadAssetAtPath<DialogueGraph>($"Assets/SNEngine/Source/SNEngine/Resources/Dialogues/{jumpRef.TargetDialogueName}.asset");
                        if (realGraph != null)
                        {
                            field.SetValue(jumpRef.Node, realGraph);
                            UnityEngine.Debug.Log($"Successfully set jump reference from {GetNodeInfo(jumpRef.Node)} to {jumpRef.TargetDialogueName}");
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning($"Could not load dialogue asset: Assets/SNEngine/Source/SNEngine/Resources/Dialogues/{jumpRef.TargetDialogueName}.asset");
                        }
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"Target dialogue '{jumpRef.TargetDialogueName}' not found for jump node.");
                }
            }
            
            // Очищаем списки после обработки
            _pendingJumps.Clear();
            _createdGraphs.Clear();
        }

        private static string GetNodeInfo(object node)
        {
            if (node is SiphoinUnityHelpers.XNodeExtensions.BaseNode baseNode)
            {
                return $"{baseNode.GetType().Name} ({baseNode.GUID})";
            }
            return node.GetType().Name;
        }

        private class JumpToReference
        {
            public object Node { get; set; }
            public string FieldName { get; set; }
            public string TargetDialogueName { get; set; }
        }
    }
}