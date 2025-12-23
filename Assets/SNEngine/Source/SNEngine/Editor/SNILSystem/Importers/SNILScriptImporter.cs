using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SNEngine.Editor.SNILSystem.Parsers;
using SNEngine.Editor.SNILSystem.Validators;
using SNEngine.Editor.SNILSystem.FunctionSystem;
using UnityEngine;

namespace SNEngine.Editor.SNILSystem.Importers
{
    public class SNILScriptImporter
    {
        public static void ImportScript(string filePath)
        {
            ImportScriptInternal(filePath, true);
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

                // Reload templates to ensure latest changes are used
                SNILTemplateManager.ReloadTemplates();

                List<string[]> scriptParts = SNILMultiScriptParser.ParseMultiScript(filePath);

                if (scriptParts.Count > 1)
                {
                    ImportMultiScript(scriptParts);
                }
                else
                {
                    ImportSingleScript(scriptParts[0]);
                }

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

        private static void ImportMultiScript(List<string[]> scriptParts)
        {
            // Сначала создаем все графы
            foreach (string[] part in scriptParts)
            {
                SNILGraphCreator.CreateSingleGraph(part);
            }

            // Затем создаем ноды для всех графов
            foreach (string[] part in scriptParts)
            {
                SNILScriptProcessor.ProcessSingleGraph(part);
            }
        }

        private static void ImportSingleScript(string[] lines)
        {
            if (lines.Length == 0) return;

            Validators.SNILSyntaxValidator validator = new Validators.SNILSyntaxValidator();
            if (!validator.Validate(lines, out string errorMessage))
            {
                Debug.LogError($"SNIL script validation failed: {errorMessage}");
                return;
            }

            string graphName = SNILScriptProcessor.ExtractGraphName(lines);
            graphName = SanitizeFileName(graphName);

            SNILGraphCreator.CreateGraphAsset(graphName);

            var functions = SNILFunctionParser.ParseFunctions(lines);
            var mainScriptLines = SNILFunctionParser.ExtractMainScriptWithoutFunctions(lines).ToArray();

            var functionInstructions = SNILScriptProcessor.ParseFunctionInstructions(functions);
            var (mainInstructions, functionCallPositions, functionCallNames) = SNILScriptProcessor.ParseScriptWithFunctionCalls(mainScriptLines);

            SNILScriptProcessor.ApplyInstructionsToGraph(graphName, mainInstructions, functionInstructions, functionCallPositions, functionCallNames);
        }

        private static string SanitizeFileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) fileName = fileName.Replace(c, '_');
            return string.IsNullOrWhiteSpace(fileName) ? "NewGraph" : fileName;
        }
    }
}