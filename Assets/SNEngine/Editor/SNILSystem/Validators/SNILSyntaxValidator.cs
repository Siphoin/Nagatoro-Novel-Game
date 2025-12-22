using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SNEngine.Editor.SNILSystem.Validators
{
    public class SNILSyntaxValidator : SNILValidator
    {
        private readonly Dictionary<string, string> _nodeTemplates;

        public SNILSyntaxValidator()
        {
            // Загружаем шаблоны для проверки существования нод
            _nodeTemplates = new Dictionary<string, string>();
            
            string snilDirectory = "Assets/SNEngine/Source/SNEngine/Editor/SNIL";
            if (Directory.Exists(snilDirectory))
            {
                string[] templateFiles = Directory.GetFiles(snilDirectory, "*.snil");

                foreach (string templateFile in templateFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(templateFile);
                    if (fileName.EndsWith(".cs")) fileName = Path.GetFileNameWithoutExtension(fileName);

                    string[] lines = File.ReadAllLines(templateFile);
                    string templateContent = "";

                    foreach (string line in lines)
                    {
                        if (!line.StartsWith("worker:", StringComparison.OrdinalIgnoreCase) && 
                            !string.IsNullOrEmpty(line.Trim()) && 
                            !line.StartsWith("//"))
                        {
                            templateContent = line; // Берём первую непустую строку как шаблон
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(templateContent))
                    {
                        _nodeTemplates[fileName] = templateContent;
                    }
                }
            }
        }

        public override bool Validate(string[] lines, out string errorMessage)
        {
            errorMessage = "";

            if (lines == null || lines.Length == 0)
            {
                errorMessage = "SNIL script is empty.";
                return false;
            }

            // Проверяем наличие директивы name:
            bool hasNameDirective = false;
            foreach (string line in lines)
            {
                if (Regex.IsMatch(line.Trim(), @"^name:\s*.+", RegexOptions.IgnoreCase))
                {
                    hasNameDirective = true;
                    break;
                }
            }

            if (!hasNameDirective)
            {
                errorMessage = "Missing 'name:' directive in script.";
                return false;
            }

            // Проверяем существование нод в строках (кроме служебных)
            List<string> contentLines = new List<string>();
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed) && 
                    !trimmed.StartsWith("//") && 
                    !trimmed.StartsWith("name:", StringComparison.OrdinalIgnoreCase) &&
                    !trimmed.StartsWith("worker:", StringComparison.OrdinalIgnoreCase))
                {
                    contentLines.Add(trimmed);
                }
            }

            if (contentLines.Count == 0)
            {
                errorMessage = "No content lines found in script.";
                return false;
            }

            // Проверяем, что первая строка - это Start
            if (!contentLines[0].Equals("Start", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "Script must start with 'Start' line.";
                return false;
            }

            // Проверяем, что последняя строка - это End или JumpTo
            string lastLine = contentLines[contentLines.Count - 1];
            bool endsWithValidExit = lastLine.Equals("End", StringComparison.OrdinalIgnoreCase) ||
                                   lastLine.StartsWith("Jump To ", StringComparison.OrdinalIgnoreCase);

            if (!endsWithValidExit)
            {
                errorMessage = "Script must end with 'End' or 'Jump To [dialogue_name]' line.";
                return false;
            }

            // Проверяем существование нод в строках
            for (int i = 1; i < contentLines.Count - 1; i++) // Пропускаем первую (Start) и последнюю (End/JumpTo)
            {
                string line = contentLines[i];
                bool isValidNode = false;

                foreach (var template in _nodeTemplates)
                {
                    if (SNILTemplateMatcher.MatchLineWithTemplate(line, template.Value) != null)
                    {
                        isValidNode = true;
                        break;
                    }
                }

                if (!isValidNode)
                {
                    errorMessage = $"Unknown node format at line {i + 1}: '{line}'";
                    return false;
                }
            }

            // Если последняя строка - JumpTo, проверяем её формат
            if (lastLine.StartsWith("Jump To ", StringComparison.OrdinalIgnoreCase))
            {
                // Принимаем оба формата: 'Jump To [dialogue_name]' и 'Jump To dialogue_name'
                if (!Regex.IsMatch(lastLine, @"^Jump To \[.*\]$") && 
                    !Regex.IsMatch(lastLine, @"^Jump To [^$]+$"))
                {
                    errorMessage = $"Invalid Jump To format at last line: '{lastLine}'. Expected format: 'Jump To [dialogue_name]' or 'Jump To dialogue_name'";
                    return false;
                }
            }

            return true;
        }
    }
}