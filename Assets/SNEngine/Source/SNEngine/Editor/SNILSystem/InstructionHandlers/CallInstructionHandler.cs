using SiphoinUnityHelpers.XNodeExtensions;
using SNEngine.Graphs;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace SNEngine.Editor.SNILSystem.InstructionHandlers
{
    public class CallInstructionHandler : BaseInstructionHandler
    {
        public override bool CanHandle(string instruction)
        {
            return Regex.IsMatch(instruction.Trim(), @"^call\s+.+", RegexOptions.IgnoreCase);
        }

        public override InstructionResult Handle(string instruction, InstructionContext context)
        {
            var (success, functionName) = ExtractValue(instruction, @"^call\s+(.+)");

            if (!success)
            {
                return InstructionResult.Error("Invalid call instruction format. Expected: 'call <function_name>'");
            }

            // Проверяем, существует ли функция
            if (!context.Functions.ContainsKey(functionName))
            {
                return InstructionResult.Error($"Function '{functionName}' not found.");
            }

            // Создаем GroupCallsNode для вызова функции
            if (context.Graph == null)
            {
                return InstructionResult.Error("Graph not initialized. Cannot create function call node.");
            }

            var dialogueGraph = (DialogueGraph)context.Graph;

            // Создаем GroupCallsNode (или другой тип ноды для вызова функции)
            var groupCallsNodeType = SNILTypeResolver.GetNodeType("GroupCallsNode");
            if (groupCallsNodeType == null)
            {
                return InstructionResult.Error("GroupCallsNode type not found.");
            }

            var callNode = dialogueGraph.AddNode(groupCallsNodeType) as BaseNode;
            if (callNode != null)
            {
                callNode.name = $"Call {functionName}";
                callNode.position = new Vector2(context.Nodes.Count * 250, 0);

                // Устанавливаем имя функции как параметр
                var parameters = new Dictionary<string, string> { { "name", functionName } };
                SNILParameterApplier.ApplyParametersToNode(callNode, parameters, "GroupCallsNode");

                AssetDatabase.AddObjectToAsset(callNode, dialogueGraph);
                context.Nodes.Add(callNode);
                // Соединяем с предыдущей нодой
                NodeConnectionUtility.ConnectNodeToLast(dialogueGraph, callNode, context);

                return InstructionResult.Ok(callNode);
            }

            return InstructionResult.Error($"Failed to create function call node for function: {functionName}");
        }
    }
}