using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SNEngine.SelectVariantsSystem;
using SiphoinUnityHelpers.XNodeExtensions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SNEngine.Editor.SNILSystem.Workers
{
    public class ShowVariantsNodeWorker : SNILWorker
    {
        public override void ApplyParameters(BaseNode node, Dictionary<string, string> parameters)
        {
            // Проверяем, что это действительно ShowVariantsNode или его наследник
            if (!(node is ShowVariantsNode showVariantsNode))
            {
                Debug.LogError($"Node {node.GetType().Name} is not a ShowVariantsNode");
                return;
            }

            // Устанавливаем параметры для ShowVariantsNode
            foreach (var param in parameters)
            {
                switch (param.Key.ToLower())
                {
                    case "variants":
                        // Разбираем строку с вариантами
                        string variantsStr = param.Value;

                        // Handle the format: "Option 1, Option 2, Option 3"
                        // Split by comma, but be careful about commas that might be inside text
                        string[] variantsArray = ParseOptionsString(variantsStr);

                        // Устанавливаем варианты через рефлексию
                        SetVariantsValue(showVariantsNode, variantsArray);
                        break;
                    default:
                        Debug.LogWarning($"Unknown parameter for ShowVariantsNode: {param.Key}");
                        break;
                }
            }
        }

        private void SetVariantsValue(Object obj, string[] values)
        {
            // Set the _variants field which is the default variants
            FieldInfo variantsField = obj.GetType().GetField("_variants", BindingFlags.NonPublic | BindingFlags.Instance);
            if (variantsField != null)
            {
                variantsField.SetValue(obj, values);
            }

            // Also try to set _currentVariants if it exists
            FieldInfo currentVariantsField = obj.GetType().GetField("_currentVariants", BindingFlags.NonPublic | BindingFlags.Instance);
            if (currentVariantsField != null)
            {
                currentVariantsField.SetValue(obj, values);
            }
        }

        private string[] ParseOptionsString(string input)
        {
            // Simple approach: split by comma
            // For more complex parsing, we might need to handle commas inside text
            string[] parts = input.Split(',');

            // Trim whitespace from each part
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i].Trim();
            }

            return parts;
        }
    }
}