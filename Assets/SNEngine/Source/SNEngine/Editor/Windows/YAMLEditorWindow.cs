using SNEngine.Editor.Language;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class LanguageYamlEditor : EditorWindow
{
    private LanguageServiceEditor langService;
    private string selectedLanguage;
    private List<string> availableLanguages = new();
    private Dictionary<string, List<string>> languageStructure = new();

    private string selectedFilePath;
    private string yamlText = "";
    private string highlightedYamlText;

    private Vector2 scrollPosFiles;
    private Vector2 scrollPosText;

    private GUIStyle textStyle;
    private GUIStyle numberStyle;
    private GUIStyle selectedFileStyle;

    private Stack<string> undoStack = new();
    private Stack<string> redoStack = new();
    private Dictionary<string, bool> foldouts = new();

    private const float FileBlockWidth = 250f;
    private const int MinFontSize = 10;
    private const int MaxFontSize = 24;
    private int currentFontSize = 14;

    [MenuItem("SNEngine/YAML Editor")]
    static void Open() => GetWindow<LanguageYamlEditor>("Language YAML Editor");

    void OnEnable()
    {
        InitializeGuiStyles();
        InitializeLanguageService();
    }

    private void InitializeGuiStyles()
    {
        textStyle = new GUIStyle(EditorStyles.textArea)
        {
            fontSize = currentFontSize,
            wordWrap = false,
            richText = true,
            padding = new RectOffset(8, 8, 4, 4),
            normal = { textColor = Color.white }
        };
        textStyle.normal.background = null;

        numberStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.UpperRight,
            normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
            fontSize = 13
        };

        selectedFileStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { background = MakeTex(2, 2, new Color(0.2f, 0.4f, 0.7f, 0.5f)), textColor = Color.white },
            hover = { background = MakeTex(2, 2, new Color(0.2f, 0.4f, 0.7f, 0.7f)), textColor = Color.white },
            padding = new RectOffset(4, 4, 2, 2),
            alignment = TextAnchor.MiddleLeft
        };
    }

    // SRP: Инициализация сервиса языков
    private void InitializeLanguageService()
    {
        langService = Resources.Load<LanguageServiceEditor>("Editor/SO/Language Service Editor");
        if (langService == null)
        {
            Debug.LogError("LanguageServiceEditor not found in Resources!");
            return;
        }

        availableLanguages = langService.GetAvailableLanguages().ToList();
        if (availableLanguages.Count > 0)
        {
            selectedLanguage = availableLanguages[0];
            ReloadLanguageStructure();
        }
    }


    private Texture2D MakeTex(int width, int height, Color col)
    {
        var pix = Enumerable.Repeat(col, width * height).ToArray();
        var tex = new Texture2D(width, height);
        tex.SetPixels(pix);
        tex.Apply();
        return tex;
    }

    // --- SRP: Подсветка YAML ---
    private string ApplyYamlHighlighting(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        string highlighted = input;

        highlighted = Regex.Replace(highlighted, @"(#[^\n]*)", "<color=#6AC46A>$1</color>"); // Комментарии

        // Ключи
        highlighted = Regex.Replace(highlighted, @"^(\s*)([^\s#:]+):",
            m => $"{m.Groups[1].Value}<color=#C678DD>{m.Groups[2].Value}</color>:", RegexOptions.Multiline);

        highlighted = Regex.Replace(highlighted, @"(['""][^'""]*['""])", "<color=#E06C75>$1</color>"); // Строки

        // Числа / bool / null
        highlighted = Regex.Replace(highlighted, @"\b(\d+(\.\d+)?|true|false|null)\b", "<color=#E5C07B>$1</color>");

        return highlighted;
    }

    void OnGUI()
    {
        if (langService == null)
        {
            EditorGUILayout.LabelField("LanguageServiceEditor not found.");
            return;
        }

        HandleKeyboardShortcuts();
        DrawToolbar();
        DrawMainBody();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        // Language selection
        EditorGUILayout.LabelField("Language:", GUILayout.Width(70));
        int langIndex = availableLanguages.IndexOf(selectedLanguage);
        int newLangIndex = EditorGUILayout.Popup(langIndex, availableLanguages.ToArray(), GUILayout.Width(120));
        if (newLangIndex >= 0 && selectedLanguage != availableLanguages[newLangIndex])
        {
            selectedLanguage = availableLanguages[newLangIndex];
            ReloadLanguageStructure();
        }

        if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60))) ReloadLanguageStructure();

        // Font size info
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"Font Size: {currentFontSize} (Ctrl+↑/↓)", EditorStyles.miniLabel, GUILayout.Width(150));

        EditorGUILayout.EndHorizontal();
    }

    private void DrawMainBody()
    {
        EditorGUILayout.BeginHorizontal();

        // Left Panel: File list
        EditorGUILayout.BeginVertical(GUILayout.Width(FileBlockWidth));
        scrollPosFiles = EditorGUILayout.BeginScrollView(scrollPosFiles);
        foreach (var kvp in languageStructure) DrawFolder(kvp.Key, kvp.Value);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // Right Panel: Editor
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

        if (!string.IsNullOrEmpty(selectedFilePath))
            DrawYamlEditor();
        else
            EditorGUILayout.LabelField("Выберите файл слева для редактирования", EditorStyles.centeredGreyMiniLabel, GUILayout.ExpandHeight(true));

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawYamlEditor()
    {
        DrawEditorToolbar();

        scrollPosText = EditorGUILayout.BeginScrollView(scrollPosText, GUILayout.ExpandHeight(true));

        string[] lines = yamlText.Split('\n');
        float lineHeight = textStyle.lineHeight + 2;
        float totalHeight = lines.Length * lineHeight;

        GUILayoutUtility.GetRect(position.width - FileBlockWidth - 20, totalHeight, textStyle);
        // Background fill for the editor area
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), new Color(0.11f, 0.11f, 0.11f));

        DrawLineNumbers(lines.Length, lineHeight);

        Rect textRect = new Rect(45, 0, position.width - FileBlockWidth - 60, totalHeight);

        GUI.SetNextControlName("YamlEditor");
        string controlName = GUI.GetNameOfFocusedControl();
        bool isFocused = controlName == "YamlEditor";

        if (!isFocused)
        {
            // Draw highlighted text on top
            string highlighted = ApplyYamlHighlighting(yamlText);
            GUI.Label(textRect, highlighted, textStyle);

            // Hidden TextArea underneath to capture focus/cursor
            Color oldColor = GUI.color;
            GUI.color = new Color(1, 1, 1, 0.01f);
            GUI.TextArea(textRect, yamlText, textStyle);
            GUI.color = oldColor;
        }
        else
        {
            // When focused, use standard TextArea for input
            EditorGUI.BeginChangeCheck();
            string newText = GUI.TextArea(textRect, yamlText, textStyle);
            if (EditorGUI.EndChangeCheck())
            {
                // Only push to undo stack if text actually changed
                if (yamlText != newText)
                {
                    undoStack.Push(yamlText);
                    redoStack.Clear(); // Clear redo stack on new action
                    yamlText = newText;
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawEditorToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60))) LoadFile(selectedFilePath);
        if (GUILayout.Button("Save (Ctrl+S)", EditorStyles.toolbarButton, GUILayout.Width(120))) SaveFile(selectedFilePath);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Undo (Ctrl+Z)", EditorStyles.toolbarButton, GUILayout.Width(80))) HandleUndo();
        if (GUILayout.Button("Redo (Ctrl+Y)", EditorStyles.toolbarButton, GUILayout.Width(80))) HandleRedo();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLineNumbers(int lineCount, float lineHeight)
    {
        for (int i = 0; i < lineCount; i++)
        {
            float y = i * lineHeight;
            // Draw alternating background
            if (i % 2 == 0)
                EditorGUI.DrawRect(new Rect(0, y, position.width, lineHeight), new Color(0.13f, 0.13f, 0.13f));
            // Draw line number
            GUI.Label(new Rect(5, y, 35, lineHeight), (i + 1).ToString(), numberStyle);
        }
    }


    private void HandleKeyboardShortcuts()
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.control)
        {
            switch (e.keyCode)
            {
                case KeyCode.S:
                    if (!string.IsNullOrEmpty(selectedFilePath)) SaveFile(selectedFilePath);
                    e.Use();
                    break;
                case KeyCode.Z:
                    HandleUndo();
                    e.Use();
                    break;
                case KeyCode.Y:
                    HandleRedo();
                    e.Use();
                    break;
                case KeyCode.UpArrow:
                    ChangeFontSize(1);
                    e.Use();
                    break;
                case KeyCode.DownArrow:
                    ChangeFontSize(-1);
                    e.Use();
                    break;
            }
        }
    }

    private void ChangeFontSize(int change)
    {
        int newSize = currentFontSize + change;
        if (newSize >= MinFontSize && newSize <= MaxFontSize)
        {
            currentFontSize = newSize;
            textStyle.fontSize = currentFontSize;
            Repaint();
        }
    }

    private void HandleUndo()
    {
        if (undoStack.Count > 0)
        {
            redoStack.Push(yamlText);
            yamlText = undoStack.Pop();
            highlightedYamlText = ApplyYamlHighlighting(yamlText);
        }
    }

    private void HandleRedo()
    {
        if (redoStack.Count > 0)
        {
            undoStack.Push(yamlText);
            yamlText = redoStack.Pop();
            highlightedYamlText = ApplyYamlHighlighting(yamlText);
        }
    }

    void ReloadLanguageStructure()
    {
        if (string.IsNullOrEmpty(selectedLanguage)) return;

        string langPath = langService.GetLanguagePath(selectedLanguage);
        languageStructure.Clear();

        if (Directory.Exists(langPath))
        {
            // Root files
            var topFiles = Directory.GetFiles(langPath, "*.yaml").Select(Path.GetFileName).ToList();
            languageStructure[langPath] = topFiles;

            // Subdirectories files
            foreach (var dir in Directory.GetDirectories(langPath))
            {
                var files = Directory.GetFiles(dir, "*.yaml").Select(Path.GetFileName).ToList();
                languageStructure[dir] = files;
            }
        }

        selectedFilePath = null;
        yamlText = "";
        foldouts.Clear();
    }

    void DrawFolder(string folderPath, List<string> files)
    {
        string folderName = Path.GetFileName(folderPath);
        if (Path.GetFileName(Path.GetDirectoryName(folderPath)) == selectedLanguage)
            folderName = "Root";

        if (!foldouts.ContainsKey(folderPath)) foldouts[folderPath] = true;
        foldouts[folderPath] = EditorGUILayout.Foldout(foldouts[folderPath], folderName);

        if (foldouts[folderPath])
        {
            EditorGUI.indentLevel++;
            foreach (var file in files)
            {
                string fullPath = Path.Combine(folderPath, file);
                GUIStyle style = fullPath == selectedFilePath ? selectedFileStyle : EditorStyles.label;
                if (GUILayout.Button(file, style))
                {
                    selectedFilePath = fullPath;
                    LoadFile(selectedFilePath);
                }
            }
            EditorGUI.indentLevel--;
        }
    }

    void LoadFile(string file)
    {
        if (File.Exists(file))
            yamlText = File.ReadAllText(file);
        else
        {
            yamlText = "";
            Debug.LogWarning($"File not found: {file}");
        }
        scrollPosText = Vector2.zero;
        undoStack.Clear();
        redoStack.Clear();

        // Clear focus to ensure the highlighted view is immediately drawn
        GUI.FocusControl("");

        Repaint();
    }

    void SaveFile(string file)
    {
        if (string.IsNullOrEmpty(file))
        {
            Debug.LogWarning("No file path selected for saving.");
            return;
        }

        try
        {
            File.WriteAllText(file, yamlText);
            AssetDatabase.Refresh();
            Debug.Log($"File saved successfully: {Path.GetFileName(file)}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save file: {ex.Message}");
        }
    }
}