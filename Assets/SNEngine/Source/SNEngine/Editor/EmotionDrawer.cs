using UnityEditor;
using UnityEngine;
using SNEngine.CharacterSystem;
namespace SNEngine.Editor
{
    [CustomPropertyDrawer(typeof(Emotion))]
    public class EmotionDrawer : PropertyDrawer
    {
        private const float PREVIEW_SIZE = 64f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2.5f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty nameProp = property.FindPropertyRelative("_name");
            SerializedProperty spriteProp = property.FindPropertyRelative("_sprite");

            float singleLine = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            Rect fieldsRect = new Rect(position.x, position.y, position.width - PREVIEW_SIZE - 10, position.height);

            Rect nameRect = new Rect(fieldsRect.x, fieldsRect.y, fieldsRect.width, singleLine);
            EditorGUI.PropertyField(nameRect, nameProp, new GUIContent("Name"));

            Rect spriteRect = new Rect(fieldsRect.x, fieldsRect.y + singleLine + spacing, fieldsRect.width, singleLine);
            EditorGUI.PropertyField(spriteRect, spriteProp, new GUIContent("Sprite"));

            Rect previewRect = new Rect(position.x + position.width - PREVIEW_SIZE, position.y + (position.height - PREVIEW_SIZE) / 2, PREVIEW_SIZE, PREVIEW_SIZE);

            EditorGUI.DrawRect(previewRect, new Color(0.1f, 0.1f, 0.1f, 0.5f));

            if (spriteProp.objectReferenceValue is Sprite sprite)
            {
                Texture2D texture = AssetPreview.GetAssetPreview(sprite.texture);
                if (texture != null)
                {
                    GUI.DrawTexture(previewRect, texture, ScaleMode.ScaleToFit);
                }
            }
            else
            {
                GUIStyle style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
                EditorGUI.LabelField(previewRect, "No Sprite", style);
            }

            EditorGUI.EndProperty();
        }
    }
}