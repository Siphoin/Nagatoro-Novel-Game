using SNEngine.Animations;
using UnityEngine;

namespace SNEngine.BackgroundSystem
{
    public interface IBackgroundRenderer
    {
        bool UseTransition { get; set; }

        void Clear();
        void ResetState();
        void SetData(Sprite sprite);
    }
}