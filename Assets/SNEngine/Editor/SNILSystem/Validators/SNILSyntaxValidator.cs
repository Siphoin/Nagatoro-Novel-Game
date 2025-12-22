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
                            !IsCommentLine(line))
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
            return Validate(lines, out errorMessage, out _);
        }

        public bool Validate(string[] lines, out string errorMessage, out List<SNILValidationError> errors)
        {
            errors = new List<SNILValidationError>();
            errorMessage = "";

            if (lines == null || lines.Length == 0)
            {
                errors.Add(new SNILValidationError
                {
                    LineNumber = 0,
                    LineContent = "",
                    ErrorType = SNILValidationErrorType.EmptyFile,
                    Message = "SNIL script is empty."
                });
                return false;
            }

            // Проверяем синтаксис функций
            ValidateFunctions(lines, errors);

            // Разделяем строки на основной скрипт (вне функций) и тела функций
            var mainScriptLines = ExtractMainScriptWithoutFunctions(lines);

            // Проверяем наличие директивы name в основном скрипте:
            bool hasNameDirective = false;
            for (int i = 0; i < mainScriptLines.Length; i++)
            {
                if (Regex.IsMatch(mainScriptLines[i].Trim(), @"^name:\s*.+", RegexOptions.IgnoreCase))
                {
                    hasNameDirective = true;
                    break;
                }
            }

            if (!hasNameDirective)
            {
                errors.Add(new SNILValidationError
                {
                    LineNumber = 0,
                    LineContent = "No name directive found",
                    ErrorType = SNILValidationErrorType.MissingNameDirective,
                    Message = "Missing 'name:' directive in script."
                });
            }

            // Проверяем существование нод в строках основного скрипта (кроме служебных и комментариев)
            List<ContentLineInfo> contentLines = new List<ContentLineInfo>();
            for (int i = 0; i < mainScriptLines.Length; i++)
            {
                string trimmed = mainScriptLines[i].Trim();
                if (!string.IsNullOrEmpty(trimmed) && 
                    !IsCommentLine(trimmed) && 
                    !trimmed.StartsWith("name:", StringComparison.OrdinalIgnoreCase) &&
                    !trimmed.StartsWith("worker:", StringComparison.OrdinalIgnoreCase))
                {
                    contentLines.Add(new ContentLineInfo { LineIndex = GetOriginalLineIndex(lines, mainScriptLines[i], 0), LineContent = trimmed });
                }
            }

            if (contentLines.Count == 0)
            {
                errors.Add(new SNILValidationError
                {
                    LineNumber = 0,
                    LineContent = "No content lines found",
                    ErrorType = SNILValidationErrorType.NoContent,
                    Message = "No content lines found in script."
                });
                errorMessage = string.Join("\n", errors.Select(e => e.ToString()));
                return false;
            }

            // Проверяем, что первая строка основного скрипта - это Start
            if (contentLines.Count > 0 && contentLines[0].LineContent != "Start")
            {
                errors.Add(new SNILValidationError
                {
                    LineNumber = contentLines[0].LineIndex + 1,
                    LineContent = contentLines[0].LineContent,
                    ErrorType = SNILValidationErrorType.InvalidStart,
                    Message = "Script must start with 'Start' line."
                });
            }

            // Проверяем, что последняя строка основного скрипта - это End или JumpTo
            if (contentLines.Count > 0)
            {
                ContentLineInfo lastLine = contentLines[contentLines.Count - 1];
                bool endsWithValidExit = lastLine.LineContent.Equals("End", StringComparison.OrdinalIgnoreCase) ||
                                       lastLine.LineContent.StartsWith("Jump To ", StringComparison.OrdinalIgnoreCase);

                if (!endsWithValidExit)
                {
                    errors.Add(new SNILValidationError
                    {
                        LineNumber = lastLine.LineIndex + 1,
                        LineContent = lastLine.LineContent,
                        ErrorType = SNILValidationErrorType.InvalidEnd,
                        Message = "Script must end with 'End' or 'Jump To [dialogue_name]' line."
                    });
                }
            }

            // Проверяем существование нод в строках основного скрипта
            for (int i = 0; i < contentLines.Count; i++)
            {
                ContentLineInfo lineInfo = contentLines[i];
                bool isValidNode = false;

                // Пропускаем служебные строки
                if (lineInfo.LineContent.Equals("Start", StringComparison.OrdinalIgnoreCase) ||
                    lineInfo.LineContent.Equals("End", StringComparison.OrdinalIgnoreCase) ||
                    lineInfo.LineContent.StartsWith("Jump To ", StringComparison.OrdinalIgnoreCase) ||
                    lineInfo.LineContent.StartsWith("call ", StringComparison.OrdinalIgnoreCase))
                {
                    isValidNode = true; // Эти строки валидны
                }
                else
                {
                    foreach (var template in _nodeTemplates)
                    {
                        if (SNILTemplateMatcher.MatchLineWithTemplate(lineInfo.LineContent, template.Value) != null)
                        {
                            isValidNode = true;
                            break;
                        }
                    }
                }

                if (!isValidNode)
                {
                    errors.Add(new SNILValidationError
                    {
                        LineNumber = lineInfo.LineIndex + 1,
                        LineContent = lineInfo.LineContent,
                        ErrorType = SNILValidationErrorType.UnknownNode,
                        Message = $"Unknown node format: '{lineInfo.LineContent}'"
                    });
                }
            }

            if (errors.Count > 0)
            {
                errorMessage = string.Join("\n", errors.Select(e => e.ToString()));
                return false;
            }

            return true;
        }

        private string[] ExtractMainScriptWithoutFunctions(string[] lines)
        {
            List<string> mainScript = new List<string>();
            bool insideFunction = false;

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("function ", StringComparison.OrdinalIgnoreCase))
                {
                    insideFunction = true;
                    // Добавляем строку function в основной скрипт как отдельную инструкцию
                    mainScript.Add(line);
                }
                else if (trimmedLine.Equals("end", StringComparison.OrdinalIgnoreCase) && insideFunction)
                {
                    insideFunction = false;
                    // Добавляем строку end в основной скрипт как отдельную инструкцию
                    mainScript.Add(line);
                }
                else if (!insideFunction)
                {
                    // Добавляем строку в основной скрипт только если мы не внутри функции
                    mainScript.Add(line);
                }
                // Строки внутри функций игнорируются при проверке основного скрипта
            }

            return mainScript.ToArray();
        }

        private void ValidateFunctions(string[] lines, List<SNILValidationError> errors)
        {
            int functionDepth = 0;
            int functionStartLine = -1;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (line.StartsWith("function ", StringComparison.OrdinalIgnoreCase))
                {
                    if (functionDepth > 0)
                    {
                        errors.Add(new SNILValidationError
                        {
                            LineNumber = i + 1,
                            LineContent = line,
                            ErrorType = SNILValidationErrorType.InvalidFunctionDefinition,
                            Message = "Nested functions are not allowed."
                        });
                    }
                    else
                    {
                        functionDepth++;
                        functionStartLine = i;
                        
                        // Проверяем, что после "function" идет имя функции
                        string functionName = line.Substring(9).Trim();
                        if (string.IsNullOrEmpty(functionName))
                        {
                            errors.Add(new SNILValidationError
                            {
                                LineNumber = i + 1,
                                LineContent = line,
                                ErrorType = SNILValidationErrorType.InvalidFunctionDefinition,
                                Message = "Function name is required after 'function' keyword."
                            });
                        }
                    }
                }
                else if (line.Equals("end", StringComparison.OrdinalIgnoreCase))
                {
                    if (functionDepth == 0)
                    {
                        errors.Add(new SNILValidationError
                        {
                            LineNumber = i + 1,
                            LineContent = line,
                            ErrorType = SNILValidationErrorType.InvalidFunctionDefinition,
                            Message = "'end' statement without matching 'function'."
                        });
                    }
                    else
                    {
                        functionDepth--;
                    }
                }
            }

            if (functionDepth > 0)
            {
                errors.Add(new SNILValidationError
                {
                    LineNumber = functionStartLine + 1,
                    LineContent = lines[functionStartLine].Trim(),
                    ErrorType = SNILValidationErrorType.FunctionNotClosed,
                    Message = "Function definition is not closed with 'end' statement."
                });
            }
        }
        
        private static bool IsCommentLine(string line)
        {
            string trimmed = line.Trim();
            return trimmed.StartsWith("//") || trimmed.StartsWith("#");
        }
        
        private static bool IsFunctionLine(string line)
        {
            string trimmed = line.Trim();
            return trimmed.StartsWith("function ", StringComparison.OrdinalIgnoreCase) ||
                   trimmed.Equals("end", StringComparison.OrdinalIgnoreCase);
        }
        
        private static int GetOriginalLineIndex(string[] originalLines, string content, int startIndex)
        {
            for (int i = startIndex; i < originalLines.Length; i++)
            {
                if (originalLines[i].Trim() == content.Trim())
                {
                    return i;
                }
            }
            return startIndex; // fallback
        }
    }

    public class SNILValidationError
    {
        public int LineNumber { get; set; }
        public string LineContent { get; set; }
        public SNILValidationErrorType ErrorType { get; set; }
        public string Message { get; set; }

        public override string ToString()
        {
            return $"Line {LineNumber}: {ErrorType} - {Message} (Content: '{LineContent}')";
        }
    }

    public enum SNILValidationErrorType
    {
        EmptyFile,
        MissingNameDirective,
        NoContent,
        InvalidStart,
        InvalidEnd,
        UnknownNode,
        InvalidJumpToFormat,
        InvalidFunctionDefinition,
        FunctionNotClosed
    }

    internal class ContentLineInfo
    {
        public int LineIndex { get; set; }
        public string LineContent { get; set; }
    }
}