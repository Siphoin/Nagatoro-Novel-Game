using SiphoinUnityHelpers.XNodeExtensions;
using SiphoinUnityHelpers.XNodeExtensions.AsyncNodes;
using SNEngine.Animations;
using SNEngine.Attributes;
using SNEngine.Debugging;
using SNEngine.Localization;
using UnityEngine;

namespace SNEngine
{
    public abstract class PrinterTextNode : AsyncNode, IPrinterNode, ILocalizationNode
    {
        [SerializeField, TextArea(10, 100)] private string _text = "Some Text";
        private string _currentText;

        public override void Execute()
        {
            if (string.IsNullOrEmpty(_currentText))
            {
                _currentText = _text;
            }
            base.Execute();
        }

        public string GetText()
        {
            if (string.IsNullOrEmpty(_currentText))
            {
                _currentText = _text;
            }
            return TextParser.ParseWithProperties(_currentText, graph as BaseGraph);
        }

        public void MarkIsEnd()
        {
            _currentText = string.Empty;
            StopTask();
        }

        #region Localization
        public object GetOriginalValue()
        {
            return _text;
        }

        public object GetValue()
        {
            return _text;
        }

        public void SetValue(object value)
        {
            if (value is string == false)
            {
                NovelGameDebug.LogError($"Error SetValue for node {GetType().Name} GUID {GUID} type not a String");
                return;
            }

            _currentText = value.ToString();
            #endregion
        }
    }
}
