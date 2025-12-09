using SNEngine.Attributes;
using SNEngine.CharacterSystem;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SNEngine.Editor
{
    [CustomPropertyDrawer(typeof(EmotionFieldAttribute))]
    public class EmotionFieldPropertyDrawer : PropertyDrawer
    {
        private static Color32 _colorWarning = Color.clear;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            CharacterNode characterNode = property.serializedObject.targetObject as CharacterNode;

            if (characterNode.Character is null)
            {
                if (_colorWarning == Color.clear)
                {
                    _colorWarning = new Color32(250, 185, 185, 255);
                }
                GUIStyle style = new(GUI.skin.label);

                style.normal.textColor = _colorWarning;

                style.alignment = TextAnchor.MiddleCenter;

                EditorGUI.LabelField(position, "Character not seted", style);

                return;
            }

            else
            {
                var emotionsList = characterNode.Character.Emotions;

                if (emotionsList == null)
                {
                    GUIStyle style = new(GUI.skin.label);
                    style.normal.textColor = Color.yellow;
                    style.alignment = TextAnchor.MiddleCenter;
                    EditorGUI.LabelField(position, "Character emotion list is null", style);
                    return;
                }

                var emotions = emotionsList.ToArray();

                var emotionsVariants = emotions.Select(e => e.Name).ToArray();

                if (emotionsVariants.Length == 0)
                {
                    GUIStyle style = new(GUI.skin.label);
                    style.normal.textColor = Color.yellow;
                    style.alignment = TextAnchor.MiddleCenter;
                    EditorGUI.LabelField(position, "No emotions defined", style);
                    return;
                }

                int selectedIndex = Array.IndexOf(emotionsVariants, property.stringValue);

                if (selectedIndex < 0)
                {
                    selectedIndex = 0;
                }

                selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, emotionsVariants);

                property.stringValue = emotionsVariants[selectedIndex];
            }
        }
    }
}