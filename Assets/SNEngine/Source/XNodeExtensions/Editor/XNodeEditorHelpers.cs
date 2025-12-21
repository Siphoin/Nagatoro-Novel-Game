#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using XNodeEditor;
using System;
using System.Linq;
using SiphoinUnityHelpers.XNodeExtensions.Variables.Set;
using SNEngine.Graphs;
using SNEngine.GlobalVariables;

namespace SiphoinUnityHelpers.XNodeExtensions.Editor
{
    public static class XNodeEditorHelpers
    {
        public static void DrawGetGlobalVaritableBody(NodeEditor editor, SerializedObject serializedObject)
        {
            serializedObject.Update();

            foreach (var tag in NodeEditorGUILayout.GetFilteredFields(serializedObject))
            {
                if (tag.name == "_guidVaritable" || tag.name == "_result") continue;

                SerializedProperty prop = serializedObject.FindProperty(tag.name);
                if (prop != null) NodeEditorGUILayout.PropertyField(prop);
            }

            DrawSelector(editor, serializedObject, "_guidVaritable", VariableselectorWindow.SelectorMode.GlobalOnly);

            XNode.NodePort resultPort = editor.target.GetOutputPort("_result");
            if (resultPort != null)
            {
                NodeEditorGUILayout.PortField(new GUIContent("Value"), resultPort);
            }

            serializedObject.ApplyModifiedProperties();
        }

        public static void DrawSetVaritableBody(NodeEditor editor, SerializedObject serializedObject)
        {
            serializedObject.Update();

            foreach (var tag in NodeEditorGUILayout.GetFilteredFields(serializedObject))
            {
                if (tag.name == "_targetGuid") continue;

                if (tag.name == "_varitable")
                {
                    XNode.NodePort p = editor.target.GetInputPort("_varitable");
                    if (p != null)
                    {
                        if (p.IsConnected)
                        {
                            NodeEditorGUILayout.PropertyField(serializedObject.FindProperty(tag.name));
                        }
                        else
                        {
                            DrawSelector(editor, serializedObject, "_targetGuid", VariableselectorWindow.SelectorMode.All);
                        }
                    }
                    continue;
                }

                SerializedProperty prop = serializedObject.FindProperty(tag.name);
                if (prop != null)
                {
                    NodeEditorGUILayout.PropertyField(prop);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawSelector(NodeEditor editor, SerializedObject serializedObject, string propertyName, VariableselectorWindow.SelectorMode mode)
        {
            var guidProp = serializedObject.FindProperty(propertyName);
            if (guidProp == null) return;

            GUILayout.Space(4);
            string currentGuid = guidProp.stringValue;
            string displayName = "Select Variable";
            Color buttonColor = new Color(0.25f, 0.25f, 0.25f);

            if (!string.IsNullOrEmpty(currentGuid))
            {
                VariableNode linkedNode = FindVariableByGuid(editor.target.graph as BaseGraph, currentGuid);
                if (linkedNode != null)
                {
                    displayName = linkedNode.Name;
                    buttonColor = linkedNode.Color;
                }
                else displayName = "<Missing>";
            }

            Color prevColor = GUI.backgroundColor;
            GUI.backgroundColor = buttonColor;

            if (GUILayout.Button(displayName, GUILayout.Height(24)))
            {
                Type genericType = GetGenericType(editor.target.GetType());
                VariableselectorWindow.Open(editor.target.graph as BaseGraph, genericType, (selectedNode) =>
                {
                    var so = new SerializedObject(editor.target);
                    so.FindProperty(propertyName).stringValue = selectedNode.GUID;
                    so.ApplyModifiedProperties();
                }, mode);
            }
            GUI.backgroundColor = prevColor;
        }

        private static VariableNode FindVariableByGuid(BaseGraph currentGraph, string guid)
        {
            if (currentGraph != null)
            {
                var localNode = currentGraph.GetNodeByGuid(guid) as VariableNode;
                if (localNode != null) return localNode;
            }

            var containers = Resources.LoadAll<VariableContainerGraph>("");
            foreach (var container in containers)
            {
                var node = container.nodes.OfType<VariableNode>().FirstOrDefault(n => n.GUID == guid);
                if (node != null) return node;
            }
            return null;
        }

        private static Type GetGenericType(Type type)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType)
                {
                    Type def = type.GetGenericTypeDefinition();
                    if (def == typeof(SetVariableNode<>) ||
                        def.Name.StartsWith("GetVaritableValueNode") ||
                        def.Name.StartsWith("GetVaritableValueFromGlobalContainerNode"))
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