using UnityEngine;
using XNode;

namespace SiphoinUnityHelpers.XNodeExtensions.Varitables
{
    [NodeTint("#524949")]
    public class FloatNode : VaritableNode<float>
    {
        public override void SetValue(object value)
        {
            if (value is double doubleValue)
            {
                SetValue((float)doubleValue);
                return;
            }

            base.SetValue(value);
        }
    }
}