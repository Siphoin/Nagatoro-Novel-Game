using SharpYaml.Serialization;
using SNEngine.Editor.Language;
using SNEngine.Editor.Windows.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class YAMLEditorWindow : EditorWindow
{
    private LanguageServiceEditor _langService;
    private string _selectedLanguage;
    private List<string> _availableLanguages = new();
    private Dictionary<string, List<string>> _languageStructure = new();

    private string _selectedFilePath;
    private string _yamlText = "";

    private Vector2 _scrollPosFiles;
    private Vector2 _scrollPosText;

    private GUIStyle _textStyle;
    private GUIStyle _numberStyle;
    private GUIStyle _previewStyle;
    private GUIStyle _selectedFileStyle;

    private YamlSyntaxStyle _syntaxStyle;

    private Stack<string> _undoStack = new();
    private Stack<string> _redoStack = new();
    private Dictionary<string, bool> _foldouts = new();

    private const float FileBlockWidth = 250f;
    private const int MinFontSize = 10;
    private const int MaxFontSize = 24;
    private int _currentFontSize = 14;

    private string _notificationText = "";
    private Color _notificationColor = Color.green;
    private double _notificationEndTime = 0;
    private const float NotificationDuration = 3.0f;
    private bool _isDirty = false;

    private string _rootLangPathNormalized;
    private string _lastInputText = "";

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
            Debug.LogWarning("styles.yaml not found at Resources/Editor/YamlEditor/styles. Using hardcoded defaults.");
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
            normal = {
                textColor = Color.white,
                background = editorBackground
            }
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
            return;
        }

        _availableLanguages = _langService.GetAvailableLanguages().ToList();
        if (_availableLanguages.Count > 0)
        {
            _selectedLanguage = _availableLanguages[0];
            ReloadLanguageStructure();
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

        string highlighted = input;

        string ColorGreenComment = _syntaxStyle.CommentColor;
        string ColorPurpleKey = _syntaxStyle.KeyColor;
        string ColorYellowKeyword = _syntaxStyle.KeywordColor;
        string ColorBlueString = _syntaxStyle.StringColor;

        highlighted = Regex.Replace(highlighted, @"(#[^\n]*)", $"<color={ColorGreenComment}>$1</color>");

        // 2. Ключи (Фиолетовый)
        highlighted = Regex.Replace(highlighted, @"^(\s*[-]?\s*)([^\s#:-][^\s#:]*):",
            m => $"{m.Groups[1].Value}<color={ColorPurpleKey}>{m.Groups[2].Value}</color>:", RegexOptions.Multiline);

        highlighted = Regex.Replace(highlighted, @"\b(\d+(\.\d+)?|true|false|null)\b", $"<color={ColorYellowKeyword}>$1</color>");

        highlighted = Regex.Replace(highlighted, @"(['""][^'""]*['""])", $"<color={ColorBlueString}>$1</color>");


        highlighted = Regex.Replace(highlighted, @"^(\s*)(-\s)([^\n]+)",
            m => $"{m.Groups[1].Value}<color={ColorBlueString}>{m.Groups[2].Value}{m.Groups[3].Value}</color>", RegexOptions.Multiline);

        highlighted = Regex.Replace(highlighted, @":\s([^\n]+)", $": <color={ColorBlueString}>$1</color>");

        return highlighted;
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
        int langIndex = _availableLanguages.IndexOf(_selectedLanguage);
        int newLangIndex = EditorGUILayout.Popup(langIndex, _availableLanguages.ToArray(), GUILayout.Width(120));
        if (newLangIndex >= 0 && _selectedLanguage != _availableLanguages[newLangIndex])
        {
            string newLanguage = _availableLanguages[newLangIndex];

            if (_isDirty)
            {
                if (EditorUtility.DisplayDialog("Unsaved Changes",
                    $"Do you want to save changes to '{Path.GetFileName(_selectedFilePath)}' before changing language?",
                    "Save and Change", "Change without Saving"))
                {
                    SaveFile(_selectedFilePath);
                }
            }

            _selectedLanguage = newLanguage;
            ReloadLanguageStructure();
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

        if (_languageStructure.ContainsKey(_rootLangPathNormalized))
        {
            _scrollPosFiles = EditorGUILayout.BeginScrollView(_scrollPosFiles);
            DrawFolder(_rootLangPathNormalized, _languageStructure[_rootLangPathNormalized]);
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.LabelField("No language structure loaded.", EditorStyles.centeredGreyMiniLabel, GUILayout.ExpandHeight(true));
        }

        EditorGUILayout.EndVertical();


        if (!string.IsNullOrEmpty(_selectedFilePath))
        {
            DrawYamlEditor();
        }
        else
        {

            EditorGUILayout.LabelField("Select file",
                EditorStyles.centeredGreyMiniLabel, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawYamlEditor()
    {
        DrawEditorToolbar();


        float editorPanelWidth = (position.width - FileBlockWidth) / 2 - 10;


        editorPanelWidth = Mathf.Max(editorPanelWidth, 100);

        EditorGUILayout.BeginHorizontal();


        EditorGUILayout.BeginVertical(GUILayout.Width(editorPanelWidth), GUILayout.ExpandHeight(true));
        GUILayout.Label($"Edit: {Path.GetFileName(_selectedFilePath)} (plain text):", EditorStyles.boldLabel);

        string controlName = "YamlEditorTextArea";
        GUI.SetNextControlName(controlName);

        bool wasFocused = GUI.GetNameOfFocusedControl() == controlName;


        _scrollPosText = EditorGUILayout.BeginScrollView(_scrollPosText, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

        if (wasFocused && Event.current.type == EventType.Used)
        {
            GUI.FocusControl(null);
        }

        EditorGUI.BeginChangeCheck();


        _lastInputText = EditorGUILayout.TextArea(_yamlText, _textStyle, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

        if (EditorGUI.EndChangeCheck())
        {
            if (_yamlText != _lastInputText)
            {
                if (_undoStack.Count == 0 || (_undoStack.Count > 0 && _undoStack.Peek() != _yamlText))
                {
                    _undoStack.Push(_yamlText);
                }
                _redoStack.Clear();
                _yamlText = _lastInputText;
                _isDirty = true;
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);


        EditorGUILayout.BeginVertical(GUILayout.Width(editorPanelWidth), GUILayout.ExpandHeight(true));
        GUILayout.Label("Preview (with syntax):", EditorStyles.boldLabel);


        Vector2 scrollPreview = GUILayout.BeginScrollView(_scrollPosText, false, true, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        string highlighted = ApplyYamlHighlighting(_yamlText);


        EditorGUILayout.SelectableLabel(highlighted, _previewStyle, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        GUILayout.EndScrollView();

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }


    private void DrawEditorToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60))) LoadFile(_selectedFilePath);

        GUI.enabled = !string.IsNullOrEmpty(_selectedFilePath) && _isDirty;
        if (GUILayout.Button("Save (Ctrl+S)", EditorStyles.toolbarButton, GUILayout.Width(120))) SaveFile(_selectedFilePath);
        GUI.enabled = true;

        GUILayout.FlexibleSpace();

        GUI.enabled = _undoStack.Count > 0;
        if (GUILayout.Button("Undo (Ctrl+Z)", EditorStyles.toolbarButton, GUILayout.Width(80))) HandleUndo();
        GUI.enabled = _redoStack.Count > 0;
        if (GUILayout.Button("Redo (Ctrl+Y)", EditorStyles.toolbarButton, GUILayout.Width(80))) HandleRedo();
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
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

    private void HandleKeyboardShortcuts()
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.control)
        {
            switch (e.keyCode)
            {
                case KeyCode.S:
                    if (!string.IsNullOrEmpty(_selectedFilePath)) SaveFile(_selectedFilePath);
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

    private void HandleUndo()
    {
        if (_undoStack.Count > 0)
        {
            _redoStack.Push(_yamlText);
            _yamlText = _undoStack.Pop();
            _isDirty = true;

            GUI.FocusControl("");
            Repaint();
        }
    }

    private void HandleRedo()
    {
        if (_redoStack.Count > 0)
        {
            _undoStack.Push(_yamlText);
            _yamlText = _redoStack.Pop();
            _isDirty = true;

            GUI.FocusControl("");
            Repaint();
        }
    }

    private void ReloadLanguageStructure()
    {
        if (string.IsNullOrEmpty(_selectedLanguage) || _langService == null) return;

        if (_isDirty && !string.IsNullOrEmpty(_selectedFilePath))
        {
            if (EditorUtility.DisplayDialog("Unsaved Changes",
                $"Do you want to save changes to '{Path.GetFileName(_selectedFilePath)}' before reloading the structure?",
                "Save and Reload", "Reload without Saving"))
            {
                SaveFile(_selectedFilePath);
            }
        }

        _isDirty = false;

        string langPath = _langService.GetLanguagePath(_selectedLanguage);
        _rootLangPathNormalized = NormalizePath(langPath);

        _languageStructure.Clear();
        _foldouts.Clear();
        _selectedFilePath = null;
        _yamlText = "";
        _lastInputText = "";


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

    private void DrawFolder(string folderPathNormalized, List<string> files)
    {
        string folderName = Path.GetFileName(folderPathNormalized);

        if (string.Equals(folderPathNormalized, _rootLangPathNormalized, System.StringComparison.OrdinalIgnoreCase))
        {
            folderName = _selectedLanguage;
        }

        if (!_foldouts.ContainsKey(folderPathNormalized))
            _foldouts[folderPathNormalized] = false;

        _foldouts[folderPathNormalized] = EditorGUILayout.Foldout(_foldouts[folderPathNormalized], folderName);

        if (_foldouts[folderPathNormalized])
        {
            EditorGUI.indentLevel++;

            foreach (string file in files)
            {
                string fullPath = Path.Combine(folderPathNormalized, file);
                bool isSelected = NormalizePath(fullPath) == NormalizePath(_selectedFilePath);

                GUIStyle style = isSelected ? _selectedFileStyle : EditorStyles.label;

                if (GUILayout.Button(file, style))
                {
                    if (!isSelected)
                    {
                        TrySwitchFile(fullPath);
                    }
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


    private void LoadFile(string file)
    {
        if (string.IsNullOrEmpty(file)) return;

        _selectedFilePath = file;
        _undoStack.Clear();
        _redoStack.Clear();
        _isDirty = false;

        if (File.Exists(file))
        {
            _yamlText = File.ReadAllText(file);
            _undoStack.Push(_yamlText);
        }
        else
        {
            _yamlText = "";
            Debug.LogWarning($"File not found: {file}");
        }
        _scrollPosText = Vector2.zero;

        GUI.FocusControl(null);

        Repaint();
    }

    private void SaveFile(string file)
    {
        if (string.IsNullOrEmpty(file))
        {
            Debug.LogWarning("No file path selected for saving.");
            return;
        }

        try
        {
            File.WriteAllText(file, _yamlText);
            AssetDatabase.Refresh();
            Debug.Log($"File saved successfully: {Path.GetFileName(file)}");

            ShowNotification("File saved successfully!", Color.green);
            _isDirty = false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save file: {ex.Message}");

            ShowNotification($"Failed to save: {Path.GetFileName(file)}", Color.red);
        }
    }

    private void TrySwitchFile(string newFilePath)
    {
        if (string.Equals(NormalizePath(_selectedFilePath), NormalizePath(newFilePath), System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!_isDirty)
        {
            LoadFile(newFilePath);
            return;
        }

        string currentFileName = Path.GetFileName(_selectedFilePath);

        int option = EditorUtility.DisplayDialogComplex(
            "Unsaved Changes",
            $"Do you want to save changes to file '{currentFileName}' before switching to '{Path.GetFileName(newFilePath)}'?",
            "Save",
            "Cancel",
            "Don't Save"
        );

        switch (option)
        {
            case 0:
                SaveFile(_selectedFilePath);
                if (!_isDirty)
                {
                    LoadFile(newFilePath);
                }
                break;

            case 1:
                break;

            case 2:
                _isDirty = false;
                LoadFile(newFilePath);
                break;
        }
    }
}