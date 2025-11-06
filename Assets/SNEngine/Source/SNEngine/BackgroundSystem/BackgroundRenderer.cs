using Cysharp.Threading.Tasks;
using DG.Tweening;
using SNEngine.Animations;
using SNEngine.Debugging;
using SNEngine.Extensions;
using System;
using UnityEngine;

namespace SNEngine.BackgroundSystem
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class BackgroundRenderer : MonoBehaviour, IBackgroundRenderer
    {
        public bool UseTransition { get; set; }

        [SerializeField] private SpriteRenderer _maskTransition;

        private Sprite _oldSetedBackground;

        private SpriteRenderer _spriteRenderer;

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

        private void Awake()
        {
            if (!TryGetComponent(out _spriteRenderer))
            {
                throw new NullReferenceException("sprite renderer component not found on background renderer");
            }
        }

        public void ResetState()
        {
            Clear();

            _spriteRenderer.color = Color.white;

            transform.position = Vector3.zero;
        }

        private void SetVisibleMaskTransition(bool visible)
        {
            if (!_maskTransition)
            {
                NovelGameDebug.LogError($"The Background Renderer {name} not have mask transition");

                return;
            }

            _maskTransition.gameObject.SetActive(visible);
        }
    }

}