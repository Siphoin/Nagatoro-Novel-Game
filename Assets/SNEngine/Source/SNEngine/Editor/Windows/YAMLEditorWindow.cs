using SharpYaml.Serialization;
using SNEngine.Editor.Language;
using SNEngine.Editor.Windows.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class YamlTab
{
    public string FilePath { get; private set; }
    public string YamlText { get; set; }
    public bool IsDirty { get; set; } = false;
    public Vector2 ScrollPosText { get; set; } = Vector2.zero;
    public Stack<string> UndoStack { get; private set; } = new Stack<string>();
    public Stack<string> RedoStack { get; private set; } = new Stack<string>();

    public YamlTab(string filePath, string yamlText)
    {
        FilePath = filePath;
        YamlText = yamlText;
        UndoStack.Push(yamlText);
    }
}

public class YAMLEditorWindow : EditorWindow
{
    private LanguageServiceEditor _langService;
    private string _selectedLanguage;
    private List<string> _availableLanguages = new();
    private Dictionary<string, List<string>> _languageStructure = new();
    private string _rootLangPathNormalized;

    private List<YamlTab> _openTabs = new();
    private int _currentTabIndex = -1;
    private YamlTab CurrentTab => _currentTabIndex >= 0 && _currentTabIndex < _openTabs.Count ? _openTabs[_currentTabIndex] : null;

    private Vector2 _scrollPosFiles;
    private GUIStyle _textStyle;
    private GUIStyle _numberStyle;
    private GUIStyle _previewStyle;
    private GUIStyle _selectedFileStyle;
    private YamlSyntaxStyle _syntaxStyle;

    private string _lastInputText = "";
    private string _searchQuery = "";
    private Dictionary<string, bool> _foldouts = new();

    private const float FileBlockWidth = 250f;
    private const float TabHeight = 24f;
    private const int MinFontSize = 10;
    private const int MaxFontSize = 24;
    private int _currentFontSize = 14;

    private string _notificationText = "";
    private Color _notificationColor = Color.green;
    private double _notificationEndTime = 0;
    private const float NotificationDuration = 3.0f;
    private bool _hasLanguages = false;

    [MenuItem("SNEngine/YAML Editor")]
    private static void Open() => GetWindow<YAMLEditorWindow>("YAML Editor");

    private void OnEnable()
    {
        LoadSyntaxStyles();
        InitializeGuiStyles();
        InitializeLanguageService();
    }


    private void LoadSyntaxStyles()
    {
        _syntaxStyle = new YamlSyntaxStyle();
        TextAsset styleAsset = Resources.Load<TextAsset>("Editor/YamlEditor/styles");

        if (styleAsset == null)
        {
            Debug.LogWarning("styles.yaml not found. Using hardcoded defaults.");
            return;
        }

        try
        {
            var serializer = new Serializer();
            YamlSyntaxStyle loadedStyle = serializer.Deserialize<YamlSyntaxStyle>(styleAsset.text);
            if (loadedStyle != null)
            {
                _syntaxStyle = loadedStyle;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load YAML styles from styles.yaml: {e.Message}. Using hardcoded defaults.");
        }
    }

    private void InitializeGuiStyles()
    {
        Texture2D editorBackground = MakeTex(1, 1, FromHex(_syntaxStyle.BackgroundColor));

        _textStyle = new GUIStyle(EditorStyles.textArea)
        {
            fontSize = _currentFontSize,
            wordWrap = false,
            richText = true,
            padding = new RectOffset(8, 8, 4, 4),
            normal = { textColor = Color.white, background = editorBackground },
            alignment = TextAnchor.UpperLeft
        };

        _numberStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.UpperRight,
            normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
            fontSize = _currentFontSize
        };

        _previewStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = _currentFontSize,
            wordWrap = false,
            richText = true,
            padding = new RectOffset(8, 8, 4, 4),
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = Color.white, background = editorBackground }
        };

        _selectedFileStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { background = MakeTex(2, 2, new Color(0.2f, 0.4f, 0.7f, 0.5f)), textColor = Color.white },
            hover = { background = MakeTex(2, 2, new Color(0.2f, 0.4f, 0.7f, 0.7f)), textColor = Color.white },
            padding = new RectOffset(4, 4, 2, 2),
            alignment = TextAnchor.MiddleLeft
        };
    }

    private void InitializeLanguageService()
    {
        _langService = Resources.Load<LanguageServiceEditor>("Editor/SO/Language Service Editor");
        if (_langService == null)
        {
            Debug.LogError("LanguageServiceEditor not found in Resources!");
            _hasLanguages = false;
            return;
        }

        _availableLanguages = _langService.GetAvailableLanguages().ToList();
        _hasLanguages = _availableLanguages.Count > 0;

        if (_hasLanguages)
        {
            _selectedLanguage = _availableLanguages[0];
            ReloadLanguageStructure();
        }
        else
        {
            _selectedLanguage = null;
            _languageStructure.Clear();
            _foldouts.Clear();
        }
    }

    private void OnGUI()
    {
        if (_langService == null)
        {
            EditorGUILayout.LabelField("LanguageServiceEditor not found.");
            return;
        }

        HandleKeyboardShortcuts();
        DrawToolbar();
        DrawMainBody();

        DrawNotification();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        EditorGUILayout.LabelField("Language:", GUILayout.Width(70));

        if (_hasLanguages)
        {
            int langIndex = _availableLanguages.IndexOf(_selectedLanguage);
            int newLangIndex = EditorGUILayout.Popup(langIndex, _availableLanguages.ToArray(), GUILayout.Width(120));
            if (newLangIndex >= 0 && _selectedLanguage != _availableLanguages[newLangIndex])
            {
                string newLanguage = _availableLanguages[newLangIndex];

                YamlTab dirtyTab = _openTabs.FirstOrDefault(t => t.IsDirty);

                if (dirtyTab != null)
                {
                    if (EditorUtility.DisplayDialog("Unsaved Changes",
                        $"Do you want to save changes to file '{Path.GetFileName(dirtyTab.FilePath)}' before changing language? Changes will be lost for other unsaved files.",
                        "Save All and Change", "Change without Saving"))
                    {
                        foreach (var tab in _openTabs.Where(t => t.IsDirty).ToList())
                        {
                            SaveFile(tab.FilePath);
                        }
                    }
                }

                _selectedLanguage = newLanguage;
                ReloadLanguageStructure();
            }
        }
        else
        {
            EditorGUILayout.LabelField("N/A", GUILayout.Width(120));
        }


        if (GUILayout.Button("Reload Structure", EditorStyles.toolbarButton, GUILayout.Width(100))) ReloadLanguageStructure();

        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"Font Size: {_currentFontSize} (Ctrl+↑/↓)", EditorStyles.miniLabel, GUILayout.Width(180));

        EditorGUILayout.EndHorizontal();
    }

    private void DrawMainBody()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(FileBlockWidth), GUILayout.ExpandHeight(true));

        if (_hasLanguages)
        {
            DrawFileBlockSearch();
        }

        if (_hasLanguages && _languageStructure.ContainsKey(_rootLangPathNormalized))
        {
            _scrollPosFiles = EditorGUILayout.BeginScrollView(_scrollPosFiles);
            DrawFolder(_rootLangPathNormalized, _languageStructure[_rootLangPathNormalized]);
            EditorGUILayout.EndScrollView();
        }
        else if (_hasLanguages)
        {
            EditorGUILayout.LabelField("No language structure loaded.", EditorStyles.centeredGreyMiniLabel, GUILayout.ExpandHeight(true));
        }
        else
        {
            EditorGUILayout.LabelField("No Languages Found.", EditorStyles.centeredGreyMiniLabel, GUILayout.ExpandHeight(true));
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

        if (_hasLanguages && _openTabs.Count > 0)
        {
            DrawTabs();
        }

        if (!_hasLanguages)
        {
            GUIStyle errorStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.red },
                fontSize = 16
            };

            EditorGUILayout.LabelField("Please generate any language",
                errorStyle, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        }
        else if (_openTabs.Count > 0)
        {
            DrawEditorContent();
        }
        else
        {
            EditorGUILayout.LabelField("Select file",
                EditorStyles.centeredGreyMiniLabel, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }


    private void DrawEditorContent()
    {
        YamlTab currentTab = CurrentTab;
        if (currentTab == null) return;

        // Тулбар редактора (Save/Reload/Undo/Redo)
        DrawEditorToolbar();

        float editorPanelWidth = (position.width - FileBlockWidth) / 2 - 10;
        editorPanelWidth = Mathf.Max(editorPanelWidth, 100);

        EditorGUILayout.BeginHorizontal();

        // --- Левая панель (Edit) ---
        EditorGUILayout.BeginVertical(GUILayout.Width(editorPanelWidth), GUILayout.ExpandHeight(true));
        GUILayout.Label($"Edit: {Path.GetFileName(currentTab.FilePath)} (plain text):", EditorStyles.boldLabel);

        string controlName = "YamlEditorTextArea";
        GUI.SetNextControlName(controlName);
        bool wasFocused = GUI.GetNameOfFocusedControl() == controlName;

        currentTab.ScrollPosText = EditorGUILayout.BeginScrollView(currentTab.ScrollPosText, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

        if (wasFocused && Event.current.type == EventType.Used)
        {
            GUI.FocusControl(null);
        }

        EditorGUI.BeginChangeCheck();

        _lastInputText = EditorGUILayout.TextArea(currentTab.YamlText, _textStyle, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

        if (EditorGUI.EndChangeCheck())
        {
            if (currentTab.YamlText != _lastInputText)
            {
                // Логика Undo/Redo теперь в YamlTab
                if (currentTab.UndoStack.Count == 0 || (currentTab.UndoStack.Count > 0 && currentTab.UndoStack.Peek() != currentTab.YamlText))
                {
                    currentTab.UndoStack.Push(currentTab.YamlText);
                }
                currentTab.RedoStack.Clear();
                currentTab.YamlText = _lastInputText;
                currentTab.IsDirty = true;
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // --- Правая панель (Preview) ---
        EditorGUILayout.BeginVertical(GUILayout.Width(editorPanelWidth), GUILayout.ExpandHeight(true));
        GUILayout.Label("Preview (with syntax):", EditorStyles.boldLabel);

        // Используем ScrollPosText из вкладки для синхронизации
        Vector2 scrollPreview = GUILayout.BeginScrollView(currentTab.ScrollPosText, false, true, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        string highlighted = ApplyYamlHighlighting(currentTab.YamlText);


        EditorGUILayout.SelectableLabel(highlighted, _previewStyle, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        GUILayout.EndScrollView();

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }


    private void DrawTabs()
    {
        GUIStyle tabStyle = new GUIStyle(EditorStyles.toolbarButton);
        tabStyle.fixedHeight = TabHeight;
        tabStyle.margin = new RectOffset(1, 1, 0, 0);
        tabStyle.padding = new RectOffset(6, 6, 2, 2);

        GUIStyle activeTabStyle = new GUIStyle(tabStyle);
        activeTabStyle.normal.background = tabStyle.hover.background;

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        bool tabClosed = false;

        for (int i = 0; i < _openTabs.Count; i++)
        {
            YamlTab tab = _openTabs[i];
            string tabName = Path.GetFileName(tab.FilePath);

            if (tab.IsDirty)
            {
                tabName += "*";
            }

            bool isActive = i == _currentTabIndex;

            EditorGUILayout.BeginHorizontal(isActive ? activeTabStyle : tabStyle, GUILayout.Height(TabHeight), GUILayout.Width(150));

            if (GUILayout.Button(tabName, EditorStyles.label, GUILayout.ExpandWidth(true)))
            {
                _currentTabIndex = i;
                GUI.FocusControl(null);
            }

            if (GUILayout.Button("x", EditorStyles.label, GUILayout.Width(14)))
            {
                TryCloseTab(i);
                tabClosed = true;
            }

            EditorGUILayout.EndHorizontal();

            if (tabClosed)
            {
                break;
            }
        }

        GUILayout.FlexibleSpace();

        EditorGUILayout.EndHorizontal();

        if (tabClosed)
        {
            throw new ExitGUIException();
        }
    }


    private void DrawEditorToolbar()
    {
        YamlTab currentTab = CurrentTab;
        if (currentTab == null) return;

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60))) LoadFile(currentTab.FilePath);

        GUI.enabled = !string.IsNullOrEmpty(currentTab.FilePath) && currentTab.IsDirty;
        if (GUILayout.Button("Save (Ctrl+S)", EditorStyles.toolbarButton, GUILayout.Width(120))) SaveFile(currentTab.FilePath);
        GUI.enabled = true;

        GUILayout.FlexibleSpace();

        GUI.enabled = currentTab.UndoStack.Count > 0;
        if (GUILayout.Button("Undo (Ctrl+Z)", EditorStyles.toolbarButton, GUILayout.Width(80))) HandleUndo();
        GUI.enabled = currentTab.RedoStack.Count > 0;
        if (GUILayout.Button("Redo (Ctrl+Y)", EditorStyles.toolbarButton, GUILayout.Width(80))) HandleRedo();
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }



    private void ReloadLanguageStructure()
    {
        if (string.IsNullOrEmpty(_selectedLanguage) || _langService == null) return;

        YamlTab dirtyTab = _openTabs.FirstOrDefault(t => t.IsDirty);

        if (dirtyTab != null)
        {
            if (EditorUtility.DisplayDialog("Unsaved Changes",
                $"Do you want to save changes to file '{Path.GetFileName(dirtyTab.FilePath)}' before reloading the structure? Changes will be lost for other unsaved files.",
                "Save All and Reload", "Reload without Saving"))
            {
                foreach (var tab in _openTabs.Where(t => t.IsDirty).ToList())
                {
                    SaveFile(tab.FilePath);
                }
            }
        }

        _openTabs.Clear();
        _currentTabIndex = -1;

        string langPath = _langService.GetLanguagePath(_selectedLanguage);
        _rootLangPathNormalized = NormalizePath(langPath);

        _languageStructure.Clear();
        _foldouts.Clear();
        _searchQuery = "";

        if (Directory.Exists(langPath))
        {
            AddFolderRecursive(langPath);

            if (_languageStructure.ContainsKey(_rootLangPathNormalized))
            {
                _foldouts[_rootLangPathNormalized] = true;
            }
        }
        else
        {
            Debug.LogWarning($"Language path not found: {langPath}");
        }

        Repaint();
    }

    private void LoadFile(string file)
    {
        if (string.IsNullOrEmpty(file)) return;

        string normalizedPath = NormalizePath(file);

        // 1. Проверяем, открыта ли уже вкладка
        YamlTab existingTab = _openTabs.FirstOrDefault(t => NormalizePath(t.FilePath) == normalizedPath);

        if (existingTab != null)
        {
            _currentTabIndex = _openTabs.IndexOf(existingTab);
            GUI.FocusControl(null);
            Repaint();
            return;
        }

        // 2. Загружаем новый файл
        string fileContent = "";
        if (File.Exists(file))
        {
            fileContent = File.ReadAllText(file);
        }
        else
        {
            Debug.LogWarning($"File not found: {file}");
        }

        YamlTab newTab = new YamlTab(file, fileContent);
        _openTabs.Add(newTab);
        _currentTabIndex = _openTabs.Count - 1;

        GUI.FocusControl(null);
        Repaint();
    }

    private void SaveFile(string file)
    {
        YamlTab tabToSave = _openTabs.FirstOrDefault(t => NormalizePath(t.FilePath) == NormalizePath(file));

        if (tabToSave == null || string.IsNullOrEmpty(file))
        {
            Debug.LogWarning("No file selected or tab open for saving.");
            return;
        }

        try
        {
            File.WriteAllText(file, tabToSave.YamlText);
            AssetDatabase.Refresh();
            Debug.Log($"File saved successfully: {Path.GetFileName(file)}");

            ShowNotification("File saved successfully!", Color.green);
            tabToSave.IsDirty = false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save file: {ex.Message}");
            ShowNotification($"Failed to save: {Path.GetFileName(file)}", Color.red);
        }
    }

    private void TrySwitchFile(string newFilePath)
    {
        YamlTab currentTab = CurrentTab;

        if (currentTab != null && string.Equals(NormalizePath(currentTab.FilePath), NormalizePath(newFilePath), System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (currentTab == null || !currentTab.IsDirty)
        {
            LoadFile(newFilePath);
            return;
        }

        string currentFileName = Path.GetFileName(currentTab.FilePath);

        int option = EditorUtility.DisplayDialogComplex(
            "Unsaved Changes",
            $"Do you want to save changes to file '{currentFileName}' before switching to '{Path.GetFileName(newFilePath)}'?",
            "Save",
            "Cancel",
            "Don't Save"
        );

        switch (option)
        {
            case 0: // Save
                SaveFile(currentTab.FilePath);
                if (!currentTab.IsDirty)
                {
                    LoadFile(newFilePath);
                }
                break;

            case 1: // Cancel
                break;

            case 2: // Don't Save
                currentTab.IsDirty = false;
                LoadFile(newFilePath);
                break;
        }
    }

    private void TryCloseTab(int index)
    {
        YamlTab tabToClose = _openTabs[index];

        if (tabToClose.IsDirty)
        {
            int option = EditorUtility.DisplayDialogComplex(
                "Unsaved Changes",
                $"Do you want to save changes to file '{Path.GetFileName(tabToClose.FilePath)}' before closing?",
                "Save",
                "Cancel",
                "Don't Save"
            );

            switch (option)
            {
                case 1: // Cancel
                    return;
                case 0: // Save
                    SaveFile(tabToClose.FilePath);
                    if (tabToClose.IsDirty) return;
                    break;
                case 2: // Don't Save
                    break;
            }
        }

        _openTabs.RemoveAt(index);

        if (_openTabs.Count == 0)
        {
            _currentTabIndex = -1;
        }
        else if (_currentTabIndex == index)
        {
            // Переключаемся на ближайшую вкладку
            _currentTabIndex = Mathf.Clamp(index - 1, 0, _openTabs.Count - 1);
        }
        else if (_currentTabIndex > index)
        {
            // Смещаем индекс
            _currentTabIndex--;
        }

        GUI.FocusControl(null);
        Repaint();
    }


    private void HandleUndo()
    {
        YamlTab currentTab = CurrentTab;
        if (currentTab == null) return;

        if (currentTab.UndoStack.Count > 0)
        {
            currentTab.RedoStack.Push(currentTab.YamlText);
            currentTab.YamlText = currentTab.UndoStack.Pop();
            currentTab.IsDirty = true;

            GUI.FocusControl("");
            Repaint();
        }
    }

    private void HandleRedo()
    {
        YamlTab currentTab = CurrentTab;
        if (currentTab == null) return;

        if (currentTab.RedoStack.Count > 0)
        {
            currentTab.UndoStack.Push(currentTab.YamlText);
            currentTab.YamlText = currentTab.RedoStack.Pop();
            currentTab.IsDirty = true;

            GUI.FocusControl("");
            Repaint();
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
                    YamlTab currentTab = CurrentTab;
                    if (currentTab != null) SaveFile(currentTab.FilePath);
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
        int newSize = _currentFontSize + change;
        if (newSize >= MinFontSize && newSize <= MaxFontSize)
        {
            _currentFontSize = newSize;
            _textStyle.fontSize = _currentFontSize;
            _numberStyle.fontSize = _currentFontSize;
            _previewStyle.fontSize = _currentFontSize;
            Repaint();
        }
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = Enumerable.Repeat(col, width * height).ToArray();
        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pix);
        tex.Apply();
        return tex;
    }

    private Color FromHex(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            return color;
        }
        Debug.LogWarning($"Could not parse hex color: {hex}");
        return Color.black;
    }

    private string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;
        return Path.GetFullPath(path)
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .TrimEnd(Path.DirectorySeparatorChar)
            .ToLowerInvariant();
    }

    private string ApplyYamlHighlighting(string input)
    {
        if (string.IsNullOrEmpty(input) || _syntaxStyle == null) return input;

        string ColorGreenComment = _syntaxStyle.CommentColor;
        string ColorPurpleKey = _syntaxStyle.KeyColor;
        string ColorYellowKeyword = _syntaxStyle.KeywordColor;
        string ColorBlueString = _syntaxStyle.StringColor;

        var sb = new StringBuilder();
        var lines = input.Split('\n');

        foreach (var rawLine in lines)
        {
            string line = rawLine.TrimEnd('\r');
            int commentIndex = IndexOfHashOutsideQuotes(line);
            string commentPart = commentIndex >= 0 ? line.Substring(commentIndex) : null;
            string content = commentIndex >= 0 ? line.Substring(0, commentIndex) : line;
            int colonIndex = IndexOfCharOutsideQuotes(content, ':');

            if (colonIndex >= 0)
            {
                string left = content.Substring(0, colonIndex);
                string right = content.Substring(colonIndex + 1);
                var leftMatch = System.Text.RegularExpressions.Regex.Match(left, @"^(\s*)(-?\s*)?(.*)$");
                string indent = leftMatch.Groups[1].Value;
                string maybeDash = leftMatch.Groups[2].Value ?? "";
                string keyText = leftMatch.Groups[3].Value ?? "";
                keyText = keyText.TrimEnd();

                var linePart = new StringBuilder();
                linePart.Append(indent);
                if (!string.IsNullOrEmpty(maybeDash) && maybeDash.Contains("-"))
                    linePart.Append($"<color={ColorBlueString}>{maybeDash}</color>");
                if (!string.IsNullOrEmpty(keyText))
                    linePart.Append($"<color={ColorPurpleKey}>{keyText}</color>");
                linePart.Append(":");

                int leadSpaces = 0;
                while (leadSpaces < right.Length && char.IsWhiteSpace(right[leadSpaces])) leadSpaces++;
                string valueLead = right.Substring(0, leadSpaces);
                string value = right.Substring(leadSpaces);

                string highlightedValue = "";
                if (string.IsNullOrEmpty(value))
                    highlightedValue = "";
                else if (value.StartsWith("'") || value.StartsWith("\""))
                    highlightedValue = $"{valueLead}<color={ColorBlueString}>{value}</color>";
                else if (value.StartsWith("|") || value.StartsWith(">"))
                    highlightedValue = $"{valueLead}<color={ColorBlueString}>{value}</color>";
                else
                    highlightedValue = valueLead + HighlightUnquotedValue(value, ColorBlueString, ColorYellowKeyword);

                sb.Append(linePart.ToString());
                sb.Append(highlightedValue);
                if (commentPart != null) sb.Append($"<color={ColorGreenComment}>{commentPart}</color>");
                sb.Append('\n');
            }
            else
            {
                int firstNonSpace = 0;
                while (firstNonSpace < content.Length && char.IsWhiteSpace(content[firstNonSpace])) firstNonSpace++;
                if (firstNonSpace < content.Length && content[firstNonSpace] == '-')
                {
                    string before = content.Substring(0, firstNonSpace);
                    string dash = "-";
                    string after = content.Substring(firstNonSpace + 1);
                    int afterLead = 0;
                    while (afterLead < after.Length && char.IsWhiteSpace(after[afterLead])) afterLead++;
                    string afterLeadStr = after.Substring(0, afterLead);
                    string value = after.Substring(afterLead);

                    string highlightedValue;
                    if (string.IsNullOrEmpty(value)) highlightedValue = "";
                    else if (value.StartsWith("'") || value.StartsWith("\""))
                        highlightedValue = $"{afterLeadStr}<color={ColorBlueString}>{value}</color>";
                    else
                        highlightedValue = afterLeadStr + HighlightUnquotedValue(value, ColorBlueString, ColorYellowKeyword);

                    sb.Append(before);
                    sb.Append($"<color={ColorBlueString}>{dash}</color>");
                    sb.Append(highlightedValue);
                    if (commentPart != null) sb.Append($"<color={ColorGreenComment}>{commentPart}</color>");
                    sb.Append('\n');
                }
                else
                {
                    if (!string.IsNullOrEmpty(content)) sb.Append(System.Net.WebUtility.HtmlEncode(content));
                    if (commentPart != null) sb.Append($"<color={ColorGreenComment}>{commentPart}</color>");
                    sb.Append('\n');
                }
            }
        }

        if (sb.Length > 0 && sb[sb.Length - 1] == '\n') sb.Length--;

        return sb.ToString();
    }

    private static int IndexOfHashOutsideQuotes(string line)
    {
        bool inSingle = false, inDouble = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\'' && !inDouble) inSingle = !inSingle;
            else if (c == '"' && !inSingle) inDouble = !inDouble;
            else if (c == '#' && !inSingle && !inDouble) return i;
        }
        return -1;
    }

    private static int IndexOfCharOutsideQuotes(string line, char target)
    {
        bool inSingle = false, inDouble = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\'' && !inDouble) inSingle = !inSingle;
            else if (c == '"' && !inSingle) inDouble = !inDouble;
            else if (c == target && !inSingle && !inDouble) return i;
        }
        return -1;
    }

    private static string HighlightUnquotedValue(string value, string colorForStrings, string colorForKeywords)
    {
        var separators = new[] { ' ', '\t', ',', '[', ']', '{', '}', ':' };
        var sb = new StringBuilder();
        int i = 0;
        while (i < value.Length)
        {
            if (separators.Contains(value[i]))
            {
                sb.Append(value[i]);
                i++;
                continue;
            }

            int start = i;
            while (i < value.Length && !separators.Contains(value[i])) i++;
            string token = value.Substring(start, i - start);

            if (IsYamlNumber(token) || IsYamlBoolOrNull(token))
                sb.Append($"<color={colorForKeywords}>{token}</color>");
            else
                sb.Append($"<color={colorForStrings}>{token}</color>");
        }
        return sb.ToString();
    }

    private static bool IsYamlNumber(string s)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(s, @"^[+-]?\d+(\.\d+)?$");
    }

    private static bool IsYamlBoolOrNull(string s)
    {
        return s == "true" || s == "false" || s == "null" ||
               s == "True" || s == "False" || s == "NULL";
    }


    private void DrawFileBlockSearch()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUI.SetNextControlName("FileSearchField");
        string newSearchQuery = GUILayout.TextField(_searchQuery, EditorStyles.toolbarSearchField);

        if (newSearchQuery != _searchQuery)
        {
            _searchQuery = newSearchQuery;
            Repaint();
        }

        if (GUILayout.Button(EditorGUIUtility.IconContent("d_winbtn_close"), EditorStyles.toolbarButton, GUILayout.Width(20)))
        {
            _searchQuery = "";
            GUI.FocusControl(null);
            Repaint();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawFolder(string folderPathNormalized, List<string> files)
    {
        if (string.IsNullOrEmpty(folderPathNormalized)) return;

        string folderName = Path.GetFileName(folderPathNormalized);
        if (string.Equals(folderPathNormalized, _rootLangPathNormalized, System.StringComparison.OrdinalIgnoreCase))
        {
            folderName = _selectedLanguage;
        }

        bool isSearching = !string.IsNullOrEmpty(_searchQuery);
        string query = _searchQuery.ToLowerInvariant();

        List<string> filteredFiles = isSearching
            ? files.Where(f => f.ToLowerInvariant().Contains(query)).ToList()
            : files;

        bool folderMatches = CheckFolderForMatchRecursive(folderPathNormalized);

        if (isSearching && !folderMatches && !string.Equals(folderPathNormalized, _rootLangPathNormalized, System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!_foldouts.ContainsKey(folderPathNormalized))
            _foldouts[folderPathNormalized] = false;

        bool shouldBeOpen = _foldouts[folderPathNormalized];

        if (isSearching && folderMatches)
        {
            if (string.Equals(folderPathNormalized, _rootLangPathNormalized, System.StringComparison.OrdinalIgnoreCase))
            {
                _foldouts[folderPathNormalized] = EditorGUILayout.Foldout(_foldouts[folderPathNormalized], folderName);
                shouldBeOpen = _foldouts[folderPathNormalized];
            }
            else
            {
                EditorGUILayout.Foldout(true, folderName, true);
                shouldBeOpen = true;
            }
        }
        else
        {
            _foldouts[folderPathNormalized] = EditorGUILayout.Foldout(_foldouts[folderPathNormalized], folderName);
            shouldBeOpen = _foldouts[folderPathNormalized];
        }

        if (shouldBeOpen)
        {
            EditorGUI.indentLevel++;

            YamlTab currentTab = CurrentTab;
            string selectedFilePathNormalized = currentTab != null ? NormalizePath(currentTab.FilePath) : string.Empty;

            foreach (string file in filteredFiles)
            {
                string fullPath = Path.Combine(folderPathNormalized, file);
                bool isSelected = NormalizePath(fullPath) == selectedFilePathNormalized;

                GUIStyle style = isSelected ? _selectedFileStyle : EditorStyles.label;

                if (GUILayout.Button(file, style))
                {
                    TrySwitchFile(fullPath);
                }
            }

            char separator = Path.DirectorySeparatorChar;
            string pathPrefix = folderPathNormalized + separator;

            List<string> subFolders = _languageStructure.Keys
                .Where(k =>
                {
                    if (!k.StartsWith(pathPrefix, System.StringComparison.OrdinalIgnoreCase))
                        return false;

                    string remainingPath = k.Substring(pathPrefix.Length);

                    return !remainingPath.Contains(separator);
                })
                .OrderBy(k => Path.GetFileName(k))
                .ToList();

            foreach (string subFolder in subFolders)
            {
                if (_languageStructure.ContainsKey(subFolder))
                {
                    DrawFolder(subFolder, _languageStructure[subFolder]);
                }
            }

            EditorGUI.indentLevel--;
        }
    }

    private bool CheckFolderForMatchRecursive(string folderPathNormalized)
    {
        if (string.IsNullOrEmpty(_searchQuery)) return true;
        if (!_languageStructure.ContainsKey(folderPathNormalized)) return false;

        string query = _searchQuery.ToLowerInvariant();

        if (_languageStructure[folderPathNormalized]
            .Any(f => f.ToLowerInvariant().Contains(query)))
        {
            return true;
        }

        char separator = Path.DirectorySeparatorChar;
        string pathPrefix = folderPathNormalized + separator;

        List<string> subFolders = _languageStructure.Keys
            .Where(k =>
            {
                if (!k.StartsWith(pathPrefix, System.StringComparison.OrdinalIgnoreCase))
                    return false;

                string remainingPath = k.Substring(pathPrefix.Length);

                return !remainingPath.Contains(separator);
            })
            .OrderBy(k => Path.GetFileName(k))
            .ToList();

        foreach (string subFolder in subFolders)
        {
            if (_languageStructure.ContainsKey(subFolder) && CheckFolderForMatchRecursive(subFolder))
            {
                return true;
            }
        }

        return false;
    }

    private void AddFolderRecursive(string folderPath)
    {
        string normalizedPath = NormalizePath(folderPath);

        List<string> yamlFiles = Directory.GetFiles(folderPath, "*.yaml", SearchOption.TopDirectoryOnly)
            .Where(f => !f.EndsWith(".meta") && !f.EndsWith(".png"))
            .Select(Path.GetFileName)
            .OrderBy(f => f)
            .ToList();

        _languageStructure[normalizedPath] = yamlFiles;

        foreach (string subDir in Directory.GetDirectories(folderPath).OrderBy(d => d))
        {
            AddFolderRecursive(subDir);
        }
    }


    private void DrawNotification()
    {
        if (!string.IsNullOrEmpty(_notificationText) && EditorApplication.timeSinceStartup < _notificationEndTime)
        {
            float width = 200f;
            float height = 24f;
            float padding = 10f;

            Rect rect = new Rect(
                position.width - width - padding,
                position.height - height - padding,
                width,
                height
            );

            GUIStyle backgroundStyle = new GUIStyle(GUI.skin.box);
            backgroundStyle.normal.background = MakeTex(1, 1, _notificationColor * 0.7f);

            GUI.Box(rect, "", backgroundStyle);

            GUIStyle textStyle = new GUIStyle(EditorStyles.boldLabel);
            textStyle.normal.textColor = Color.white;
            textStyle.alignment = TextAnchor.MiddleCenter;

            GUI.Label(rect, _notificationText, textStyle);

            Repaint();
        }
    }

    private void ShowNotification(string message, Color color)
    {
        _notificationText = message;
        _notificationColor = color;
        _notificationEndTime = EditorApplication.timeSinceStartup + NotificationDuration;
        Repaint();
    }
}