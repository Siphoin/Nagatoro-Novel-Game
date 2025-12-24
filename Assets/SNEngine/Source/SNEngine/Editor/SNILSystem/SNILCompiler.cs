using System;
using System.Collections.Generic;
using SNEngine.Editor.SNILSystem.Importers;
using SNEngine.Editor.SNILSystem.InstructionHandlers;
using SNEngine.Editor.SNILSystem.Parsers;
using SNEngine.Editor.SNILSystem.Validators;
using UnityEngine;

namespace SNEngine.Editor.SNILSystem
{
    public class SNILCompiler
    {
        public static void ImportScript(string filePath)
        {
            // Используем новую систему обработчиков инструкций
            SNILInstructionBasedCompiler.CompileScript(filePath);
        }

        public static bool ValidateScript(string filePath, out List<SNILValidationError> errors)
        {
            return SNILScriptValidator.ValidateScript(filePath, out errors);
        }

        public static void ImportScriptWithoutPostProcessing(string filePath)
        {
            SNILInstructionBasedCompiler.CompileScriptWithoutPostProcessing(filePath);
        }

        public static List<string> GetAllGraphNamesInFile(string filePath)
        {
            return SNILScriptValidator.GetAllGraphNamesInFile(filePath);
        }

        public static void CreateAllGraphsInFile(string filePath)
        {
            SNILScriptValidator.CreateAllGraphsInFile(filePath);
        }

        public static void ProcessAllGraphsInFile(string filePath)
        {
            var scriptParts = SNILMultiScriptParser.ParseMultiScript(filePath);

            foreach (string[] part in scriptParts)
            {
                // Используем новую систему обработчиков инструкций
                ProcessSingleScriptPart(part);
            }
        }

        private static void ProcessSingleScriptPart(string[] lines)
        {
            // Создаем контекст выполнения
            var context = new InstructionContext();

            // Обрабатываем каждую инструкцию
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                if (string.IsNullOrEmpty(trimmedLine) || IsCommentLine(trimmedLine))
                    continue;

                // Используем менеджер обработчиков для обработки инструкции
                var result = InstructionHandlerManager.Instance.ProcessInstruction(trimmedLine, context);

                if (!result.Success)
                {
                    Debug.LogError($"Failed to process instruction '{trimmedLine}': {result.ErrorMessage}");
                }
            }
        }

        private static bool IsCommentLine(string line)
        {
            string trimmed = line.Trim();
            return trimmed.StartsWith("//") || trimmed.StartsWith("#");
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