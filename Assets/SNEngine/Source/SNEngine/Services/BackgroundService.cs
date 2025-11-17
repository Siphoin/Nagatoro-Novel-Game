using Cysharp.Threading.Tasks;
using DG.Tweening;
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

        #region Animations

        public async UniTask SetTransperent(float fadeValue, float duration, Ease ease)
        {
            await _background.SetTransperent(fadeValue, duration, ease);
        }

        public async UniTask SetColor(Color color, float duration, Ease ease)
        {
            await _background.SetColor(color, duration, ease);
        }

        public async UniTask SetBrightness(float brightnessValue, float duration, Ease ease)
        {
            await _background.SetBrightness(brightnessValue, duration, ease);
        }

        public async UniTask MoveTo(Vector3 position, float duration, Ease ease)
        {
            await _background.MoveTo(position, duration, ease);
        }

        public async UniTask LocalMoveTo(Vector3 localPosition, float duration, Ease ease)
        {
            await _background.LocalMoveTo(localPosition, duration, ease);
        }

        public async UniTask RotateTo(Vector3 rotation, float duration, Ease ease)
        {
            await _background.RotateTo(rotation, duration, ease);
        }

        public async UniTask LocalRotateTo(Vector3 localRotation, float duration, Ease ease)
        {
            await _background.LocalRotateTo(localRotation, duration, ease);
        }

        public async UniTask ScaleTo(Vector3 scale, float duration, Ease ease)
        {
            await _background.ScaleTo(scale, duration, ease);
        }

        public async UniTask PunchPosition(Vector3 punch, float duration, int vibrato = 10, float elasticity = 1)
        {
            await _background.PunchPosition(punch, duration, vibrato, elasticity);
        }

        public async UniTask PunchRotation(Vector3 punch, float duration, int vibrato = 10, float elasticity = 1)
        {
            await _background.PunchRotation(punch, duration, vibrato, elasticity);
        }

        public async UniTask PunchScale(Vector3 punch, float duration, int vibrato = 10, float elasticity = 1)
        {
            await _background.PunchScale(punch, duration, vibrato, elasticity);
        }

        public async UniTask ShakePosition(float duration, float strength = 90, int vibrato = 10, bool fadeOut = true)
        {
            await _background.ShakePosition(duration, strength, vibrato, fadeOut);
        }

        public async UniTask ShakeRotation(float duration, float strength = 90, int vibrato = 10, bool fadeOut = true)
        {
            await _background.ShakeRotation(duration, strength, vibrato, fadeOut);
        }

        public async UniTask ShakeScale(float duration, float strength = 1, int vibrato = 10, float fadeOut = 0)
        {
            await _background.ShakeScale(duration, strength, vibrato, fadeOut);
        }

        public async UniTask MoveOnPath(Vector3[] path, float duration, PathType pathType = PathType.CatmullRom, Ease ease = Ease.Linear)
        {
            await _background.MoveOnPath(path, duration, pathType, ease);
        }

        public async UniTask LookAtTarget(Vector3 worldPosition, float duration, Ease ease)
        {
            await _background.LookAtTarget(worldPosition, duration, ease);
        }

        public void SetLoopingMove(Vector3 endValue, float duration, LoopType loopType = LoopType.Yoyo, Ease ease = Ease.Linear)
        {
            _background.SetLoopingMove(endValue, duration, loopType, ease);
        }

        public void SetLoopingRotate(Vector3 endValue, float duration, LoopType loopType = LoopType.Yoyo, Ease ease = Ease.Linear)
        {
            _background.SetLoopingRotate(endValue, duration, loopType, ease);
        }

        public void SetLoopingScale(Vector3 endValue, float duration, LoopType loopType = LoopType.Yoyo, Ease ease = Ease.Linear)
        {
            _background.SetLoopingScale(endValue, duration, loopType, ease);
        }

        #endregion
    }
}