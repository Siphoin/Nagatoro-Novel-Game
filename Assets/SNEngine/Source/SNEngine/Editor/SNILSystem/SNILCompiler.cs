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
        public static bool ImportScript(string filePath)
        {
            // Используем новую систему обработчиков инструкций
            return SNILInstructionBasedCompiler.CompileScript(filePath);
        }

        public static bool ValidateScript(string filePath, out List<SNILValidationError> errors)
        {
            return SNILScriptValidator.ValidateScript(filePath, out errors);
        }

        public static bool ImportScriptWithoutPostProcessing(string filePath)
        {
            return SNILInstructionBasedCompiler.CompileScriptWithoutPostProcessing(filePath);
        }

        public static List<string> GetAllGraphNamesInFile(string filePath)
        {
            return SNILScriptValidator.GetAllGraphNamesInFile(filePath);
        }

        public static bool CreateAllGraphsInFile(string filePath)
        {
            try
            {
                SNILScriptValidator.CreateAllGraphsInFile(filePath);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Create all graphs failed with exception: {ex.Message}");
                return false;
            }
        }

        public static bool ProcessAllGraphsInFile(string filePath)
        {
            var scriptParts = SNILMultiScriptParser.ParseMultiScript(filePath);
            bool allSuccessful = true;

            foreach (string[] part in scriptParts)
            {
                // Используем новую систему обработчиков инструкций
                if (!ProcessSingleScriptPart(part))
                {
                    allSuccessful = false;
                }
            }

            return allSuccessful;
        }

        private static bool ProcessSingleScriptPart(string[] lines)
        {
            bool hasErrors = false;

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
                    hasErrors = true;
                }
            }

            return !hasErrors;
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