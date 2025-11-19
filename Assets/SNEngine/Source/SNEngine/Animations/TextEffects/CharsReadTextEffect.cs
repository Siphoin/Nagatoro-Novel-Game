using Cysharp.Threading.Tasks;
using DG.Tweening;
using SNEngine.Animations.TextEffects;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class CharsFadeIn : TextEffect
{
    [SerializeField] private float _fadeDuration = 0.25f;
    [SerializeField] private float _delayBetweenChars = 0.05f;

    private Color32[][] _colors;
    private bool _loopRunning;
    private bool _forceCompleted;

    protected override void TextForceCompleted(TextMeshProUGUI textMesh)
    {
        _forceCompleted = true;
    }

    private void OnEnable()
    {
        Component.color = Color.clear;
        UniTask.DelayFrame(1).ContinueWith(() => StartFadeIn()).Forget();
        Component.color = Color.white;
    }

    private void StartFadeIn()
    {
        _forceCompleted = false;
        Component.ForceMeshUpdate();
        CacheColors();
        SetAllAlpha(0);
        ApplyColors();

        if (!_loopRunning)
        {
            _loopRunning = true;
            FadeInText().Forget();
        }
    }

    private void CacheColors()
    {
        var mesh = Component.textInfo.meshInfo;
        _colors = new Color32[mesh.Length][];
        for (int i = 0; i < mesh.Length; i++)
            _colors[i] = (Color32[])mesh[i].colors32.Clone();
    }

    private void ApplyColors()
    {
        var mesh = Component.textInfo.meshInfo;
        for (int i = 0; i < mesh.Length; i++)
            mesh[i].colors32 = _colors[i];

        Component.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private void SetAlpha(int charIndex, byte alpha)
    {
        var info = Component.textInfo;
        if (charIndex < 0 || charIndex >= info.characterCount) return;

        var ci = info.characterInfo[charIndex];
        if (!ci.isVisible) return;

        int mi = ci.materialReferenceIndex;
        int vi = ci.vertexIndex;

        for (int j = 0; j < 4; j++)
        {
            var c = _colors[mi][vi + j];
            _colors[mi][vi + j] = new Color32(c.r, c.g, c.b, alpha);
        }
    }

    private void SetAllAlpha(byte alpha)
    {
        int count = Component.textInfo.characterCount;
        for (int i = 0; i < count; i++)
            SetAlpha(i, alpha);
    }

    private async UniTask FadeInText()
    {
        var info = Component.textInfo;
        int total = info.characterCount;
        float startTime = Time.time;

        while (!_forceCompleted)
        {
            bool anyFading = false;

            for (int i = 0; i < total; i++)
            {
                if (!info.characterInfo[i].isVisible) continue;

                float charStart = startTime + i * _delayBetweenChars;
                float t = Mathf.Clamp01((Time.time - charStart) / _fadeDuration);

                if (t < 1f) anyFading = true;

                SetAlpha(i, (byte)(t * 255));
            }

            ApplyColors();

            if (!anyFading) break;

            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        SetAllAlpha(255);
        ApplyColors();
        _loopRunning = false;
    }
}
