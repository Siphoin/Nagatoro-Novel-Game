using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace SiphoinUnityHelpers.XNodeExtensions.Editor
{
    public abstract class SwitchNodeEditorBase<T> : NodeEditor
    {
        public override void OnBodyGUI()
        {
            serializedObject.Update();

            // 1. Отрисовка базовых портов Enter и Exit (из BaseNodeInteraction)
            EditorGUILayout.BeginHorizontal();
            NodeEditorGUILayout.PortField(target.GetInputPort("_enter"), GUILayout.Width(20));
            GUILayout.FlexibleSpace();
            NodeEditorGUILayout.PortField(target.GetOutputPort("_exit"), GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();

            // 2. Основные параметры
            NodeEditorGUILayout.PropertyField(serializedObject.FindProperty("_value"));
            NodeEditorGUILayout.PropertyField(serializedObject.FindProperty("_defaultCase"));

            SerializedProperty casesProp = serializedObject.FindProperty("_cases");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Cases", EditorStyles.boldLabel);

            // 3. Список кейсов
            for (int i = 0; i < casesProp.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();

                // Кнопка удаления
                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    casesProp.DeleteArrayElementAtIndex(i);
                    serializedObject.ApplyModifiedProperties();
                    ((XNode.Node)target).UpdatePorts();
                    break;
                }

                // Поле значения
                SerializedProperty element = casesProp.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(element, GUIContent.none);

                // Порт (выносим на край ноды)
                NodePort port = target.GetOutputPort("case " + i);
                NodeEditorGUILayout.PortField(GUIContent.none, port, GUILayout.Width(0));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5);

            // 4. Кнопка добавления (выровненная по центру полей ввода)
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(25); // Компенсируем кнопку удаления
            if (GUILayout.Button("+ Add Case"))
            {
                casesProp.InsertArrayElementAtIndex(casesProp.arraySize);
                serializedObject.ApplyModifiedProperties();
                ((XNode.Node)target).UpdatePorts();
            }
            GUILayout.Space(18); // Компенсируем зону портов справа
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                if (target is NodeControlExecute node)
                {
                    node.UpdatePorts();
                }
            }
        }

    }
}