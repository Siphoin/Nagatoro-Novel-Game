using UnityEngine;
using XNode;

namespace SiphoinUnityHelpers.XNodeExtensions.Variables.DictionarySystem.Get
{
    public abstract class GetDictionaryVariableNode<TKey, TValue> : BaseNode
    {
        [Input(ShowBackingValue.Never, ConnectionType.Override)] public DictionaryVariableNode<TKey, TValue> _dictionary;
        [Input] public TKey _key;
        [Output] public TValue _value;

        public override object GetValue(NodePort port)
        {
            if (port.fieldName == nameof(_value))
            {
                DictionaryVariableNode<TKey, TValue> dictNode = GetInputValue(nameof(_dictionary), _dictionary);
                TKey key = GetInputValue(nameof(_key), _key);

                if (dictNode != null && key != null)
                {
                    if (dictNode.TryGetValue(key, out TValue result))
                    {
                        return result;
                    }
                }
                return default(TValue);
            }
            return null;
        }
    }
}