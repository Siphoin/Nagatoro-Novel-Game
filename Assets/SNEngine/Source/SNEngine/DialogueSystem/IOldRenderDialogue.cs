using UnityEngine;

namespace SNEngine.DialogSystem
{
    public interface IOldRenderDialogue
    {
        Texture2D UpdateRender();

        void Clear();
    }
}
