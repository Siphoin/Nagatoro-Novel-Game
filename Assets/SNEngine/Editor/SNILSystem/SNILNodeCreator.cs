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

                string displayName = inst.NodeTypeName;
                if (displayName.EndsWith("Node"))
                    displayName = displayName.Substring(0, displayName.Length - 4);

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
    }
}