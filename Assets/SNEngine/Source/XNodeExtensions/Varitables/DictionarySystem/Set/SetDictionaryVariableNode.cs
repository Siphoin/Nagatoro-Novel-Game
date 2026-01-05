using SiphoinUnityHelpers.XNodeExtensions.Variables.DictionarySystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SiphoinUnityHelpers.XNodeExtensions.Variables.DictionarySystem.Set
{
    public abstract class SetDictionaryVariableNode<TKey, TValue> : BaseNodeInteraction
    {
        [Input(ShowBackingValue.Never, ConnectionType.Override), SerializeField] private DictionaryVariableNode<TKey, TValue> _dictionary;
        [Input, SerializeField] private TKey _key;
        [Input, SerializeField] private TValue _value;

        public override void Execute()
        {
            var dictPort = GetInputPort(nameof(_dictionary));
            var keyPort = GetInputPort(nameof(_key));
            var valuePort = GetInputPort(nameof(_value));

            TKey finalKey = GetInputValue(nameof(_key), _key);
            TValue finalValue = GetInputValue(nameof(_value), _value);

            var connections = dictPort.GetConnections();

            foreach (var port in connections)
            {
                if (port.node is DictionaryVariableNode<TKey, TValue> dictNode)
                {
                    dictNode[finalKey] = finalValue;
                    OnValueApplied(dictNode, finalKey, finalValue);
                }
            }
        }

        protected virtual void OnValueApplied(DictionaryVariableNode<TKey, TValue> node, TKey key, TValue value) { }
    }
}