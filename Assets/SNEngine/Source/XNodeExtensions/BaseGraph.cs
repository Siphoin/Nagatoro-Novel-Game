using Cysharp.Threading.Tasks;
using SiphoinUnityHelpers.XNodeExtensions.AsyncNodes;
using SiphoinUnityHelpers.XNodeExtensions.Attributes;
using SiphoinUnityHelpers.XNodeExtensions.Debugging;
using SiphoinUnityHelpers.XNodeExtensions.Exceptions;
using SNEngine.AsyncNodes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using XNode;

namespace SiphoinUnityHelpers.XNodeExtensions
{
    public abstract class BaseGraph : NodeGraph
    {
        [SerializeField]
        private string _guid;

        private NodeQueue _queue;

        private IDictionary<string, VaritableNode> _varitables;

        public event Action OnEndExecute;

        public event Action<BaseNode> OnNextNode;
        new private List<Node> nodes => base.nodes;

        public bool IsPaused { get; private set; }

        public bool IsRunning => _queue is null ? false : !_queue.IsEnding;

        protected NodeQueue Queue => _queue;

        public IDictionary<string, VaritableNode> Varitables => _varitables;

        public IReadOnlyDictionary<string, BaseNode> AllNodes
        {
            get
            {
                Dictionary<string, BaseNode> data = new();
                var baseNodes = nodes.OfType<BaseNode>();
                foreach (var node in baseNodes)
                {
                    var key = node.GUID;
                    data.Add(key, node);
                }

                return data;
            }
        }

        public string GUID
        {
            get
            {
#if UNITY_EDITOR
                RegenerateGUID();
#endif
                return _guid;
            }
        }
#if UNITY_EDITOR
        private void RegenerateGUID()
        {
            if (string.IsNullOrEmpty(_guid))
                ResetGUID();
        }

        private void ResetGUID()
        {
            _guid = Guid.NewGuid().ToString("N").Substring(0, 15);
        }

        private void Awake()
        {
            RegenerateGUID();
        }
#endif

        public virtual void Execute ()
        {
            var queue = new List<BaseNodeInteraction>();
            var normalizeNodes = TopologicalSortInteractionNodes();
            BuidVaritableNodes();

            for (int i = 0; normalizeNodes.Count > i; i++)
            {
                var node = normalizeNodes[i];
                queue.Add(node);
            }

            _queue = new NodeQueue(this, queue);

            ExecuteProcess().Forget();

        }

        protected void BuidVaritableNodes()
        {
            if (_varitables is null)
            {
                Dictionary<string, VaritableNode> nodes = new Dictionary<string, VaritableNode>();

                foreach (var node in this.nodes)
                {
                    if (node is VaritableNode)
                    {
                        VaritableNode varitableNode = node as VaritableNode;
                        nodes.Add(varitableNode.Name, varitableNode);
                    }
                }

                _varitables = nodes;
            }
        }

        public T GetValueFromVaritable<T>(string name)
        {
            var node = _varitables[name];

            if (node is null)
            {
                throw new NullReferenceException($"Varitable Node with name not found");
            }

            var value = node.GetCurrentValue();

            if (value.GetType() != typeof(T))
            {
                throw new InvalidCastException($"varitable node {node.Name} have type {value.GetType()}. Argument type {typeof(T)}");
            }

            return (T)value;
        }

        public virtual void Continue ()
        {
            IsPaused = true;
        }

        public virtual void Pause ()
        {
            IsPaused = false;
        }

        public virtual void Stop ()
        {
            End();

            _queue.Exit();       
        }

        private List<BaseNodeInteraction> TopologicalSortInteractionNodes()
        {
            List<BaseNodeInteraction> executableNodes = nodes
                .OfType<BaseNodeInteraction>()
                .ToList();

            var allInteractionNodes = executableNodes
                .ToDictionary(node => node, node => 0);

            foreach (var node in executableNodes)
            {
                foreach (var outputPort in node.Ports.Where(p => p.IsOutput && p.IsConnected))
                {
                    foreach (var connection in outputPort.GetConnections())
                    {
                        if (connection.node is BaseNodeInteraction nextNode)
                        {
                            if (allInteractionNodes.ContainsKey(nextNode))
                            {
                                allInteractionNodes[nextNode]++;
                            }
                        }
                    }
                }
            }

            var queue = new Queue<BaseNodeInteraction>(
                allInteractionNodes.Where(pair => pair.Value == 0).Select(pair => pair.Key)
            );

            var sortedList = new List<BaseNodeInteraction>();

            while (queue.Count > 0)
            {
                var currentNode = queue.Dequeue();

                if (!currentNode.IsControlledAnotherNode)
                {
                    sortedList.Add(currentNode);
                }
                foreach (var outputPort in currentNode.Ports.Where(p => p.IsOutput && p.IsConnected))
                {
                    foreach (var connection in outputPort.GetConnections())
                    {
                        if (connection.node is BaseNodeInteraction nextNode)
                        {
                            if (allInteractionNodes.ContainsKey(nextNode))
                            {
                                allInteractionNodes[nextNode]--;

                                if (allInteractionNodes[nextNode] == 0)
                                {
                                    queue.Enqueue(nextNode);
                                }
                            }
                        }
                    }
                }
            }

            return sortedList;
        }

        public virtual void JumptToNode(string targetGuid)
    {
        BuidVaritableNodes();

        var allInteractionNodes = nodes
            .OfType<BaseNodeInteraction>()
            .ToList();

            allInteractionNodes = TopologicalSortInteractionNodes();

        int targetIndex = allInteractionNodes.FindIndex(node => node.GUID == targetGuid);

        List<BaseNodeInteraction> filteredNodes;

        if (targetIndex != -1)
        {

            filteredNodes = allInteractionNodes
                .Select((node, index) => new { Node = node, Index = index })
                .Where(item =>
                {
                    if (item.Index < targetIndex)
                    {
                        if (item.Node is IIncludeWaitingNode waitNode)
                        {
                            waitNode.SkipWait();
                        }

                        return item.Node.CanSkip() == false;
                    }
                    else
                    {
                        return true;
                    }
                })
                .Select(item => item.Node)
                .ToList();
        }
        else
        {
            filteredNodes = new List<BaseNodeInteraction>();
        }

        _queue = new NodeQueue(this, filteredNodes);

        ExecuteProcess().Forget();
    }





    public BaseNode GetNodeByGuid (string guid)
        {
            foreach (var node in from item in nodes
                                 let node = item as BaseNode
                                 where node.GUID == guid
                                 select node)
            {
                return node;
            }

            return null;
        }

        private async UniTask ExecuteProcess ()
        {
            _queue.OnEnd += End;

            for (int i = 0; i < _queue.Count; i++)
            {
                await UniTask.WaitUntil(() => !IsPaused);

                var node = await _queue.Next();

                if (node is null)
                {
                    break;
                }

                OnNextNode?.Invoke(node);

               
            }
        }

        private void End()
        {
            _queue.OnEnd -= End;

            OnEndExecute?.Invoke();

            ResetVaritables();

            XNodeExtensionsDebug.Log($"graph {name} end execute");
        }

        private void ResetVaritables()
        {
            foreach (var node in nodes.Where(node => node is VaritableNode))
            {
                VaritableNode varitableNode = node as VaritableNode;

                varitableNode.ResetValue();
            }
        }
    }
}
