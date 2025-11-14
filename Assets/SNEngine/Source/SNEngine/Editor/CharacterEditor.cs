using SNEngine.CharacterSystem;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
namespace SNEngine.Editor
{
    [CustomEditor(typeof(Character))]
    public class CharacterEditor : UnityEditor.Editor
    {
        private const int PREVIEW_SIZE = 200;
        private const int NAME_HEIGHT = 24;
        private const string GRID_CACHE_PATH = "Assets/SNEngine/Source/SNEngine/Editor/Cache/GridTexture.asset";
        private int _selectedEmotionIndex = 0;
        private Texture2D _gridTexture;

        private void OnEnable()
        {
            _gridTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GRID_CACHE_PATH);

            if (_gridTexture == null)
            {
                _gridTexture = CreateGridTexture(16, new Color(0.15f, 0.15f, 0.15f, 1f), new Color(0.2f, 0.2f, 0.2f, 1f));

                string directory = Path.GetDirectoryName(GRID_CACHE_PATH);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                AssetDatabase.CreateAsset(_gridTexture, GRID_CACHE_PATH);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private Texture2D CreateGridTexture(int cellSize, Color lineColor, Color bgColor)
        {
            Texture2D texture = new Texture2D(cellSize, cellSize);
            Color[] pixels = new Color[cellSize * cellSize];

            for (int y = 0; y < cellSize; y++)
            {
                for (int x = 0; x < cellSize; x++)
                {
                    if (x == 0 || y == 0)
                    {
                        pixels[y * cellSize + x] = lineColor;
                    }
                    else
                    {
                        pixels[y * cellSize + x] = bgColor;
                    }
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Point;
            return texture;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Character character = (Character)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Emotion Preview", EditorStyles.boldLabel);

            SerializedProperty emotionsProp = serializedObject.FindProperty("_emotions");

            if (emotionsProp.arraySize > 0)
            {
                string[] emotionNames = character.Emotions.Select(e => e.Name).ToArray();
                _selectedEmotionIndex = EditorGUILayout.Popup("Select emotion", _selectedEmotionIndex, emotionNames);

                if (_selectedEmotionIndex >= emotionsProp.arraySize)
                {
                    _selectedEmotionIndex = 0;
                }

                SerializedProperty emotionElement = emotionsProp.GetArrayElementAtIndex(_selectedEmotionIndex);
                SerializedProperty spriteProp = emotionElement.FindPropertyRelative("_sprite");

                EditorGUILayout.Space(5);

                Rect previewAreaRect = GUILayoutUtility.GetRect(PREVIEW_SIZE, PREVIEW_SIZE, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));

                if (_gridTexture != null)
                {
                    GUI.DrawTextureWithTexCoords(previewAreaRect, _gridTexture, new Rect(0, 0, previewAreaRect.width / _gridTexture.width, previewAreaRect.height / _gridTexture.height));
                }
                else
                {
                    EditorGUI.DrawRect(previewAreaRect, new Color(0.15f, 0.15f, 0.15f, 1f));
                }

                string hexColor = ColorUtility.ToHtmlStringRGB(character.ColorName);
                string coloredName = $"<color=#{hexColor}>{character.OriginalName}</color>";

                GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    richText = true,
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };

                Rect nameRect = new Rect(
                    previewAreaRect.x,
                    previewAreaRect.y + previewAreaRect.height - NAME_HEIGHT,
                    previewAreaRect.width,
                    NAME_HEIGHT
                );
                EditorGUI.LabelField(nameRect, coloredName, nameStyle);

                float padding = 8f;
                Rect spriteRect = new Rect(
                    previewAreaRect.x + padding,
                    previewAreaRect.y + padding,
                    previewAreaRect.width - 2 * padding,
                    previewAreaRect.height - NAME_HEIGHT - 2 * padding
                );

                if (spriteProp.objectReferenceValue != null)
                {
                    Sprite sprite = (Sprite)spriteProp.objectReferenceValue;
                    Texture2D texture = AssetPreview.GetAssetPreview(sprite.texture);

                    if (texture != null)
                    {
                        GUI.DrawTexture(spriteRect, texture, ScaleMode.ScaleToFit);
                    }
                }
                else
                {
                    Rect nullSpriteRect = new Rect(previewAreaRect.x, previewAreaRect.y, previewAreaRect.width, previewAreaRect.height - NAME_HEIGHT);
                    GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.gray } };
                    EditorGUI.LabelField(nullSpriteRect, "Нет спрайта", labelStyle);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Character has no emotions. Add at least one.", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}