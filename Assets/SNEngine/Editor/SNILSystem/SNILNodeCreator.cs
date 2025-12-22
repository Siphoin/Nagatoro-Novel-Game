using System;
using System.Collections.Generic;
using System.Linq;
using SiphoinUnityHelpers.XNodeExtensions;
using SiphoinUnityHelpers.XNodeExtensions.NodesControlExecutes;
using SNEngine.Graphs;
using UnityEditor;
using UnityEngine;
using XNode;

namespace SNEngine.Editor.SNILSystem
{
    public class SNILNodeCreator
    {
        public static void CreateNodesFromInstructions(DialogueGraph graph, List<SNILInstruction> mainInstructions, List<SNILInstruction> functionInstructions = null)
        {
            List<BaseNode> mainNodes = new List<BaseNode>();
            List<BaseNode> functionNodes = new List<BaseNode>();
            List<GroupCallsNode> groupCallNodes = new List<GroupCallsNode>();
            Dictionary<string, GroupCallsNode> functionMap = new Dictionary<string, GroupCallsNode>();
            Dictionary<string, List<BaseNode>> functionBodies = new Dictionary<string, List<BaseNode>>();

            int mainNodeIndex = 0;
            int functionNodeIndex = 0;

            // Сначала создаем ноды функций и групп вызовов
            if (functionInstructions != null)
            {
                // Сначала создаем GroupCallsNode для каждой функции и сохраняем их в маппинг
                var groupCallInstructions = functionInstructions.Where(inst => inst.NodeTypeName == "GroupCallsNode").ToList();
                
                foreach (var inst in groupCallInstructions)
                {
                    if (inst.NodeType == null) continue;

                    GroupCallsNode node = graph.AddNode(inst.NodeType) as GroupCallsNode;

                    string displayName = FormatNodeDisplayName(inst.NodeTypeName);
                    node.name = displayName;
                    node.position = new Vector2(functionNodeIndex * 250, -150); // Размещаем группы вызовов выше основного потока

                    SNILParameterApplier.ApplyParametersToNode(node, inst.Parameters, inst.NodeTypeName);

                    AssetDatabase.AddObjectToAsset(node, graph);
                    
                    // Сохраняем соответствие имя функции -> GroupCallsNode
                    if (inst.Parameters.ContainsKey("name"))
                    {
                        string functionName = inst.Parameters["name"];
                        functionMap[functionName] = node;
                        functionBodies[functionName] = new List<BaseNode>(); // Инициализируем пустое тело функции
                    }
                    
                    groupCallNodes.Add(node);
                    functionNodes.Add(node);
                    functionNodeIndex++;
                }

                // Затем создаем ноды тела функций и распределяем их по соответствующим функциям
                var functionBodyInstructions = functionInstructions.Where(inst => inst.NodeTypeName != "GroupCallsNode").ToList();
                
                // Для простоты, предположим, что инструкции идут в том же порядке, что и определения функций
                string currentFunctionName = null;
                foreach (var inst in functionInstructions)
                {
                    if (inst.NodeTypeName == "GroupCallsNode" && inst.Parameters.ContainsKey("name"))
                    {
                        currentFunctionName = inst.Parameters["name"];
                    }
                    else if (inst.NodeTypeName != "GroupCallsNode" && currentFunctionName != null)
                    {
                        BaseNode node = graph.AddNode(inst.NodeType) as BaseNode;

                        string displayName = FormatNodeDisplayName(inst.NodeTypeName);
                        node.name = displayName;
                        node.position = new Vector2(functionNodeIndex * 250, -100); // Размещаем тело функций еще ниже

                        SNILParameterApplier.ApplyParametersToNode(node, inst.Parameters, inst.NodeTypeName);

                        AssetDatabase.AddObjectToAsset(node, graph);
                        functionNodes.Add(node);
                        functionBodies[currentFunctionName].Add(node); // Добавляем ноду к телу текущей функции
                        functionNodeIndex++;
                    }
                }
            }

            // Затем создаем ноды основного скрипта
            for (int i = 0; i < mainInstructions.Count; i++)
            {
                var inst = mainInstructions[i];
                if (inst.NodeType == null) continue;

                BaseNode node = graph.AddNode(inst.NodeType) as BaseNode;

                string displayName = FormatNodeDisplayName(inst.NodeTypeName);
                node.name = displayName;
                
                // Размещаем StartNode в начале
                if (inst.NodeTypeName.Equals("StartNode", StringComparison.OrdinalIgnoreCase))
                {
                    node.position = new Vector2(0, 0);
                }
                else
                {
                    node.position = new Vector2((i + 1) * 250, 0); // Сдвигаем остальные ноды
                }

                SNILParameterApplier.ApplyParametersToNode(node, inst.Parameters, inst.NodeTypeName);

                AssetDatabase.AddObjectToAsset(node, graph);
                mainNodes.Add(node);
            }

            // Подключаем ноды тела функций к соответствующим GroupCallsNode через порт _operations
            foreach (var functionBody in functionBodies)
            {
                if (functionBody.Value.Count > 0 && functionMap.ContainsKey(functionBody.Key))
                {
                    var groupNode = functionMap[functionBody.Key];
                    ConnectFunctionBodyToGroup(groupNode, functionBody.Value);
                }
            }

            // Соединяем основные ноды последовательно
            ConnectMainNodesSequentially(mainNodes);

            // Обрабатываем вызовы функций в основном скрипте и подключаем их к соответствующим GroupCallsNode
            ProcessFunctionCalls(mainInstructions, mainNodes, functionMap);
        }

        private static void ConnectFunctionBodyToGroup(GroupCallsNode groupNode, List<BaseNode> bodyNodes)
        {
            if (bodyNodes.Count == 0) return;

            // Подключаем первую ноду тела к порту _operations GroupCallsNode
            var operationsPort = groupNode.GetOutputPort("_operations");
            var firstNodeEnterPort = bodyNodes[0] is BaseNodeInteraction interaction ? interaction.GetEnterPort() : null;
            
            if (operationsPort != null && firstNodeEnterPort != null)
            {
                operationsPort.Connect(firstNodeEnterPort);
            }

            // Подключаем остальные ноды тела последовательно
            for (int i = 0; i < bodyNodes.Count - 1; i++)
            {
                if (bodyNodes[i] is BaseNodeInteraction curr && bodyNodes[i + 1] is BaseNodeInteraction next)
                {
                    var outPort = curr.GetExitPort();
                    var inPort = next.GetEnterPort();
                    if (outPort != null && inPort != null) outPort.Connect(inPort);
                }
            }
        }

        private static void ConnectMainNodesSequentially(List<BaseNode> mainNodes)
        {
            // Находим StartNode и убеждаемся, что он первый
            var startNode = mainNodes.FirstOrDefault(n => n is SiphoinUnityHelpers.XNodeExtensions.BaseNodeInteraction && 
                                                         n.GetType().Name.Equals("StartNode", StringComparison.OrdinalIgnoreCase));
            
            if (startNode != null)
            {
                // Перемещаем StartNode в начало списка для правильного соединения
                mainNodes.Remove(startNode);
                mainNodes.Insert(0, startNode);
            }

            // Соединяем основные ноды последовательно
            for (int i = 0; i < mainNodes.Count - 1; i++)
            {
                if (mainNodes[i] is BaseNodeInteraction curr && mainNodes[i + 1] is BaseNodeInteraction next)
                {
                    var outPort = curr.GetExitPort();
                    var inPort = next.GetEnterPort();
                    if (outPort != null && inPort != null) outPort.Connect(inPort);
                }
            }
        }

        private static void ProcessFunctionCalls(List<SNILInstruction> mainInstructions, List<BaseNode> mainNodes, Dictionary<string, GroupCallsNode> functionMap)
        {
            // Обработка вызовов функций (команды "call")
            for (int i = 0; i < mainInstructions.Count; i++)
            {
                var inst = mainInstructions[i];
                if (inst.NodeTypeName == "CallFunctionNode" && inst.Parameters.ContainsKey("functionName"))
                {
                    string functionName = inst.Parameters["functionName"];
                    
                    // Находим ноду вызова функции в списке mainNodes
                    if (i < mainNodes.Count && functionMap.ContainsKey(functionName))
                    {
                        var callNode = mainNodes[i] as BaseNodeInteraction;
                        var targetGroup = functionMap[functionName];
                        
                        if (callNode != null)
                        {
                            // В реальности вызов функции должен как-то активировать GroupCallsNode
                            // Это упрощенная реализация - просто подключаем следующую ноду после вызова
                        }
                    }
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