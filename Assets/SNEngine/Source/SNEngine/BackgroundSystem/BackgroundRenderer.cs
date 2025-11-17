using Cysharp.Threading.Tasks;
using DG.Tweening;
using SiphoinUnityHelpers.XNodeExtensions.Attributes;
using UnityEngine;

namespace SNEngine.BackgroundSystem
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class BackgroundRenderer : MonoBehaviour, IBackgroundRenderer
    {
        public bool UseTransition { get; set; }

        [SerializeField] private SpriteRenderer _maskTransition;

        private Sprite _oldSetedBackground;

        [SerializeField, ReadOnly(ReadOnlyMode.Always)] private SpriteRenderer _spriteRenderer;
        private Tween _currentTween;
        protected SpriteRenderer SpriteRenderer => _spriteRenderer;

        public void SetData(Sprite data)
        {
            if (_maskTransition != null)
            {
                _oldSetedBackground = _spriteRenderer.sprite;

                _maskTransition.sprite = _oldSetedBackground;
            }

            UpdateBackground(data).Forget();
        }

        private async UniTask UpdateBackground(Sprite data)
        {
            await UniTask.WaitForEndOfFrame(this);

            _spriteRenderer.sprite = data;
        }

        public void Clear()
        {
            _spriteRenderer.sprite = null;
        }

        private void OnValidate()
        {
            if (!_spriteRenderer)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        public void ResetState()
        {
            Clear();
            _spriteRenderer.color = Color.white;
            transform.position = Vector3.zero;
            transform.localEulerAngles = Vector3.zero;
            transform.localScale = Vector3.one;
            _currentTween?.Kill();
        }

        #region Animations

        public async UniTask SetTransperent(float fadeValue, float duration, Ease ease)
        {
            _currentTween = _spriteRenderer.DOFade(fadeValue, duration).SetEase(ease);
            await _currentTween;
        }

        public async UniTask SetColor(Color color, float duration, Ease ease)
        {
            _currentTween = _spriteRenderer.DOColor(color, duration).SetEase(ease);
            await _currentTween;
        }

        public async UniTask SetBrightness(float brightnessValue, float duration, Ease ease)
        {
            Color targetColor = new Color(brightnessValue, brightnessValue, brightnessValue, _spriteRenderer.color.a);
            _currentTween = _spriteRenderer.DOColor(targetColor, duration).SetEase(ease);
            await _currentTween;
        }

        public async UniTask MoveTo(Vector3 position, float duration, Ease ease)
        {
            _currentTween = transform.DOMove(position, duration).SetEase(ease);
            await _currentTween;
        }

        public async UniTask LocalMoveTo(Vector3 localPosition, float duration, Ease ease)
        {
            _currentTween = transform.DOLocalMove(localPosition, duration).SetEase(ease);
            await _currentTween;
        }

        public async UniTask RotateTo(Vector3 rotation, float duration, Ease ease)
        {
            _currentTween = transform.DORotate(rotation, duration).SetEase(ease);
            await _currentTween;
        }

        public async UniTask LocalRotateTo(Vector3 localRotation, float duration, Ease ease)
        {
            _currentTween = transform.DOLocalRotate(localRotation, duration).SetEase(ease);
            await _currentTween;
        }

        public async UniTask ScaleTo(Vector3 scale, float duration, Ease ease)
        {
            _currentTween = transform.DOScale(scale, duration).SetEase(ease);
            await _currentTween;
        }

        public async UniTask PunchPosition(Vector3 punch, float duration, int vibrato = 10, float elasticity = 1)
        {
            _currentTween = transform.DOPunchPosition(punch, duration, vibrato, elasticity);
            await _currentTween;
        }

        public async UniTask PunchRotation(Vector3 punch, float duration, int vibrato = 10, float elasticity = 1)
        {
            _currentTween = transform.DOPunchRotation(punch, duration, vibrato, elasticity);
            await _currentTween;
        }

        public async UniTask PunchScale(Vector3 punch, float duration, int vibrato = 10, float elasticity = 1)
        {
            _currentTween = transform.DOPunchScale(punch, duration, vibrato, elasticity);
            await _currentTween;
        }

        public async UniTask ShakePosition(float duration, float strength = 90, int vibrato = 10, bool fadeOut = true)
        {
            _currentTween = transform.DOShakePosition(duration, strength, vibrato, 90, fadeOut);
            await _currentTween;
        }

        public async UniTask ShakeRotation(float duration, float strength = 90, int vibrato = 10, bool fadeOut = true)
        {
            _currentTween = transform.DOShakeRotation(duration, strength, vibrato, 90, fadeOut);
            await _currentTween;
        }

        public async UniTask ShakeScale(float duration, float strength = 1, int vibrato = 10, float fadeOut = 0)
        {
            _currentTween = transform.DOShakeScale(duration, strength, vibrato, fadeOut);
            await _currentTween;
        }

        public async UniTask MoveOnPath(Vector3[] path, float duration, PathType pathType = PathType.CatmullRom, Ease ease = Ease.Linear)
        {
            _currentTween = transform.DOPath(path, duration, pathType).SetEase(ease);
            await _currentTween;
        }

        public async UniTask LookAtTarget(Vector3 worldPosition, float duration, Ease ease)
        {
            _currentTween = transform.DOLookAt(worldPosition, duration).SetEase(ease);
            await _currentTween;
        }

        public void SetLoopingMove(Vector3 endValue, float duration, LoopType loopType = LoopType.Yoyo, Ease ease = Ease.Linear)
        {
            _currentTween?.Kill();
            _currentTween = transform.DOLocalMove(endValue, duration)
                .SetEase(ease)
                .SetLoops(-1, loopType);
        }

        public void SetLoopingRotate(Vector3 endValue, float duration, LoopType loopType = LoopType.Yoyo, Ease ease = Ease.Linear)
        {
            _currentTween?.Kill();
            _currentTween = transform.DOLocalRotate(endValue, duration)
                .SetEase(ease)
                .SetLoops(-1, loopType);
        }

        public void SetLoopingScale(Vector3 endValue, float duration, LoopType loopType = LoopType.Yoyo, Ease ease = Ease.Linear)
        {
            _currentTween?.Kill();
            _currentTween = transform.DOScale(endValue, duration)
                .SetEase(ease)
                .SetLoops(-1, loopType);
        }

        #endregion
    }
}