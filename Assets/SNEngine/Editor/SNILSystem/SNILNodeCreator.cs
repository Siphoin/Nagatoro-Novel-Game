using System;
using System.Collections.Generic;
using SiphoinUnityHelpers.XNodeExtensions;
using SNEngine.Graphs;
using UnityEditor;
using UnityEngine;

namespace SNEngine.Editor.SNILSystem
{
    public class SNILNodeCreator
    {
        public static void CreateNodesFromInstructions(DialogueGraph graph, List<SNILInstruction> instructions)
        {
            List<BaseNode> nodes = new List<BaseNode>();
            for (int i = 0; i < instructions.Count; i++)
            {
                var inst = instructions[i];
                if (inst.NodeType == null) continue;

                BaseNode node = graph.AddNode(inst.NodeType) as BaseNode;

                string displayName = FormatNodeDisplayName(inst.NodeTypeName);
                node.name = displayName;
                node.position = new Vector2(i * 250, 0);

                SNILParameterApplier.ApplyParametersToNode(node, inst.Parameters, inst.NodeTypeName);

                AssetDatabase.AddObjectToAsset(node, graph);
                nodes.Add(node);
            }

            for (int i = 0; i < nodes.Count - 1; i++)
            {
                if (nodes[i] is BaseNodeInteraction curr && nodes[i + 1] is BaseNodeInteraction next)
                {
                    var outPort = curr.GetExitPort();
                    var inPort = next.GetEnterPort();
                    if (outPort != null && inPort != null) outPort.Connect(inPort);
                }
            }
        }

        private static string FormatNodeDisplayName(string nodeTypeName)
        {
            // Преобразуем "ShowCharacter" в "Show Character"
            // Добавляем пробел перед заглавной буквой, если перед ней есть строчная буква
            if (string.IsNullOrEmpty(nodeTypeName)) return nodeTypeName;

            var result = new System.Text.StringBuilder();
            result.Append(nodeTypeName[0]); // Первую букву добавляем как есть

            for (int i = 1; i < nodeTypeName.Length; i++)
            {
                char currentChar = nodeTypeName[i];
                char prevChar = nodeTypeName[i - 1];

                // Если текущий символ - заглавная буква, а предыдущий - строчная, вставляем пробел
                if (char.IsUpper(currentChar) && char.IsLower(prevChar))
                {
                    result.Append(' ');
                }
                result.Append(currentChar);
            }

            string displayName = result.ToString();
            
            // Убираем "Node" из конца, если оно есть
            if (displayName.EndsWith("Node"))
                displayName = displayName.Substring(0, displayName.Length - 4);

            return displayName;
        }
    }
}