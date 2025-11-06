using SNEngine.BackgroundSystem;
using SNEngine.Debugging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SNEngine.Services
{
    [CreateAssetMenu(menuName = "SNEngine/Services/Bavkground Service")]
    public class BackgroundService : ServiceBase
    {
        private IBackgroundRenderer _background;

        public override void Initialize()
        {
            var background = Resources.Load<BackgroundRenderer>("Render/Background");

            var screenBackground = Resources.Load<ScreenBackgroundRender>("Render/ScreenBackground");

            var screenBackgroundPrefab = Instantiate(screenBackground);

            screenBackgroundPrefab.name = screenBackground.name;

            Object.DontDestroyOnLoad(screenBackgroundPrefab);

            var backgroundPrefab = Instantiate(background);

            backgroundPrefab.name = background.name;

            Object.DontDestroyOnLoad(backgroundPrefab);

            _background = backgroundPrefab;
        }

        public override void ResetState()
        {
            _background.ResetState();
        }

        public void Set(Sprite sprite)
        {
            if (sprite is null)
            {
                NovelGameDebug.LogError($"Sprite for set background not seted. Check your graph");
            }

            _background.SetData(sprite);
        }

        public void Clear()
        {
            _background.Clear();
        }
    }

}
