using SNEngine.DialogSystem;
using UnityEngine;

namespace SNEngine.Services
{
    [CreateAssetMenu(menuName = "SNEngine/Services/Render Old Dialogue Service")]
    public class RenderOldDialogueService : ServiceBase, IService, IOldRenderDialogue
    {
        private IOldRenderDialogue _renderDialogue;

        public override void Initialize()
        {
            var render = Resources.Load<OldRenderDialogue>("Render/OldRenderDialogue");

            var prefab = Object.Instantiate(render);

            prefab.name = render.name;

            Object.DontDestroyOnLoad(prefab);

            _renderDialogue = prefab;
        }

        public Texture2D UpdateRender()
        {
            return _renderDialogue.UpdateRender();
        }

        public void Clear()
        {
            _renderDialogue.Clear();
        }
    }
}
