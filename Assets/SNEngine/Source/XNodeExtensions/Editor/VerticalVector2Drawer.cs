using UnityEditor;
using UnityEngine;

namespace SiphoinUnityHelpers.XNodeExtensions.Editor
{
    [CustomPropertyDrawer(typeof(Vector2))]
    public class VerticalVector2Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Оставляем стандартный заголовок
            Rect labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, label);

            // Настраиваем отступы для компактности
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0; // Сбрасываем для точного позиционирования

            float lineH = EditorGUIUtility.singleLineHeight;
            float spacing = 2f;

            // Уменьшаем ширину меток "X" и "Y" до минимума
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 15f;

            // Поле X
            Rect xRect = new Rect(position.x + 15f, position.y + lineH + spacing, position.width - 15f, lineH);
            EditorGUI.PropertyField(xRect, property.FindPropertyRelative("x"), new GUIContent("X"));

            // Поле Y
            Rect yRect = new Rect(position.x + 15f, position.y + (lineH + spacing) * 2, position.width - 15f, lineH);
            EditorGUI.PropertyField(yRect, property.FindPropertyRelative("y"), new GUIContent("Y"));

            // Возвращаем настройки обратно
            EditorGUIUtility.labelWidth = originalLabelWidth;
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (EditorGUIUtility.singleLineHeight + 2f) * 3;
        }
    }
}