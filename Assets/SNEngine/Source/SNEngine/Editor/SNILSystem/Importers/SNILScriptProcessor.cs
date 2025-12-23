using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SNEngine.Graphs;
using SNEngine.Editor.SNILSystem.FunctionSystem;
using SNEngine.Editor.SNILSystem.Parsers;
using UnityEditor;
using UnityEngine;

namespace SNEngine.Editor.SNILSystem.Importers
{
    public class SNILScriptProcessor
    {
        public static void ProcessSingleGraph(string[] lines)
        {
            if (lines.Length == 0) return;

            string graphName = ExtractGraphName(lines);
            graphName = SanitizeFileName(graphName);

            string assetPath = $"Assets/SNEngine/Source/SNEngine/Resources/Dialogues/{graphName}.asset";
            DialogueGraph graph = AssetDatabase.LoadAssetAtPath<DialogueGraph>(assetPath);

            if (graph == null)
            {
                Debug.LogError($"Could not load graph: {assetPath}");
                return;
            }

            var functions = SNILFunctionParser.ParseFunctions(lines);
            var mainScriptLines = SNILFunctionParser.ExtractMainScriptWithoutFunctions(lines).ToArray();

            var functionInstructions = ParseFunctionInstructions(functions);

            var (mainInstructions, functionCallPositions, functionCallNames) = ParseScriptWithFunctionCalls(mainScriptLines);

            ApplyInstructionsToGraph(graphName, mainInstructions, functionInstructions, functionCallPositions, functionCallNames);
        }

        public static List<SNILInstruction> ParseFunctionInstructions(List<SNILFunction> functions)
        {
            var functionInstructions = new List<SNILInstruction>();

            foreach (var function in functions)
            {
                // Создаем GroupCallsNode для функции
                var groupCallsNodeInstruction = new SNILInstruction
                {
                    Type = SNILInstructionType.Generic,
                    NodeTypeName = "GroupCallsNode",
                    Parameters = new Dictionary<string, string> { { "name", function.Name } },
                    NodeType = SNILTypeResolver.GetNodeType("GroupCallsNode")
                };

                functionInstructions.Add(groupCallsNodeInstruction);

                // Создаем ноды для тела функции
                var functionBodyInstructions = ParseScript(function.Body);

                // Добавляем инструкции тела функции
                foreach (var instruction in functionBodyInstructions)
                {
                    functionInstructions.Add(instruction);
                }
            }

            return functionInstructions;
        }

        public static (List<SNILInstruction>, List<int>, List<string>) ParseScriptWithFunctionCalls(string[] lines)
        {
            var templates = SNILTemplateManager.GetNodeTemplates();
            List<SNILInstruction> instructions = new List<SNILInstruction>();
            List<int> functionCallPositions = new List<int>(); // Позиции вызовов функций в потоке
            List<string> functionCallNames = new List<string>(); // Имена вызываемых функций

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || IsCommentLine(trimmed)) continue;

                var nameMatch = Regex.Match(trimmed, @"^name:\s*(.+)", RegexOptions.IgnoreCase);
                if (nameMatch.Success) continue;

                // Пропускаем только определения функций и концы функций (не конец диалога)
                if (trimmed.StartsWith("function ", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Equals("end", StringComparison.Ordinal)) // Только lowercase "end" для функций, не "End" для диалога
                {
                    continue;
                }

                // Обрабатываем вызовы функций
                if (trimmed.StartsWith("call ", StringComparison.OrdinalIgnoreCase))
                {
                    string functionName = trimmed.Substring(5).Trim(); // "call ".Length = 5
                    functionCallPositions.Add(instructions.Count); // Сохраняем позицию вызова функции
                    functionCallNames.Add(functionName); // Сохраняем имя вызываемой функции
                    continue; // Пропускаем создание ноды для вызова функции
                }

                // For now, use the simple parser for non-block instructions
                // The block parser handles IF-ELSE-ENDIF structures
                var instruction = MatchLineToTemplate(trimmed, templates);
                if (instruction != null)
                {
                    instructions.Add(instruction);
                }
            }

            // For a more complete implementation, we would need to integrate function call handling into the block parser
            // For now, we'll use the block parser for the main logic
            var blockInstructions = SNILBlockParser.ParseWithBlocks(lines);

            // We need to filter out function calls from the block parser and track their positions and names
            var filteredInstructions = new List<SNILInstruction>();
            var updatedFunctionCallPositions = new List<int>();
            var updatedFunctionCallNames = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                if (trimmed.StartsWith("call ", StringComparison.OrdinalIgnoreCase))
                {
                    string functionName = trimmed.Substring(5).Trim();
                    updatedFunctionCallPositions.Add(filteredInstructions.Count);
                    updatedFunctionCallNames.Add(functionName);
                }
            }

            // Since the block parser handles all instructions including nested ones,
            // we'll return the block parser results
            return (blockInstructions, updatedFunctionCallPositions, updatedFunctionCallNames);
        }

        private static List<SNILInstruction> ParseScript(string[] lines)
        {
            // Use the new block parser to handle IF-ELSE-ENDIF structures
            return SNILBlockParser.ParseWithBlocks(lines);
        }

        public static string ExtractGraphName(string[] lines)
        {
            string graphName = "NewGraph";
            foreach (string line in lines)
            {
                var nameMatch = Regex.Match(line.Trim(), @"^name:\s*(.+)", RegexOptions.IgnoreCase);
                if (nameMatch.Success)
                {
                    graphName = nameMatch.Groups[1].Value.Trim();
                    break;
                }
            }
            return graphName;
        }

        public static void ApplyInstructionsToGraph(string graphName, List<SNILInstruction> mainInstructions, List<SNILInstruction> functionInstructions, List<int> functionCallPositions, List<string> functionCallNames = null)
        {
            string assetPath = $"Assets/SNEngine/Source/SNEngine/Resources/Dialogues/{graphName}.asset";
            DialogueGraph graph = AssetDatabase.LoadAssetAtPath<DialogueGraph>(assetPath);

            if (graph == null)
            {
                Debug.LogError($"Could not load graph: {assetPath}");
                return;
            }

            SNILNodeCreator.CreateNodesFromInstructions(graph, mainInstructions, functionInstructions, functionCallPositions, functionCallNames);

            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Imported: {assetPath}");
        }

        private static string SanitizeFileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) fileName = fileName.Replace(c, '_');
            return string.IsNullOrWhiteSpace(fileName) ? "NewGraph" : fileName;
        }

        private static bool IsCommentLine(string line)
        {
            string trimmed = line.Trim();
            return trimmed.StartsWith("//") || trimmed.StartsWith("#");
        }

        private static SNILInstruction MatchLineToTemplate(string line, Dictionary<string, SNILTemplateInfo> templates)
        {
            foreach (var template in templates)
            {
                var parameters = SNILTemplateMatcher.MatchLineWithTemplate(line, template.Value.Template);
                if (parameters != null)
                {
                    return new SNILInstruction
                    {
                        Type = SNILInstructionType.Generic,
                        NodeTypeName = template.Key,
                        Parameters = parameters,
                        NodeType = SNILTypeResolver.GetNodeType(template.Key)
                    };
                }
            }
            return null;
        }
    }
}