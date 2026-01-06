using UnityEngine;
using XNode;
using SiphoinUnityHelpers.XNodeExtensions.Attributes;
using SiphoinUnityHelpers.XNodeExtensions.Extensions;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using Newtonsoft.Json.Linq;
using SiphoinUnityHelpers.XNodeExtensions.Debugging;

namespace SiphoinUnityHelpers.XNodeExtensions
{
    public abstract class VariableCollectionNode<T> : VariableNode, IList<T>
    {
        private List<T> _startValue;

        [Space(10)]
        [SerializeField, Output(ShowBackingValue.Always, dynamicPortList = true), ReadOnly(ReadOnlyMode.OnEditor)]
        private List<T> _elements = new List<T>();

        [Space(10)]
        [SerializeField, Output(ShowBackingValue.Never), ReadOnly(ReadOnlyMode.OnEditor)]
        private NodePortEnumerable _enumerable;

        public int Count => _elements.Count;
        public bool IsReadOnly => false;

        public T this[int index]
        {
            get => _elements[index];
            set => _elements[index] = value;
        }

        public override object GetStartValue() => _startValue;

        public override object GetValue(NodePort port)
        {
            if (port.fieldName != nameof(_enumerable))
            {
                int index = RegexCollectionNode.GetIndex(port);
                return _elements[index];
            }

            return _elements.AsEnumerable();
        }

        public void SetValue(IEnumerable<T> value)
        {
            _elements = value.ToList();
        }

        public void SetValue(int index, T value)
        {
            if (index < 0) return;

            if (index >= _elements.Count - 1)
            {
                int countToAdd = index - _elements.Count + 1;
                for (int i = 0; i < countToAdd; i++)
                {
                    _elements.Add(default);
                }
            }

            _elements[index] = value;
        }

        public override object GetCurrentValue() => _elements.ToList();

        public override void ResetValue()
        {
            _elements = _startValue.ToList();
        }

        public override void SetValue(object value)
        {
            if (value is IEnumerable<T> collection)
            {
                _elements = collection.ToList();
            }
            else
            {
                XNodeExtensionsDebug.LogError($"Collection node {GUID} don`t apply the value {value.GetType().Name}");
            }
        }

        public void Add(T item) => _elements.Add(item);
        public void Clear() => _elements.Clear();
        public bool Contains(T item) => _elements.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _elements.CopyTo(array, arrayIndex);
        public bool Remove(T item) => _elements.Remove(item);
        public int IndexOf(T item) => _elements.IndexOf(item);
        public void Insert(int index, T item) => _elements.Insert(index, item);
        public void RemoveAt(int index) => _elements.RemoveAt(index);

        public IEnumerator<T> GetEnumerator() => _elements.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            Validate();
        }

        protected override void Validate()
        {
            base.Validate();
            if (!Application.isPlaying)
            {
                _startValue = _elements.ToList();
            }
        }

        protected override void ValidateName()
        {
            name = Color.ToColorTag($"{Name} ({GetDefaultName()}List)");
        }
#endif
    }
}