using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace CoreGame.FightSystem.UI
{
    public class HintUI : MonoBehaviour
    {
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private TextMeshProUGUI _hintText;
        [SerializeField] private float _fadeDuration = 0.5f;
        [SerializeField] private float _displayDuration = 2f;
        [SerializeField] private float _scaleXDuration = 0.3f;
        [SerializeField] private Ease _scaleXEase = Ease.OutBack;

        private Tween _currentTween;
        private float _initialScaleX;

        private void Awake()
        {
            if (_backgroundImage != null)
            {
                var c = _backgroundImage.color;
                c.a = 0f;
                _backgroundImage.color = c;
            }
            if (_hintText != null)
            {
                var c = _hintText.color;
                c.a = 0f;
                _hintText.color = c;
            }

            _initialScaleX = transform.localScale.x;
            transform.localScale = new Vector3(0f, transform.localScale.y, transform.localScale.z);
            gameObject.SetActive(false);
        }

        public void ShowHint(string message)
        {
            if (_hintText != null)
            {
                _hintText.text = message;
                var c = _hintText.color;
                c.a = 0f;
                _hintText.color = c;
            }

            _currentTween?.Kill(true);

            gameObject.SetActive(true);
            transform.localScale = new Vector3(0f, transform.localScale.y, transform.localScale.z);

            if (_backgroundImage != null)
            {
                var c = _backgroundImage.color;
                c.a = 0f;
                _backgroundImage.color = c;
            }

            // Анимация: 1. Расширение по X. 2. Фейд фона. 3. Фейд текста.
            Sequence showSequence = DOTween.Sequence().SetLink(gameObject);

            // 1. Сжатие/Расширение по X
            showSequence.Append(transform.DOScaleX(_initialScaleX, _scaleXDuration).SetEase(_scaleXEase));

            // 2. Фейд фона (начинается с расширением)
            showSequence.Join(_backgroundImage.DOFade(1f, _fadeDuration).SetEase(Ease.Linear));

            // 3. Фейд текста (начинается после расширения)
            showSequence.Append(_hintText.DOFade(1f, _fadeDuration).SetEase(Ease.Linear));


            // Анимация скрытия: 1. Задержка. 2. Фейд текста. 3. Фейд фона. 4. Сжатие по X.
            Sequence hideSequence = DOTween.Sequence().SetLink(gameObject);

            // 1. Задержка
            hideSequence.AppendInterval(_displayDuration);

            // 2. Фейд текста
            hideSequence.Append(_hintText.DOFade(0f, _fadeDuration).SetEase(Ease.Linear));

            // 3. Фейд фона (начинается вместе с фейдом текста)
            hideSequence.Join(_backgroundImage.DOFade(0f, _fadeDuration).SetEase(Ease.Linear));

            // 4. Сжатие по X (начинается вместе с фейдом текста)
            hideSequence.Join(transform.DOScaleX(0f, _scaleXDuration).SetEase(_scaleXEase));

            hideSequence.OnComplete(() =>
            {
                gameObject.SetActive(false);
            });

            _currentTween = DOTween.Sequence().Append(showSequence).Append(hideSequence);
        }
    }
}