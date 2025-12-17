using UnityEngine;

namespace SiphoinUnityHelpers.XNodeExtensions.Varitables.Set
{
    public abstract class SetVaritableNode<T> : BaseNodeInteraction
    {
        [Input(ShowBackingValue.Never), SerializeField] private T _varitable;

        [Input, SerializeField] private T _value;

        [HideInInspector, SerializeField] private string _targetGuid;

        public string TargetGuid { get => _targetGuid; set => _targetGuid = value; }

        public override void Execute()
        {
            var outputVaritable = GetInputPort(nameof(_varitable));
            var inputValue = GetInputPort(nameof(_value));
            var connectedVaritables = outputVaritable.GetConnections();

            object finalValue = _value;
            var connectedValue = inputValue.Connection;

            if (connectedValue != null)
            {
                finalValue = connectedValue.GetOutputValue();
            }

            if (connectedVaritables.Count == 0 && !string.IsNullOrEmpty(_targetGuid))
            {
                if (graph is BaseGraph baseGraph)
                {
                    var targetNode = baseGraph.GetNodeByGuid(_targetGuid);
                    if (targetNode is VaritableNode<T> typedNode)
                    {
                        if (finalValue is T castValue)
                        {
                            typedNode.SetValue(castValue);
                        }
                        else
                        {
                            try
                            {
                                typedNode.SetValue((T)System.Convert.ChangeType(finalValue, typeof(T)));
                            }
                            catch { }
                        }
                    }
                }
            }
            else
            {
                foreach (var port in connectedVaritables)
                {
                    var connectedVaritable = port.node;

                    if (connectedVaritable is VaritableNode<T> varitableNode)
                    {
                        if (finalValue is T castValue)
                        {
                            varitableNode.SetValue(castValue);
                        }
                    }

                    if (connectedVaritable is VaritableCollectionNode<T> collectionNode)
                    {
                        int index = RegexCollectionNode.GetIndex(port);
                        if (finalValue is T castValue)
                        {
                            collectionNode.SetValue(index, castValue);
                        }
                    }
                }
            }
        }
    }
}