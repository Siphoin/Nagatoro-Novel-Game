using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace CoreGame.UI
{
    public class MenuAutoHideUI : MonoBehaviour
    {
        [SerializeField] private List<RectTransform> _uiElements;
        [SerializeField] private float _inactivityTimeout = 5f;
        [SerializeField] private float _offScreenOffset = 5000f;
        [SerializeField] private float _transitionDuration = 0.5f;

        [Header("Ease Settings")]
        [SerializeField] private Ease _hideEase = Ease.InExpo;
        [SerializeField] private Ease _showEase = Ease.OutExpo;

        private List<Vector2> _originalAnchoredPositions;
        private Vector3 _lastMousePosition;
        private bool _isUIHidden = false;

        private CancellationTokenSource _inactivityCts;

        private void Start()
        {
            _originalAnchoredPositions = new List<Vector2>();
            foreach (var rect in _uiElements)
            {
                _originalAnchoredPositions.Add(rect.anchoredPosition);
            }
            _lastMousePosition = Input.mousePosition;

            DetectMouseMovementAsync();
            ResetInactivityTimer();
        }

        private async void DetectMouseMovementAsync()
        {
            while (Application.isPlaying && enabled)
            {
                Vector3 currentMousePosition = Input.mousePosition;
                bool mouseMoved = Vector3.Distance(currentMousePosition, _lastMousePosition) > 5f;

                if (mouseMoved)
                {
                    if (_isUIHidden)
                    {
                        ShowUI();
                    }
                    else
                    {
                        ResetInactivityTimer();
                    }
                }

                _lastMousePosition = currentMousePosition;

                await UniTask.Delay(100);
            }
        }

        private void ResetInactivityTimer()
        {
            _inactivityCts?.Cancel();
            _inactivityCts?.Dispose();

            _inactivityCts = new CancellationTokenSource();
            StartInactivityTimer();
        }

        private async void StartInactivityTimer()
        {
            if (_isUIHidden) return;

            CancellationToken token = _inactivityCts.Token;

            try
            {
                await UniTask.Delay((int)(_inactivityTimeout * 1000), cancellationToken: token);

                if (!token.IsCancellationRequested && !_isUIHidden)
                {
                    HideUI();
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void HideUI()
        {
            _isUIHidden = true;
            for (int i = 0; i < _uiElements.Count; i++)
            {
                RectTransform rect = _uiElements[i];
                Vector2 originalAnchorPos = _originalAnchoredPositions[i];

                if (rect.TryGetComponent<MenuEffectText>(out var effectText))
                {
                    effectText.enabled = false;
                }

                float dirX = 0;
                float dirY = 0;

                if (Mathf.Abs(originalAnchorPos.x) < 0.01f)
                {
                    dirX = -1f;
                }
                else
                {
                    dirX = Mathf.Sign(originalAnchorPos.x);
                }

                if (Mathf.Abs(originalAnchorPos.y) < 0.01f)
                {
                    dirY = -1f;
                }
                else
                {
                    dirY = Mathf.Sign(originalAnchorPos.y);
                }

                Vector2 targetPosition = new Vector2(
                    originalAnchorPos.x + dirX * _offScreenOffset,
                    originalAnchorPos.y + dirY * _offScreenOffset
                );

                rect.DOKill(true);
                rect.DOAnchorPos(targetPosition, _transitionDuration).SetEase(_hideEase).SetLink(rect.gameObject);
            }
        }

        private void ShowUI()
        {
            _isUIHidden = false;
            for (int i = 0; i < _uiElements.Count; i++)
            {
                RectTransform rect = _uiElements[i];
                Vector2 originalPosition = _originalAnchoredPositions[i];

                rect.DOKill(true);

                rect.DOAnchorPos(originalPosition, _transitionDuration).SetEase(_showEase)
                    .OnComplete(() =>
                    {
                        rect.anchoredPosition = originalPosition;

                        if (rect.TryGetComponent<MenuEffectText>(out var effectText))
                        {
                            effectText.enabled = true;
                            effectText.transform.localPosition = rect.localPosition;
                        }
                    })
                    .SetLink(rect.gameObject);
            }
            ResetInactivityTimer();
        }

        private void OnDestroy()
        {
            _inactivityCts?.Cancel();
            _inactivityCts?.Dispose();

            foreach (var rect in _uiElements)
            {
                if (rect != null)
                {
                    rect.DOKill(true);
                    if (rect.TryGetComponent<MenuEffectText>(out var effectText))
                    {
                        effectText.enabled = true;
                    }
                }
            }
        }
    }
}