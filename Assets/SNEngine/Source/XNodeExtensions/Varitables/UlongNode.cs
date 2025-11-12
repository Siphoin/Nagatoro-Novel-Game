namespace SiphoinUnityHelpers.XNodeExtensions.Varitables
{
    [NodeTint("#494d52")]
    public class UlongNode : VaritableNode<ulong>
    {
        public override void SetValue(object value)
        {
            if (value is long longValue)
            {
                SetValue((ulong)longValue);
                return;
            }

            base.SetValue(value);
        }
    }
}