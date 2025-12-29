using SiphoinUnityHelpers.XNodeExtensions.NodesControlExecutes;
using SiphoinUnityHelpers.XNodeExtensions.NodesControlExecutes.Switch;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace SiphoinUnityHelpers.XNodeExtensions.Editor
{
    public abstract class BaseSwitchNodeEditor<T> : NodeEditor
    {
        public override void OnBodyGUI()
        {
            serializedObject.Update();
            var node = target as SwitchNode<T>;

            NodeEditorGUILayout.PropertyField(serializedObject.FindProperty("_enter"));
            NodeEditorGUILayout.PropertyField(serializedObject.FindProperty("_value"));

            EditorGUILayout.Space(5);
            DrawInlineCases(node);
            EditorGUILayout.Space(5);

            NodeEditorGUILayout.PropertyField(serializedObject.FindProperty("_default"));

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawInlineCases(SwitchNode<T> node)
        {
            SerializedProperty casesProp = serializedObject.FindProperty("_cases");
            int indexToRemove = -1;

            for (int i = 0; i < casesProp.arraySize; i++)
            {
                SerializedProperty element = casesProp.GetArrayElementAtIndex(i);
                string portName = GetPortNameFromProperty(element);
                var port = node.GetOutputPort(portName);

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    indexToRemove = i;
                }

                EditorGUILayout.PropertyField(element, GUIContent.none, true, GUILayout.MinWidth(50));

                if (port != null)
                {
                    GUILayout.FlexibleSpace();
                    NodeEditorGUILayout.PortField(GUIContent.none, port, GUILayout.Width(20));
                }

                EditorGUILayout.EndHorizontal();
            }

            if (indexToRemove != -1)
            {
                casesProp.DeleteArrayElementAtIndex(indexToRemove);
                serializedObject.ApplyModifiedProperties();
                SyncPorts();
            }

            if (GUILayout.Button("Add Case", EditorStyles.miniButton))
            {
                casesProp.InsertArrayElementAtIndex(casesProp.arraySize);
                serializedObject.ApplyModifiedProperties();
                SyncPorts();
            }
        }

        protected void SyncPorts()
        {
            var node = target as SwitchNode<T>;
            if (node == null) return;

            var casesProperty = serializedObject.FindProperty("_cases");
            HashSet<string> currentCasePortNames = new HashSet<string>();

            for (int i = 0; i < casesProperty.arraySize; i++)
            {
                string portName = GetPortNameFromProperty(casesProperty.GetArrayElementAtIndex(i));
                currentCasePortNames.Add(portName);

                if (!node.HasPort(portName))
                {
                    node.AddDynamicOutput(typeof(NodeControlExecute), XNode.Node.ConnectionType.Multiple, XNode.Node.TypeConstraint.None, portName);
                }
            }

            List<string> portsToRemove = new List<string>();
            foreach (var port in node.DynamicOutputs)
            {
                if (!currentCasePortNames.Contains(port.fieldName))
                {
                    portsToRemove.Add(port.fieldName);
                }
            }

            foreach (var portName in portsToRemove)
            {
                node.RemoveDynamicPort(portName);
            }
        }

        protected abstract string GetPortNameFromProperty(SerializedProperty prop);
    }
}