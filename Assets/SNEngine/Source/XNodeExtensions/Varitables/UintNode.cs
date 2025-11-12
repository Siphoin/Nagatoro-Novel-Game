namespace SiphoinUnityHelpers.XNodeExtensions.Varitables
{
    [NodeTint("#524a4a")]
    public class UintNode : VaritableNode<uint>
    {
        public override void SetValue(object value)
        {
            if (value is long longValue)
            {
                SetValue((uint)longValue);
                return;
            }

            base.SetValue(value);
        }
    }
}