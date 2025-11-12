using System;
using UnityEngine;
using XNode;

namespace SiphoinUnityHelpers.XNodeExtensions.Varitables
{
    [NodeTint("#524a4a")]
    public class IntNode : VaritableNode<int>
    {
        public override void SetValue(object value)
        {
            if (value is long longValue)
            {
                SetValue((int)longValue);
                return;
            }

            base.SetValue(value);
        }
    }
}