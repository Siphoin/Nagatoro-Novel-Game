using SNEngine.Polling;
using SNEngine.SpriteObjectSystem;
using SNEngine.Utils;
using UnityEngine;

namespace SNEngine.Services
{
    [CreateAssetMenu(menuName = "SNEngine/Services/Sprite Object Service")]
    public class SpriteObjectService : ServiceBase
    {
        private PoolMono<SpriteObject> _spriteObjects;
        public override void Initialize()
        {
            var prefab = ResourceLoader.LoadCustomOrVanilla<SpriteObject>("Render/SpriteObject");
            var container = new GameObject($"{nameof(SpriteObject)}s");
            _spriteObjects = new(prefab, container.transform, 9, true);
            DontDestroyOnLoad(container);
        }

        public ISpriteObject GetSpriteObject ()
        {
            return _spriteObjects.GetFreeElement();
        }
    }
}
