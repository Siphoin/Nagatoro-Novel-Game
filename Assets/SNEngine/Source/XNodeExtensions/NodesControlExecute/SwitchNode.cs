using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using XNode;

namespace SiphoinUnityHelpers.XNodeExtensions.NodesControlExecutes
{
    public abstract class SwitchNode<T> : NodeControlExecute
    {
        private const float DURATION_SHOW_RESULT = 3.2f;

        [Input(ShowBackingValue.Never, ConnectionType.Override)] public T _value;
        [Output, SerializeField] public NodePort _defaultCase;

        [SerializeField] private List<T> _cases = new List<T>();
        protected IEnumerable<T> Cases => _cases;

        public override void Execute()
        {
            T inputVal = GetInputValue(nameof(_value), _value);
            NodePort targetPort = null;

            for (int i = 0; i < _cases.Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(inputVal, _cases[i]))
                {
                    targetPort = GetOutputPort("case " + i);
                    break;
                }
            }

            if (targetPort == null || !targetPort.IsConnected)
            {
                targetPort = GetOutputPort(nameof(_defaultCase));
            }

            if (targetPort != null)
            {
                HighlightBranchRecursiveAsync(targetPort).Forget();
                ExecuteNodesFromPort(targetPort).Forget();
            }
        }

        protected override void Init()
        {
            SyncPorts();
        }

        public override void UpdatePorts()
        {
            SyncPorts();
        }

        private void SyncPorts()
        {
            for (int i = 0; i < _cases.Count; i++)
            {
                string portName = "case " + i;
                if (!HasPort(portName))
                {
                    AddDynamicOutput(typeof(NodeControlExecute), ConnectionType.Multiple, TypeConstraint.None, portName);
                }
            }

            List<NodePort> portsToRemove = DynamicOutputs
                .Where(p => p.fieldName.StartsWith("case "))
                .Where(p =>
                {
                    int index;
                    if (int.TryParse(p.fieldName.Replace("case ", ""), out index))
                    {
                        return index >= _cases.Count;
                    }
                    return true;
                })
                .ToList();

            foreach (var port in portsToRemove)
            {
                RemoveDynamicPort(port);
            }
        }

        protected virtual void OnValidate()
        {
            SyncPorts();
        }

        private async UniTaskVoid HighlightBranchRecursiveAsync(NodePort startPort)
        {
            Color color = Color.yellow;
            HashSet<Node> branchNodes = new HashSet<Node> { this };
            GetChildNodesRecursive(startPort, branchNodes);

            foreach (var node in branchNodes)
            {
                NodeHighlighter.HighlightNode(node, color);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(DURATION_SHOW_RESULT));

            foreach (var node in branchNodes)
            {
                if (node != null)
                    NodeHighlighter.RemoveHighlight(node);
            }
        }

        private void GetChildNodesRecursive(NodePort port, HashSet<Node> visited)
        {
            if (port == null || !port.IsConnected) return;

            foreach (var connection in port.GetConnections())
            {
                Node nextNode = connection.node;
                if (nextNode != null && !visited.Contains(nextNode))
                {
                    visited.Add(nextNode);
                    foreach (NodePort output in nextNode.Outputs)
                    {
                        GetChildNodesRecursive(output, visited);
                    }
                }
            }
        }
    }
}