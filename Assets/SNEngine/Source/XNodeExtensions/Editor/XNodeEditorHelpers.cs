#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using XNodeEditor;
using System;
using SiphoinUnityHelpers.XNodeExtensions.Varitables.Set;

namespace SiphoinUnityHelpers.XNodeExtensions.Editor
{
    public static class XNodeEditorHelpers
    {
        public static void DrawSetVaritableBody(NodeEditor editor, SerializedObject serializedObject)
        {
            serializedObject.Update();

            bool isCollectionStorage = editor.target.GetType().IsSubclassOf(typeof(VaritableNode)) &&
                                      !editor.target.GetType().Name.StartsWith("Set");

            foreach (var tag in NodeEditorGUILayout.GetFilteredFields(serializedObject))
            {
                if (tag.name == "_varitable" || tag.name == "_targetGuid" || tag.name == "_enumerable") continue;

                if (tag.name == "_elements" && !isCollectionStorage) continue;

                SerializedProperty prop = serializedObject.FindProperty(tag.name);
                if (prop != null)
                {
                    NodeEditorGUILayout.PropertyField(prop);
                }
            }

            var inputPort = editor.target.GetInputPort("_varitable");

            if (inputPort != null && !inputPort.IsConnected)
            {
                var guidProp = serializedObject.FindProperty("_targetGuid");
                GUILayout.Space(8);

                string currentGuid = guidProp?.stringValue;
                string displayName = "Select Variable";
                Color buttonColor = new Color(0.25f, 0.25f, 0.25f);

                if (!string.IsNullOrEmpty(currentGuid) && editor.target.graph is BaseGraph baseGraph)
                {
                    var linkedNode = baseGraph.GetNodeByGuid(currentGuid) as VaritableNode;
                    if (linkedNode != null)
                    {
                        displayName = linkedNode.Name;
                        buttonColor = linkedNode.Color;
                    }
                    else
                    {
                        displayName = "<Missing>";
                        buttonColor = new Color(0.5f, 0.2f, 0.2f);
                    }
                }

                Color prevColor = GUI.backgroundColor;
                GUI.backgroundColor = buttonColor;

                if (GUILayout.Button(displayName, GUILayout.Height(30)))
                {
                    Type genericType = GetGenericType(editor.target.GetType());
                    VaritableSelectorWindow.Open(editor.target.graph as BaseGraph, genericType, (selectedNode) =>
                    {
                        var innerSerialized = new SerializedObject(editor.target);
                        var innerProp = innerSerialized.FindProperty("_targetGuid");
                        if (innerProp != null)
                        {
                            innerProp.stringValue = selectedNode.GUID;
                            innerSerialized.ApplyModifiedProperties();
                        }
                    });
                }

                GUI.backgroundColor = prevColor;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static Type GetGenericType(Type type)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType)
                {
                    Type def = type.GetGenericTypeDefinition();
                    if (def == typeof(SetVaritableNode<>) || def == typeof(VaritableCollectionNode<>))
                    {
                        return type.GetGenericArguments()[0];
                    }
                }
                type = type.BaseType;
            }
            return null;
        }
    }
}
#endif