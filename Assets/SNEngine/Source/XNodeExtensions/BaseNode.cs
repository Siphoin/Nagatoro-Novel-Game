using SiphoinUnityHelpers.Attributes;
using SiphoinUnityHelpers.XNodeExtensions.Attributes;
using SNEngine.AsyncNodes;
using System;
using System.Collections;
using UnityEngine;
using XNode;
#if UNITY_EDITOR
using XNodeEditor;
#endif

namespace SiphoinUnityHelpers.XNodeExtensions
{
    [NodeTint("#3b3b3b")]
    [NodeWidth(230)]
    public abstract class BaseNode : Node
    {
        [SerializeField, NodeGuid]
        private string _nodeGuid;

        public string GUID
        {
            get
            {
                if (string.IsNullOrEmpty(_nodeGuid))
                    _nodeGuid = Guid.NewGuid().ToString("N").Substring(0, 15);
                return _nodeGuid;
            }
        }

        public virtual void Execute()
        {
            throw new NotImplementedException($"Node {GetType().Name} has no implementation for Execute()");
        }

        protected T GetDataFromPort<T>(string fieldName)
        {
            return (T)GetDataFromPort(fieldName, typeof(T));
        }

        protected object GetDataFromPort(string fieldName, Type type)
        {
            var inputParentPort = GetInputPort(fieldName);
            if (inputParentPort?.Connection == null)
                return null;

            var value = inputParentPort.Connection.GetOutputValue();
            if (value == null)
                return null;

            if (type == typeof(IEnumerable))
                return value as IEnumerable;

            return Convert.ChangeType(value, type);
        }

        public override object GetValue(NodePort port)
        {
            return null;
        }

        public override string ToString()
        {
            return $"{name} GUID: {GUID} Parent Graph: {graph.name} Is Async? {this is IIncludeWaitingNode}";
        }

#if UNITY_EDITOR
        protected string GetDefaultName()
        {
            return NodeEditorUtilities.NodeDefaultName(GetType());
        }

        [ContextMenu("Reset GUID")]
        private void ResetGuid()
        {
            _nodeGuid = Guid.NewGuid().ToString("N").Substring(0, 15);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
