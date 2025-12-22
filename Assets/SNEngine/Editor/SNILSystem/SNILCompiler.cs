using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SiphoinUnityHelpers.XNodeExtensions;
using SNEngine.Graphs;
using SNEngine.DialogSystem;
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
                SNILPostProcessor.ProcessAllReferences();
            }
            catch (Exception e)
            {
                Debug.LogError($"Import failed: {e.Message}\n{e.StackTrace}");
            }
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
            SNILSyntaxValidator validator = new SNILSyntaxValidator();
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

            var instructions = ParseScript(lines);
            SNILNodeCreator.CreateNodesFromInstructions(graph, instructions);

            EditorUtility.SetDirty(graph);
        }

        private static void ImportSingleScript(string[] lines)
        {
            if (lines.Length == 0) return;

            // Валидируем синтаксис перед компиляцией
            SNILSyntaxValidator validator = new SNILSyntaxValidator();
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

            var instructions = ParseScript(lines);
            SNILNodeCreator.CreateNodesFromInstructions(graph, instructions);

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
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//")) continue;

                var nameMatch = Regex.Match(trimmed, @"^name:\s*(.+)", RegexOptions.IgnoreCase);
                if (nameMatch.Success) continue;

                var instruction = MatchLineToTemplate(trimmed, templates);
                if (instruction != null)
                {
                    instructions.Add(instruction);
                }
            }

            return instructions;
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