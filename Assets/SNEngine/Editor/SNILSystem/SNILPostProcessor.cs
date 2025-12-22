using System;
using System.Collections.Generic;
using SNEngine.Graphs;

namespace SNEngine.Editor.SNILSystem
{
    public class SNILPostProcessor
    {
        private static Dictionary<string, DialogueGraph> _createdGraphs = new Dictionary<string, DialogueGraph>();
        private static List<JumpToReference> _pendingJumps = new List<JumpToReference>();

        public static void RegisterGraph(string name, DialogueGraph graph)
        {
            _createdGraphs[name] = graph;
        }

        public static void RegisterJumpToReference(object node, string fieldName, string targetDialogueName)
        {
            _pendingJumps.Add(new JumpToReference
            {
                Node = node,
                FieldName = fieldName,
                TargetDialogueName = targetDialogueName
            });
        }

        public static void ProcessAllReferences()
        {
            foreach (var jumpRef in _pendingJumps)
            {
                if (_createdGraphs.ContainsKey(jumpRef.TargetDialogueName))
                {
                    var field = jumpRef.Node.GetType().GetField(jumpRef.FieldName,
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Instance);
                    
                    if (field != null)
                    {
                        field.SetValue(jumpRef.Node, _createdGraphs[jumpRef.TargetDialogueName]);
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

        private class JumpToReference
        {
            public object Node { get; set; }
            public string FieldName { get; set; }
            public string TargetDialogueName { get; set; }
        }
    }
}