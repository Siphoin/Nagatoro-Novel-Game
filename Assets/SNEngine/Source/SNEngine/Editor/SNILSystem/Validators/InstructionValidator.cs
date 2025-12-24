using System;
using System.Collections.Generic;
using System.Linq;

namespace SNEngine.Editor.SNILSystem.Validators
{
    public class InstructionValidator
    {
        public static List<SNILValidationError> ValidateInstructions(string[] lines)
        {
            var errors = new List<SNILValidationError>();
            
            var mainScriptLines = ScriptLineExtractor.ExtractMainScriptWithoutFunctions(lines);

            for (int i = 0; i < mainScriptLines.Length; i++)
            {
                string line = mainScriptLines[i];
                string trimmedLine = line.Trim();

                if (string.IsNullOrEmpty(trimmedLine) || IsCommentLine(trimmedLine))
                    continue;

                // Получаем оригинальный индекс строки в исходном массиве
                int originalLineIndex = GetOriginalLineIndex(lines, line, 0);

                // Проверяем специальные инструкции
                if (System.Text.RegularExpressions.Regex.IsMatch(trimmedLine, @"^name:\s*.+", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    // Уже проверили в NameDirectiveValidator
                    continue;
                }
                else if (trimmedLine.Equals("Start", System.StringComparison.OrdinalIgnoreCase))
                {
                    // Уже проверили в NameDirectiveValidator
                    continue;
                }
                else if (trimmedLine.Equals("End", System.StringComparison.OrdinalIgnoreCase))
                {
                    // Allow 'End' inside blocks (e.g., inside If Show Variant branches). Only enforce 'End' to be last
                    // when it's a top-level script terminator.
                    if (!IsLineInsideBlock(mainScriptLines, i) && !IsLastSignificantLine(mainScriptLines, i))
                    {
                        errors.Add(new SNILValidationError
                        {
                            LineNumber = originalLineIndex + 1,
                            LineContent = trimmedLine,
                            ErrorType = SNILValidationErrorType.InvalidEnd,
                            Message = "Script must end with 'End' line."
                        });
                    }
                }
                else if (trimmedLine.StartsWith("Jump To ", System.StringComparison.OrdinalIgnoreCase))
                {
                    // Проверяем, что это последняя значимая строка
                    if (!IsLastSignificantLine(mainScriptLines, i))
                    {
                        errors.Add(new SNILValidationError
                        {
                            LineNumber = originalLineIndex + 1,
                            LineContent = trimmedLine,
                            ErrorType = SNILValidationErrorType.InvalidEnd,
                            Message = "Script with 'Jump To' must end with this line."
                        });
                    }
                }
                else if (trimmedLine.StartsWith("call ", System.StringComparison.OrdinalIgnoreCase))
                {
                    // Проверяем формат вызова функции
                    string functionName = trimmedLine.Substring(5).Trim();
                    if (string.IsNullOrEmpty(functionName))
                    {
                        errors.Add(new SNILValidationError
                        {
                            LineNumber = originalLineIndex + 1,
                            LineContent = trimmedLine,
                            ErrorType = SNILValidationErrorType.InvalidFunctionDefinition,
                            Message = "Function name is required after 'call' keyword."
                        });
                    }
                }
                else if (trimmedLine.StartsWith("function ", System.StringComparison.OrdinalIgnoreCase))
                {
                    // Уже обработали в FunctionValidator
                    continue;
                }
                else if (trimmedLine.Equals("end", System.StringComparison.Ordinal))
                {
                    // Уже обработали в FunctionValidator
                    continue;
                }
                else
                {
                    // Для всех остальных инструкций используем систему валидаторов
                    var validationResult = InstructionValidatorManager.Instance.ValidateInstruction(trimmedLine);
                    if (!validationResult.IsValid)
                    {
                        errors.Add(new SNILValidationError
                        {
                            LineNumber = originalLineIndex + 1,
                            LineContent = trimmedLine,
                            ErrorType = SNILValidationErrorType.UnknownNode,
                            Message = validationResult.ErrorMessage
                        });
                    }
                }
            }

            // Ensure the top-level script ends with an End or Jump To (End inside blocks is allowed)
            int lastSignificantIdx = -1;
            for (int i = mainScriptLines.Length - 1; i >= 0; i--)
            {
                var t = mainScriptLines[i].Trim();
                if (!string.IsNullOrEmpty(t) && !IsCommentLine(t))
                {
                    lastSignificantIdx = i;
                    break;
                }
            }

            if (lastSignificantIdx == -1)
            {
                errors.Add(new SNILValidationError
                {
                    LineNumber = 0,
                    LineContent = "",
                    ErrorType = SNILValidationErrorType.NoContent,
                    Message = "Script contains no content."
                });
            }
            else
            {
                var lastTrim = mainScriptLines[lastSignificantIdx].Trim();
                bool ok = false;
                if (lastTrim.StartsWith("Jump To ", StringComparison.OrdinalIgnoreCase)) ok = true;
                if (lastTrim.Equals("End", StringComparison.OrdinalIgnoreCase) && !IsLineInsideBlock(mainScriptLines, lastSignificantIdx)) ok = true;

                if (!ok)
                {
                    // If the script ends with a top-level 'If Show Variant' block that itself guarantees termination
                    // (all its branches end with 'End' or 'Jump To'), consider it valid.
                    if (lastTrim.Equals("endif", StringComparison.OrdinalIgnoreCase))
                    {
                        int ifStartIdx = FindMatchingIfStart(mainScriptLines, lastSignificantIdx);
                        if (ifStartIdx >= 0 && IsIfBlockTerminating(mainScriptLines, ifStartIdx, lastSignificantIdx))
                        {
                            ok = true;
                        }
                    }
                }

                if (!ok)
                {
                    int originalLineIndex = GetOriginalLineIndex(lines, mainScriptLines[lastSignificantIdx], 0);
                    errors.Add(new SNILValidationError
                    {
                        LineNumber = originalLineIndex + 1,
                        LineContent = mainScriptLines[lastSignificantIdx].Trim(),
                        ErrorType = SNILValidationErrorType.InvalidEnd,
                        Message = "Script must end with a top-level 'End' or 'Jump To' instruction."
                    });
                }
            }

            return errors;
        }

        private static bool IsLastSignificantLine(string[] lines, int currentIndex)
        {
            for (int i = currentIndex + 1; i < lines.Length; i++)
            {
                string trimmed = lines[i].Trim();
                if (!string.IsNullOrEmpty(trimmed) && !IsCommentLine(trimmed))
                {
                    return false; // Найдена еще одна значимая строка
                }
            }
            return true; // Это последняя значимая строка
        }

        private static bool IsCommentLine(string line)
        {
            string trimmed = line.Trim();
            return trimmed.StartsWith("//") || trimmed.StartsWith("#");
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

        private static bool IsLineInsideBlock(string[] lines, int index)
        {
            // Simple block stack: increment on encountering a block start (e.g. "If Show Variant"), decrement on "endif"
            int open = 0;
            for (int i = 0; i <= index; i++)
            {
                var t = lines[i].Trim();
                if (t.Equals("If Show Variant", StringComparison.OrdinalIgnoreCase)) open++;
                if (t.Equals("endif", StringComparison.OrdinalIgnoreCase) && open > 0) open--;
            }
            return open > 0;
        }

        private static int FindMatchingIfStart(string[] lines, int endifIndex)
        {
            int depth = 0;
            for (int i = endifIndex; i >= 0; i--)
            {
                var t = lines[i].Trim();
                if (t.Equals("endif", StringComparison.OrdinalIgnoreCase))
                {
                    depth++;
                }
                else if (t.Equals("If Show Variant", StringComparison.OrdinalIgnoreCase))
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }

            return -1; // not found
        }

        private static bool IsIfBlockTerminating(string[] lines, int ifStart, int endifIndex)
        {
            // We consider the block terminating if every branch present (True/False or variant-named sections) ends with 'End' or 'Jump To'
            var branches = new List<(int start, int end)>();
            int i = ifStart + 1;
            // Skip Variants: section
            while (i < endifIndex && (string.IsNullOrWhiteSpace(lines[i]) || lines[i].TrimStart().StartsWith("//") || lines[i].TrimStart().StartsWith("#"))) i++;
            if (i < endifIndex && lines[i].Trim().StartsWith("Variants", StringComparison.OrdinalIgnoreCase))
            {
                i++; // skip header
                while (i < endifIndex)
                {
                    var t = lines[i].Trim();
                    if (string.IsNullOrEmpty(t) || t.StartsWith("//") || t.StartsWith("#")) { i++; continue; }
                    if (t.EndsWith(":")) break; // next section header
                    i++;
                }
            }

            // Now collect sections until endif
            while (i < endifIndex)
            {
                var t = lines[i].Trim();
                if (string.IsNullOrEmpty(t) || t.StartsWith("//") || t.StartsWith("#")) { i++; continue; }
                if (t.EndsWith(":"))
                {
                    int sectionStart = i + 1;
                    var header = t.Substring(0, t.Length - 1).Trim();
                    // collect until next header or endif
                    int j = sectionStart;
                    int nestedIf = 0;
                    int lastSignificant = -1;
                    while (j < endifIndex)
                    {
                        var line = lines[j].Trim();
                        if (line.Equals("If Show Variant", StringComparison.OrdinalIgnoreCase)) { nestedIf++; }
                        else if (line.Equals("endif", StringComparison.OrdinalIgnoreCase))
                        {
                            if (nestedIf > 0) { nestedIf--; }
                            else break; // this would be handled by outer loop
                        }

                        if (nestedIf == 0 && !string.IsNullOrEmpty(line) && !line.StartsWith("//") && !line.StartsWith("#") && !line.EndsWith(":"))
                        {
                            lastSignificant = j;
                        }

                        // stop when we see next section header at nesting 0
                        if (nestedIf == 0 && j + 1 < endifIndex && lines[j + 1].Trim().EndsWith(":")) { j++; break; }

                        j++;
                    }

                    if (lastSignificant == -1)
                    {
                        // empty branch -> not terminating
                        return false;
                    }

                    var last = lines[lastSignificant].Trim();
                    if (!(last.Equals("End", StringComparison.OrdinalIgnoreCase) || last.StartsWith("Jump To ", StringComparison.OrdinalIgnoreCase)))
                    {
                        return false; // branch doesn't end properly
                    }

                    i = j + 1;
                    continue;
                }

                // Unexpected lines between sections - skip
                i++;
            }

            // all branches checked
            return true;
        }
    }
}