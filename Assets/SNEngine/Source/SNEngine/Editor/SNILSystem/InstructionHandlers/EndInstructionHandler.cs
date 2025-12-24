using SiphoinUnityHelpers.XNodeExtensions;
using SNEngine.Graphs;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SNEngine.Editor.SNILSystem.InstructionHandlers
{
    public class EndInstructionHandler : BaseInstructionHandler
    {
        public override bool CanHandle(string instruction)
        {
            return string.Equals(instruction.Trim(), "End", StringComparison.OrdinalIgnoreCase);
        }

        public override InstructionResult Handle(string instruction, InstructionContext context)
        {
            if (context.Graph == null)
            {
                return InstructionResult.Error("Graph not initialized. 'name:' instruction must be processed before 'End'.");
            }

            var dialogueGraph = (DialogueGraph)context.Graph;
            
            // Создаем ExitNode (End нода)
            var exitNodeType = SNILTypeResolver.GetNodeType("ExitNode");
            if (exitNodeType == null)
            {
                return InstructionResult.Error("ExitNode type not found.");
            }

            var exitNode = dialogueGraph.AddNode(exitNodeType) as BaseNode;
            if (exitNode != null)
            {
                exitNode.name = "End";
                // Позиционируем после последней ноды
                float xPosition = context.Nodes.Count > 0 ? 250 * context.Nodes.Count : 250;
                exitNode.position = new Vector2(xPosition, 0);

                // Применяем параметры, если есть
                var parameters = new Dictionary<string, string>();
                SNILParameterApplier.ApplyParametersToNode(exitNode, parameters, "ExitNode");

                AssetDatabase.AddObjectToAsset(exitNode, dialogueGraph);
                context.Nodes.Add(exitNode);

                // Сохраняем ссылку на EndNode в контексте
                if (context.Variables.ContainsKey("EndNode"))
                    context.Variables["EndNode"] = exitNode;
                else
                    context.Variables.Add("EndNode", exitNode);

                // Обновляем последнюю ноду
                context.LastNode = exitNode;
            }

            return InstructionResult.Ok(exitNode);
        }
    }
}