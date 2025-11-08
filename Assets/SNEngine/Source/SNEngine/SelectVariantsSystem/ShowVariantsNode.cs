using Cysharp.Threading.Tasks;
using SiphoinUnityHelpers.XNodeExtensions;
using SiphoinUnityHelpers.XNodeExtensions.AsyncNodes;
using SiphoinUnityHelpers.XNodeExtensions.Attributes;
using SNEngine.Attributes;
using SNEngine.Debugging;
using SNEngine.Localization;
using SNEngine.Services;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;
namespace SNEngine.SelectVariantsSystem
{
    public class ShowVariantsNode : AsyncNode, ILocalizationNode
    {
        private const int START_VALUE_INDEX = -1;

        [SerializeField, Input(dynamicPortList = true, connectionType = ConnectionType.Override), ReadOnly(ReadOnlyMode.OnEditor)] private string[] _variants = new string[]
        {
            "Variant A",
            "Variant B"
        };
        [Header("Parameters:")]

        [Space]

        [Input(connectionType = ConnectionType.Override), SerializeField, LeftToggle] private bool _hideCharacters = true;

        [Input(connectionType = ConnectionType.Override), SerializeField, LeftToggle] private bool _hideDialogWindow = true;

        [Input(connectionType = ConnectionType.Override), SerializeField, LeftToggle] private bool _returnCharacterVisible = true;

        [Space]

        [SerializeField] private AnimationButtonsType _typeAnimation = AnimationButtonsType.Fade;

        [Header("User selected: (start with 0)")]

        [Space]

        [Output(ShowBackingValue.Never), SerializeField] private int _selectedIndex;

        private int _index;

        public override void Execute()
        {
            base.Execute();

            Show().Forget();


        }

        public override object GetValue(NodePort port)
        {
            return _index;
        }

        private async UniTask Show ()
        {
            _index = START_VALUE_INDEX;

            var variants = _variants.ToArray();

            bool hideDialogWindow = _hideDialogWindow;

            bool hideCharacters = _hideCharacters;

            bool returnCharacterVisible = _returnCharacterVisible;

            var inputHideDialogWindow = GetInputPort(nameof(_hideDialogWindow));

            var inputHideCharacters = GetInputPort(nameof(_hideCharacters));

            var inputReturnCharacterVisible = GetInputPort(nameof(_returnCharacterVisible));

            if (inputHideCharacters.Connection != null)
            {
                hideCharacters = GetDataFromPort<bool>(nameof(_hideCharacters));
            }

            if (inputHideDialogWindow.Connection != null)
            {
                hideDialogWindow = GetDataFromPort<bool>(nameof(_hideDialogWindow));
            }

            if (inputReturnCharacterVisible.Connection != null)
            {
                returnCharacterVisible = GetDataFromPort<bool>(nameof(_returnCharacterVisible));
            }

            for (int i = 0; i < variants.Length; i++)
            {

                string fieldName = $"{nameof(_variants)} {i}";

                var port = GetInputPort(fieldName);

                if (port.Connection != null)
                {
                    variants[i] = GetInputValue<string>(fieldName);
                }

                variants[i] = TextParser.ParseWithProperties(variants[i], graph as BaseGraph);
            }

            var serviceShowVariants = NovelGame.Instance.GetService<SelectVariantsService>();

            serviceShowVariants.OnSelect += OnSelect;

            serviceShowVariants.ShowVariants(variants, hideCharacters, hideDialogWindow, returnCharacterVisible, _typeAnimation);

            while (_index == START_VALUE_INDEX)
            {
                await UniTask.WaitUntil(() => _index == START_VALUE_INDEX, cancellationToken: TokenSource.Token);
            }

        }

        private void OnSelect(int index)
        {
            var serviceShowVariants = NovelGame.Instance.GetService<SelectVariantsService>();

            serviceShowVariants.OnSelect -= OnSelect;

            _index = index;

            StopTask();

        }
        #region Localization
        public object GetOriginalValue()
        {
            return _variants.AsEnumerable();
        }

        public void SetValue(object value)
        {
            if (value is IEnumerable<string> strings)
            {

            }

            else
            {
                NovelGameDebug.LogError($"Error SetValue for node {GetType().Name} GUID {GUID} type not a String collection");
            }
        }

        public object GetValue()
        {
            return _variants.AsEnumerable();
        }
        #endregion
    }
}
