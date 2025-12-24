using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SNEngine.Editor.SNILSystem.FunctionSystem;
using SNEngine.Editor.SNILSystem.InstructionHandlers;
using SNEngine.Editor.SNILSystem.Parsers;
using SNEngine.Editor.SNILSystem.Validators;
using SNEngine.Graphs;
using UnityEngine;

namespace SNEngine.Editor.SNILSystem
{
    public class SNILInstructionBasedCompiler
    {
        public static void CompileScript(string filePath)
        {
            CompileScriptInternal(filePath, true);
        }

        public static void CompileScriptWithoutPostProcessing(string filePath)
        {
            CompileScriptInternal(filePath, false);
        }

        private static void CompileScriptInternal(string filePath, bool doPostProcessing)
        {
            try
            {
                filePath = filePath.Trim().Trim('"', '@', '\'');

                if (!File.Exists(filePath))
                {
                    Debug.LogError($"File not found: {filePath}");
                    return;
                }

                // Reload templates to ensure latest changes are used
                SNILTemplateManager.ReloadTemplates();

                List<string[]> scriptParts = SNILMultiScriptParser.ParseMultiScript(filePath);

                if (scriptParts.Count > 1)
                {
                    CompileMultiScript(scriptParts);
                }
                else
                {
                    CompileSingleScript(scriptParts[0]);
                }

                if (doPostProcessing)
                {
                    SNILPostProcessor.ProcessAllReferences();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Compilation failed: {e.Message}\n{e.StackTrace}");
            }
        }

        private static void CompileMultiScript(List<string[]> scriptParts)
        {
            foreach (string[] part in scriptParts)
            {
                CompileSingleScript(part);
            }
        }

        private static void CompileSingleScript(string[] lines)
        {
            if (lines.Length == 0) return;

            // Валидация
            Validators.SNILSyntaxValidator validator = new Validators.SNILSyntaxValidator();
            if (!validator.Validate(lines, out string errorMessage))
            {
                Debug.LogError($"SNIL script validation failed: {errorMessage}");
                return;
            }

            // Извлекаем функции и основной скрипт один раз
            var functions = SNILFunctionParser.ParseFunctions(lines);
            var mainScriptLines = SNILFunctionParser.ExtractMainScriptWithoutFunctions(lines).ToArray();

            // Создаем контекст выполнения
            var context = new InstructionContext();

            // Регистрируем все функции в контексте
            foreach (var function in functions)
            {
                if (!context.Functions.ContainsKey(function.Name))
                    context.Functions.Add(function.Name, function);
                else
                    context.Functions[function.Name] = function;
            }

            // Обрабатываем основной скрипт (после регистрации функций)
            foreach (string line in mainScriptLines)
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

            // После обработки всех инструкций, соединяем ноды последовательно
            if (context.Graph != null)
            {
                var dialogueGraph = (DialogueGraph)context.Graph;
                NodeConnectionUtility.ConnectNodesSequentially(dialogueGraph, context.Nodes);
            }
        }

        private static bool IsCommentLine(string line)
        {
            string trimmed = line.Trim();
            return trimmed.StartsWith("//") || trimmed.StartsWith("#");
        }
    }
}