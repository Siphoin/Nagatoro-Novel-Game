using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SiphoinUnityHelpers.XNodeExtensions;
using SNEngine.Graphs;
using SNEngine.DialogSystem;
using SNEngine.Editor.SNILSystem.FunctionSystem;
using SNEngine.Editor.SNILSystem.Parsers;
using SNEngine.Editor.SNILSystem.Validators;
using UnityEditor;
using UnityEngine;
using XNode;

namespace SNEngine.Editor.SNILSystem
{
    public class SNILCompiler
    {
        public static void ImportScript(string filePath)
        {
            ImportScriptInternal(filePath, true);
        }

        public static bool ValidateScript(string filePath, out List<Validators.SNILValidationError> errors)
        {
            errors = new List<Validators.SNILValidationError>();
            
            try
            {
                filePath = filePath.Trim().Trim('"', '@', '\'');

                if (!File.Exists(filePath))
                {
                    errors.Add(new Validators.SNILValidationError
                    {
                        LineNumber = 0,
                        LineContent = "",
                        ErrorType = Validators.SNILValidationErrorType.EmptyFile,
                        Message = $"File not found: {filePath}"
                    });
                    return false;
                }

                // Проверяем, содержит ли файл несколько скриптов
                List<string[]> scriptParts = SNILMultiScriptParser.ParseMultiScript(filePath);
                
                bool isValid = true;
                foreach (string[] part in scriptParts)
                {
                    Validators.SNILSyntaxValidator validator = new Validators.SNILSyntaxValidator();
                    if (!validator.Validate(part, out string errorMessage, out List<Validators.SNILValidationError> partErrors))
                    {
                        errors.AddRange(partErrors);
                        isValid = false;
                    }
                }
                
                return isValid;
            }
            catch (Exception e)
            {
                errors.Add(new Validators.SNILValidationError
                {
                    LineNumber = 0,
                    LineContent = "",
                    ErrorType = Validators.SNILValidationErrorType.EmptyFile,
                    Message = $"Import failed: {e.Message}"
                });
                return false;
            }
        }

        public static void ImportScriptWithoutPostProcessing(string filePath)
        {
            ImportScriptInternal(filePath, false);
        }

        private static void ImportScriptInternal(string filePath, bool doPostProcessing)
        {
            try
            {
                filePath = filePath.Trim().Trim('"', '@', '\'');

                if (!File.Exists(filePath))
                {
                    Debug.LogError($"File not found: {filePath}");
                    return;
                }

                // Проверяем, содержит ли файл несколько скриптов
                List<string[]> scriptParts = SNILMultiScriptParser.ParseMultiScript(filePath);
                
                if (scriptParts.Count > 1)
                {
                    // Обрабатываем как многодиалоговый файл
                    ImportMultiScript(scriptParts);
                }
                else
                {
                    // Обрабатываем как обычный файл
                    ImportSingleScript(scriptParts[0]);
                }
                
                // Выполняем пост-обработку для установки всех ссылок
                if (doPostProcessing)
                {
                    SNILPostProcessor.ProcessAllReferences();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Import failed: {e.Message}\n{e.StackTrace}");
            }
        }

        public static List<string> GetAllGraphNamesInFile(string filePath)
        {
            List<string> graphNames = new List<string>();
            
            if (!File.Exists(filePath))
            {
                return graphNames;
            }

            var scriptParts = SNILMultiScriptParser.ParseMultiScript(filePath);
            
            foreach (string[] part in scriptParts)
            {
                // Разбираем функции из скрипта
                var functions = SNILFunctionParser.ParseFunctions(part);
                var mainScriptLines = SNILFunctionParser.ExtractMainScriptWithoutFunctions(part).ToArray();

                string graphName = "NewGraph"; // Заглушка
                foreach (string line in mainScriptLines)
                {
                    var nameMatch = Regex.Match(line.Trim(), @"^name:\s*(.+)", RegexOptions.IgnoreCase);
                    if (nameMatch.Success)
                    {
                        graphName = nameMatch.Groups[1].Value.Trim();
                        break;
                    }
                }
                
                graphNames.Add(graphName);
            }
            
            return graphNames;
        }

        public static void CreateAllGraphsInFile(string filePath)
        {
            var scriptParts = SNILMultiScriptParser.ParseMultiScript(filePath);
            
            foreach (string[] part in scriptParts)
            {
                // Разбираем функции из скрипта
                var functions = SNILFunctionParser.ParseFunctions(part);
                var mainScriptLines = SNILFunctionParser.ExtractMainScriptWithoutFunctions(part).ToArray();

                string graphName = "NewGraph"; // Заглушка
                foreach (string line in mainScriptLines)
                {
                    var nameMatch = Regex.Match(line.Trim(), @"^name:\s*(.+)", RegexOptions.IgnoreCase);
                    if (nameMatch.Success)
                    {
                        graphName = nameMatch.Groups[1].Value.Trim();
                        break;
                    }
                }

                graphName = SanitizeFileName(graphName);
                
                // Проверяем, существует ли уже такой граф
                string assetPath = $"Assets/Resources/Dialogues/{graphName}.asset";
                DialogueGraph graph = AssetDatabase.LoadAssetAtPath<DialogueGraph>(assetPath);
                
                if (graph == null)
                {
                    graph = ScriptableObject.CreateInstance<DialogueGraph>();
                    graph.name = graphName;

                    string folderPath = "Assets/Resources/Dialogues";
                    if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
                    if (!AssetDatabase.IsValidFolder(folderPath)) AssetDatabase.CreateFolder("Assets/Resources", "Dialogues");

                    AssetDatabase.CreateAsset(graph, assetPath);
                    AssetDatabase.SaveAssets();
                }
                
                // Регистрируем граф для пост-обработки
                SNILPostProcessor.RegisterGraph(graphName, graph);
            }
        }

        public static void ProcessAllGraphsInFile(string filePath)
        {
            var scriptParts = SNILMultiScriptParser.ParseMultiScript(filePath);
            
            foreach (string[] part in scriptParts)
            {
                // Разбираем функции из скрипта
                var functions = SNILFunctionParser.ParseFunctions(part);
                var mainScriptLines = SNILFunctionParser.ExtractMainScriptWithoutFunctions(part).ToArray();

                string graphName = "NewGraph"; // Заглушка
                foreach (string line in mainScriptLines)
                {
                    var nameMatch = Regex.Match(line.Trim(), @"^name:\s*(.+)", RegexOptions.IgnoreCase);
                    if (nameMatch.Success)
                    {
                        graphName = nameMatch.Groups[1].Value.Trim();
                        break;
                    }
                }

                graphName = SanitizeFileName(graphName);
                
                // Получаем существующий граф
                string assetPath = $"Assets/Resources/Dialogues/{graphName}.asset";
                DialogueGraph graph = AssetDatabase.LoadAssetAtPath<DialogueGraph>(assetPath);
                
                if (graph == null)
                {
                    Debug.LogError($"Could not load graph: {assetPath}");
                    continue;
                }

                // Сначала создаем ноды функций
                var functionInstructions = ParseFunctionInstructions(functions);
                
                // Затем создаем ноды основного скрипта (включая вызовы функций)
                var mainInstructions = ParseScript(mainScriptLines);
                
                SNILNodeCreator.CreateNodesFromInstructions(graph, mainInstructions, functionInstructions);
            }
        }

        private static List<SNILInstruction> ParseFunctionInstructions(List<SNILFunction> functions)
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

        private static void ImportMultiScript(List<string[]> scriptParts)
        {
            // Сначала создаем все графы
            foreach (string[] part in scriptParts)
            {
                CreateSingleGraph(part);
            }
            
            // Затем создаем ноды для всех графов
            foreach (string[] part in scriptParts)
            {
                ProcessSingleGraph(part);
            }
        }

        private static void CreateSingleGraph(string[] lines)
        {
            if (lines.Length == 0) return;

            // Валидируем синтаксис перед компиляцией
            Validators.SNILSyntaxValidator validator = new Validators.SNILSyntaxValidator();
            if (!validator.Validate(lines, out string errorMessage))
            {
                Debug.LogError($"SNIL script validation failed: {errorMessage}");
                return;
            }

            string graphName = "NewGraph"; // Заглушка
            foreach (string line in lines)
            {
                var nameMatch = Regex.Match(line.Trim(), @"^name:\s*(.+)", RegexOptions.IgnoreCase);
                if (nameMatch.Success)
                {
                    graphName = nameMatch.Groups[1].Value.Trim();
                    break;
                }
            }

            graphName = SanitizeFileName(graphName);
            
            DialogueGraph graph = ScriptableObject.CreateInstance<DialogueGraph>();
            graph.name = graphName;

            string folderPath = "Assets/Resources/Dialogues";
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(folderPath)) AssetDatabase.CreateFolder("Assets/Resources", "Dialogues");

            string assetPath = $"{folderPath}/{graphName}.asset";
            AssetDatabase.CreateAsset(graph, assetPath);

            // Регистрируем граф для пост-обработки
            SNILPostProcessor.RegisterGraph(graphName, graph);
        }

        private static void ProcessSingleGraph(string[] lines)
        {
            if (lines.Length == 0) return;

            string graphName = "NewGraph"; // Заглушка
            foreach (string line in lines)
            {
                var nameMatch = Regex.Match(line.Trim(), @"^name:\s*(.+)", RegexOptions.IgnoreCase);
                if (nameMatch.Success)
                {
                    graphName = nameMatch.Groups[1].Value.Trim();
                    break;
                }
            }

            graphName = SanitizeFileName(graphName);
            
            // Получаем существующий граф
            string assetPath = $"Assets/Resources/Dialogues/{graphName}.asset";
            DialogueGraph graph = AssetDatabase.LoadAssetAtPath<DialogueGraph>(assetPath);
            
            if (graph == null)
            {
                Debug.LogError($"Could not load graph: {assetPath}");
                return;
            }

            // Разбираем функции из скрипта
            var functions = SNILFunctionParser.ParseFunctions(lines);
            var mainScriptLines = SNILFunctionParser.ExtractMainScriptWithoutFunctions(lines).ToArray();

            // Сначала создаем ноды функций
            var functionInstructions = ParseFunctionInstructions(functions);
            
            // Затем создаем ноды основного скрипта (включая вызовы функций)
            var mainInstructions = ParseScript(mainScriptLines);

            SNILNodeCreator.CreateNodesFromInstructions(graph, mainInstructions, functionInstructions);
        }

        private static void ImportSingleScript(string[] lines)
        {
            if (lines.Length == 0) return;

            // Валидируем синтаксис перед компиляцией
            Validators.SNILSyntaxValidator validator = new Validators.SNILSyntaxValidator();
            if (!validator.Validate(lines, out string errorMessage))
            {
                Debug.LogError($"SNIL script validation failed: {errorMessage}");
                return;
            }

            string graphName = "NewGraph"; // Заглушка
            foreach (string line in lines)
            {
                var nameMatch = Regex.Match(line.Trim(), @"^name:\s*(.+)", RegexOptions.IgnoreCase);
                if (nameMatch.Success)
                {
                    graphName = nameMatch.Groups[1].Value.Trim();
                    break;
                }
            }

            graphName = SanitizeFileName(graphName);
            
            DialogueGraph graph = ScriptableObject.CreateInstance<DialogueGraph>();
            graph.name = graphName;

            string folderPath = "Assets/Resources/Dialogues";
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(folderPath)) AssetDatabase.CreateFolder("Assets/Resources", "Dialogues");

            string assetPath = $"{folderPath}/{graphName}.asset";
            AssetDatabase.CreateAsset(graph, assetPath);

            // Регистрируем граф для пост-обработки
            SNILPostProcessor.RegisterGraph(graphName, graph);

            // Разбираем функции из скрипта
            var functions = SNILFunctionParser.ParseFunctions(lines);
            var mainScriptLines = SNILFunctionParser.ExtractMainScriptWithoutFunctions(lines).ToArray();

            // Сначала создаем ноды функций
            var functionInstructions = ParseFunctionInstructions(functions);
            
            // Затем создаем ноды основного скрипта (включая вызовы функций)
            var mainInstructions = ParseScript(mainScriptLines);

            SNILNodeCreator.CreateNodesFromInstructions(graph, mainInstructions, functionInstructions);

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

        private static List<SNILInstruction> ParseScript(string[] lines)
        {
            var templates = SNILTemplateManager.GetNodeTemplates();
            List<SNILInstruction> instructions = new List<SNILInstruction>();

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || IsCommentLine(trimmed)) continue;

                var nameMatch = Regex.Match(trimmed, @"^name:\s*(.+)", RegexOptions.IgnoreCase);
                if (nameMatch.Success) continue;

                // Пропускаем только определения функций и концы, но обрабатываем вызовы функций
                if (trimmed.StartsWith("function ", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Equals("end", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Обрабатываем вызовы функций
                if (trimmed.StartsWith("call ", StringComparison.OrdinalIgnoreCase))
                {
                    string functionName = trimmed.Substring(5).Trim(); // "call ".Length = 5
                    var callInstruction = new SNILInstruction
                    {
                        Type = SNILInstructionType.Generic,
                        NodeTypeName = "CallFunctionNode",
                        Parameters = new Dictionary<string, string> { { "functionName", functionName } },
                        NodeType = SNILTypeResolver.GetNodeType("CallFunctionNode")
                    };
                    
                    instructions.Add(callInstruction);
                    continue;
                }

                var instruction = MatchLineToTemplate(trimmed, templates);
                if (instruction != null)
                {
                    instructions.Add(instruction);
                }
            }

            return instructions;
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

    public class SNILInstruction
    {
        public SNILInstructionType Type { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
        public string NodeTypeName { get; set; }
        public Type NodeType { get; set; }
    }

    public enum SNILInstructionType { Generic }
}