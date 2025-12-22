using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SiphoinUnityHelpers.XNodeExtensions;
using SNEngine.DialogSystem;
using SNEngine.Editor.SNILSystem.Workers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SNEngine.Editor.SNILSystem
{
    public class SNILParameterApplier
    {
        public static void ApplyParametersToNode(BaseNode node, Dictionary<string, string> parameters, string nodeName)
        {
            SNILWorker worker = GetWorkerForNode(node, nodeName);
            worker?.ApplyParameters(node, parameters);
        }

        public static void ApplyParametersToNodeGeneric(BaseNode node, Dictionary<string, string> parameters)
        {
            // Общий метод для применения параметров через рефлексию
            System.Type type = node.GetType();
            var fields = GetAllFields(type);

            foreach (var kvp in parameters)
            {
                var field = fields.FirstOrDefault(f =>
                    f.Name.Equals(kvp.Key, System.StringComparison.OrdinalIgnoreCase) ||
                    f.Name.Equals("_" + kvp.Key, System.StringComparison.OrdinalIgnoreCase));

                if (field != null)
                {
                    object val = ConvertValue(kvp.Value, field.FieldType);
                    if (val != null || !field.FieldType.IsValueType)
                    {
                        field.SetValue(node, val);
                    }
                }
            }
        }

        private static SNILWorker GetWorkerForNode(BaseNode node, string nodeName)
        {
            // Сначала пытаемся получить информацию о воркере из шаблона
            var templateInfo = SNILTemplateManager.GetTemplateInfo(nodeName);
            if (!string.IsNullOrEmpty(templateInfo?.WorkerName))
            {
                // Используем рефлексию для создания экземпляра воркера по имени
                return CreateWorkerByName(templateInfo.WorkerName);
            }

            // Если воркер не указан в шаблоне, используем логику по умолчанию
            if (node is DialogNode)
            {
                return new DialogNodeWorker();
            }
            else if (nodeName.Equals("StartNode", System.StringComparison.OrdinalIgnoreCase))
            {
                return new StartNodeWorker();
            }
            else if (nodeName.Equals("ExitNode", System.StringComparison.OrdinalIgnoreCase))
            {
                return new ExitNodeWorker();
            }
            
            // Для других типов нод возвращаем общий воркер
            return new GenericNodeWorker();
        }

        private static SNILWorker CreateWorkerByName(string workerName)
        {
            // Пытаемся найти тип воркера в текущей сборке
            System.Type workerType = null;
            
            // Сначала ищем с полным именем класса
            workerType = System.Type.GetType($"SNEngine.Editor.SNILSystem.Workers.{workerName}");
            
            // Если не нашли, ищем в текущей сборке
            if (workerType == null)
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    workerType = assembly.GetTypes()
                        .FirstOrDefault(t => t.Name == workerName && 
                                           typeof(Workers.SNILWorker).IsAssignableFrom(t));
                    if (workerType != null) break;
                }
            }

            if (workerType != null)
            {
                return (SNILWorker)Activator.CreateInstance(workerType);
            }

            // Если не удалось создать воркер по имени, возвращаем общий воркер
            return new GenericNodeWorker();
        }

        private static FieldInfo[] GetAllFields(System.Type type)
        {
            var fields = new List<FieldInfo>();
            while (type != null && type != typeof(object))
            {
                fields.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));
                type = type.BaseType;
            }
            return fields.ToArray();
        }

        private static object ConvertValue(string value, System.Type targetType)
        {
            if (targetType == typeof(string)) return value;
            if (targetType == typeof(int)) return int.TryParse(value, out int i) ? i : 0;
            if (targetType == typeof(float)) return float.TryParse(value, out float f) ? f : 0f;
            if (targetType == typeof(bool)) return bool.TryParse(value, out bool b) ? b : false;
            if (targetType.IsEnum) return System.Enum.Parse(targetType, value, true);

            if (typeof(Object).IsAssignableFrom(targetType))
            {
                string filter = $"t:{targetType.Name} {value}";
                string[] guids = AssetDatabase.FindAssets(filter);
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    return AssetDatabase.LoadAssetAtPath(path, targetType);
                }
            }

            return null;
        }
    }
    
    // Добавляем общий воркер для остальных нод
    public class GenericNodeWorker : Workers.SNILWorker
    {
        public override void ApplyParameters(BaseNode node, Dictionary<string, string> parameters)
        {
            // Применяем параметры общим способом через рефлексию
            SNILParameterApplier.ApplyParametersToNodeGeneric(node, parameters);
        }
    }
}