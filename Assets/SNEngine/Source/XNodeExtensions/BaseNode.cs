using SiphoinUnityHelpers.Attributes;
using SiphoinUnityHelpers.XNodeExtensions.Attributes;
using SNEngine.AsyncNodes;
using System;
using System.Collections;
using System.Collections.Generic;
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
#if UNITY_EDITOR
                RegenerateGUID();
#endif
                return _nodeGuid;
            }
        }
#if UNITY_EDITOR
        private void RegenerateGUID()
        {
            if (string.IsNullOrEmpty(_nodeGuid))
                ResetGUID();
        }

        private void ResetGUID()
        {
            _nodeGuid = Guid.NewGuid().ToString("N").Substring(0, 15);
        }

#endif

#if UNITY_EDITOR

        private void Awake()
        {
            RegenerateGUID();

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

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

        public virtual bool CanSkip()
        {
            return true;
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
