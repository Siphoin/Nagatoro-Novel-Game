using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        public static void CreateNodesFromInstructions(DialogueGraph graph, List<SNILInstruction> mainInstructions, List<SNILInstruction> functionInstructions = null, List<int> functionCallPositions = null)
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
                    // Размещаем GroupCallsNode на том же уровне, что и основной поток, но чуть выше для визуального отделения
                    node.position = new Vector2(0, -150); // Временно ставим в нулевую позицию, переместим позже

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
                        // Размещаем тело функций выше основного потока, чтобы не мешало
                        node.position = new Vector2(0, -100); // Временная позиция, будет обновлена позже

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

            // Создаем полный список нод с вставленными GroupCallsNode в нужные места
            var completeNodeSequence = BuildCompleteNodeSequence(mainNodes, functionCallPositions, functionMap);
            
            // Расставляем позиции для всех нод в последовательности
            PositionNodesHorizontally(completeNodeSequence);
            
            // Соединяем все ноды последовательно
            ConnectNodesSequentially(completeNodeSequence);
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
            
            // Позиционируем ноды тела функции выше основного потока
            PositionFunctionBodyNodes(groupNode, bodyNodes);
        }

        private static void PositionFunctionBodyNodes(GroupCallsNode groupNode, List<BaseNode> bodyNodes)
        {
            if (bodyNodes.Count == 0) return;

            // Получаем позицию GroupCallsNode для определения стартовой точки
            float groupNodeX = groupNode.position.x;
            float groupNodeY = groupNode.position.y; // Это будет -150 или другое отрицательное значение
            
            // Размещаем ноды тела функции немного правее GroupCallsNode и выше основного потока
            for (int i = 0; i < bodyNodes.Count; i++)
            {
                bodyNodes[i].position = new Vector2(groupNodeX + (i + 1) * 250, groupNodeY - 50); // Ещё выше основного потока
            }
        }

        private static List<BaseNode> BuildCompleteNodeSequence(List<BaseNode> mainNodes, List<int> functionCallPositions, Dictionary<string, GroupCallsNode> functionMap)
        {
            var completeSequence = new List<BaseNode>(mainNodes);
            
            // Находим StartNode и убеждаемся, что он первый
            var startNode = completeSequence.FirstOrDefault(n => n is SiphoinUnityHelpers.XNodeExtensions.BaseNodeInteraction && 
                                                                n.GetType().Name.Equals("StartNode", StringComparison.OrdinalIgnoreCase));
            
            if (startNode != null)
            {
                completeSequence.Remove(startNode);
                completeSequence.Insert(0, startNode);
            }

            // Вставляем GroupCallsNode в нужные позиции
            // Для упрощения, вставляем все GroupCallsNode в порядке их появления в functionMap
            if (functionCallPositions != null && functionCallPositions.Count > 0)
            {
                // Сортируем позиции по возрастанию, чтобы индексы не смещались при вставке
                var sortedPositions = functionCallPositions.OrderBy(x => x).ToList();
                
                // Вставляем GroupCallsNode в указанные позиции
                for (int i = sortedPositions.Count - 1; i >= 0; i--)
                {
                    int pos = sortedPositions[i];
                    // В реальности нужно знать, какую именно функцию вызвать в этой позиции
                    // Для упрощения, возьмем первую доступную GroupCallsNode
                    var groupNode = functionMap.Values.ElementAtOrDefault(i);
                    if (groupNode != null && pos < completeSequence.Count && pos >= 0)
                    {
                        completeSequence.Insert(pos, groupNode);
                    }
                }
            }
            else
            {
                // Если нет информации о позициях вызовов, вставляем все GroupCallsNode в конец
                foreach (var groupNode in functionMap.Values)
                {
                    completeSequence.Add(groupNode);
                }
            }
            
            return completeSequence;
        }

        private static void PositionNodesHorizontally(List<BaseNode> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                // Устанавливаем позицию на основном уровне (y=0) с интервалом 250 пикселей
                node.position = new Vector2(i * 250, 0);
            }
        }

        private static void ConnectNodesSequentially(List<BaseNode> nodes)
        {
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