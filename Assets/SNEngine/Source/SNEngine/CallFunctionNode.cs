using SiphoinUnityHelpers.XNodeExtensions;
using SNEngine.Services;
using UnityEngine;

namespace SNEngine
{
    public class CallFunctionNode : BaseNodeInteraction
    {
        [SerializeField] private string _functionName;

        public override void Execute()
        {
            // В реальности здесь должна быть логика вызова функции
            // Для упрощения просто продолжаем выполнение
            Debug.Log($"Calling function: {_functionName}");
        }
    }
}