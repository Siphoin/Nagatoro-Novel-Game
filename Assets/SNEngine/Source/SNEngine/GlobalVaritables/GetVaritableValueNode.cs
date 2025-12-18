using SiphoinUnityHelpers.XNodeExtensions;
using SNEngine.Debugging;
using SNEngine.Graphs;
using SNEngine.Services;
using System.Linq;
using UnityEngine;
using XNode;

namespace SNEngine.GlobalVaritables
{
    public abstract class GetVaritableValueNode<T> : BaseNode
    {
        [SerializeField, HideInInspector] private string _guidVaritable;
        [Output] private T _result;

        public override object GetValue(NodePort port)
        {
            if (!Application.isPlaying || port.fieldName != "_result") return null;

            var service = NovelGame.Instance.GetService<VaritablesContainerService>();
            if (service?.GlobalVaritables == null) return null;

            string targetGuid = _guidVaritable?.Trim();
            if (string.IsNullOrEmpty(targetGuid)) return default(T);

            var varitableNode = service.GlobalVaritables.Values.FirstOrDefault(v =>
                v != null && v.GUID != null && v.GUID.Trim() == targetGuid);

            return varitableNode != null ? varitableNode.GetCurrentValue() : default(T);
        }
    }
}