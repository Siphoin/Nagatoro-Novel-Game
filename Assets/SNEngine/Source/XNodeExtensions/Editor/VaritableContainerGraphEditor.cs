using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;
using SiphoinUnityHelpers.XNodeExtensions;

namespace SiphoinUnityHelpers.XNodeExtensions.Editor
{
    // Конкретная реализация для VaritableContainerGraph - фильтруем по VaritableNode и SetVaritableNode производным
    [CustomNodeGraphEditor(typeof(SNEngine.Graphs.VaritableContainerGraph))]
    public class VaritableContainerGraphEditor : FilteredNodeGraphEditor
    {
        protected override bool IsNodeTypeAllowed(Type nodeType)
        {
            // Проверяем, является ли тип производным от VaritableNode
            if (typeof(VaritableNode).IsAssignableFrom(nodeType))
                return true;

            // Проверяем, является ли тип производным от SetVaritableNode<T> (любого типа T)
            var currentType = nodeType;
            while (currentType != null && currentType != typeof(object))
            {
                if (currentType.IsGenericType)
                {
                    var genericTypeDef = currentType.GetGenericTypeDefinition();
                    // Проверяем, является ли базовый тип одним из SetVaritableNode<T>
                    if (genericTypeDef.Name.StartsWith("SetVaritableNode"))
                    {
                        return true;
                    }
                }
                currentType = currentType.BaseType;
            }

            // Дополнительно проверяем, если имя класса содержит "SetVaritable" или является специфичным типом
            if (nodeType.Name.Contains("SetVaritable"))
                return true;

            return false; // Все остальные типы запрещены
        }
    }
}