namespace SNEngine.Audio
{
    public class ResetStateNode : AudioNodeInteraction
    {
        protected override void Interact(AudioObject input) => input.ResetState();
    }
}
