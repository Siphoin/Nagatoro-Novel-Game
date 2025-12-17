#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using XNodeEditor;
using SiphoinUnityHelpers.XNodeExtensions.Varitables.Set;
using System;
using System.Linq;

namespace SiphoinUnityHelpers.XNodeExtensions.Editor
{
    [CustomNodeEditor(typeof(SetVaritableNode<int>))]
    public class SetVaritableNodeEditor : NodeEditor
    {
        public override void OnBodyGUI()
        {
            serializedObject.Update();
            var inputPort = target.GetInputPort("_varitable");

            if (inputPort != null && !inputPort.IsConnected)
            {
                var guidProp = serializedObject.FindProperty("_targetGuid");
                GUILayout.Space(10);
                EditorGUILayout.BeginVertical(GUI.skin.box);

                string currentGuid = guidProp?.stringValue;
                string displayName = "Select Variable";

                if (!string.IsNullOrEmpty(currentGuid) && target.graph is BaseGraph baseGraph)
                {
                    var linkedNode = baseGraph.GetNodeByGuid(currentGuid) as VaritableNode;
                    if (linkedNode != null)
                    {
                        displayName = $"Target: {linkedNode.Name}";
                    }
                }

                if (GUILayout.Button(displayName, EditorStyles.miniButton))
                {
                    Type genericType = GetGenericType(target.GetType());

                    VaritableSelectorWindow.Open(target.graph as BaseGraph, genericType, (selectedNode) =>
                    {
                        var innerProp = serializedObject.FindProperty("_targetGuid");
                        if (innerProp != null)
                        {
                            innerProp.stringValue = selectedNode.GUID;
                            serializedObject.ApplyModifiedProperties();
                        }
                    });
                }

                EditorGUILayout.EndVertical();
            }
            base.OnBodyGUI();

            serializedObject.ApplyModifiedProperties();
        }

        private Type GetGenericType(Type type)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SetVaritableNode<>))
                {
                    return type.GetGenericArguments()[0];
                }
                type = type.BaseType;
            }
            return null;
        }
    }
}
#endif