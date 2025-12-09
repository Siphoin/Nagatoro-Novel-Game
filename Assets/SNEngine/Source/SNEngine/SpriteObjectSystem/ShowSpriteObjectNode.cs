using UnityEngine;

namespace SNEngine.SpriteObjectSystem
{
    public class ShowSpriteObjectNode : SpriteObjectInteractionNode
    {
        [Input(ShowBackingValue.Unconnected), SerializeField] private Sprite _sprite;

        protected override async void Interact(SpriteObject input)
        {
            Sprite inputSprite = GetInputValue(nameof(_sprite), _sprite);

            if (inputSprite != null)
            {
                input.SetSprite(inputSprite);
            }
            
            input.Show();
        }
    }
}