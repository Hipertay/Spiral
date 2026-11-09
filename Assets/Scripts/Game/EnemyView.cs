using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class EnemyView : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer spriteRenderer;

    [Header("Hit Effect")]
    public Material currentMaterial;

    [Header("Settings")]
    [Tooltip("Скорость проигрывания кадров анимаций (кадров в секунду).")]
    public float framesPerSecond = 12f;

    [Tooltip("Скорость Idle относительно framesPerSecond (0.5 = вдвое медленнее).")]
    public float idleSpeedMultiplier = 0.5f;

    [Header("Fade")]
    [Tooltip("Длительность появления и исчезновения через _FadeAmount материала.")]
    public float fadeDuration = 0.25f;

    private static readonly int FadeAmountId = Shader.PropertyToID("_FadeAmount");

    private readonly WaitForSeconds HitBlendWait = new WaitForSeconds(0.05f);

    private Coroutine colorHitRoutine;

    // Проигрывание кадров: Idle по кругу, Attack/Defeat — один раз.
    private CharacterData character;
    private Sprite[] frames;
    private int frameIndex;
    private float frameTimer;
    private bool loopFrames;
    private float currentFps = 12f;
    private bool midpointFired;
    private Action midpointCallback;
    private Action completeCallback;

    public int MaxHp { get; private set; }
    public int Hp { get; private set; }

    /// <summary>Масштаб, при котором враг занимает полный размер в центре.</summary>
    public float BaseScale { get; private set; } = 1f;

    public bool HasAttackAnimation => character != null && character.HasAttack;
    public bool HasDefeatAnimation => character != null && character.HasDefeat;

    /// <summary>Длительность исчезновения — по ней боёвка ждёт перед спавном/перемещением.</summary>
    public float FadeDuration => Mathf.Max(0.01f, fadeDuration);

    public void Setup(CharacterData data, int hp, float baseScale)
    {
        character = data;
        BaseScale = Mathf.Max(0.0001f, baseScale);

        if (spriteRenderer != null && data != null && data.HasIdle)
            spriteRenderer.sprite = data.idleFrames[0];

        if (spriteRenderer != null)
        {
            // Копируем материал, чтобы у каждого врага был свой _HitEffectBlend.
            Material source = spriteRenderer.sharedMaterial;
            if (source != null && source.HasProperty("_HitEffectBlend"))
            {
                currentMaterial = new Material(source);
                spriteRenderer.material = currentMaterial;
                currentMaterial.SetColor("_HitEffectColor", Color.white);
                // Glow умножается на цвет хита: 0 делал вспышку чёрной.
                currentMaterial.SetFloat("_HitEffectGlow", 1f);
                currentMaterial.SetFloat("_HitEffectBlend", 0f);
            }
        }

        MaxHp = Mathf.Max(1, hp);
        Hp = MaxHp;

        // Появление: новый враг выставляется на _FadeAmount = 1 и проявляется до 0.
        if (currentMaterial != null && currentMaterial.HasProperty(FadeAmountId))
        {
            currentMaterial.DOKill();
            SetFade(1f);

            currentMaterial.DOFloat(0f, "_FadeAmount", Mathf.Max(0.01f, fadeDuration))
                .SetEase(Ease.Linear)
                .SetUpdate(true);
        }

        PlayIdle();
    }

    /// <summary>Мгновенно выставляет степень «растворения» материала (0 — виден, 1 — исчез).</summary>
    public void SetFade(float value)
    {
        if (currentMaterial == null || !currentMaterial.HasProperty(FadeAmountId))
            return;

        currentMaterial.SetFloat(FadeAmountId, Mathf.Clamp01(value));
    }

    /// <summary>Плавно убирает врага: _FadeAmount 0 → 1, затем объект удаляется.</summary>
    public void FadeOutAndDestroy()
    {
        if (currentMaterial == null || !currentMaterial.HasProperty(FadeAmountId))
        {
            Destroy(gameObject);
            return;
        }

        currentMaterial.DOKill();
        currentMaterial.DOFloat(1f, "_FadeAmount", Mathf.Max(0.01f, fadeDuration))
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (this != null)
                    Destroy(gameObject);
            });

        // Страховка: даже если твин будет убит, объект всё равно уберётся с карты.
        Destroy(gameObject, Mathf.Max(0.01f, fadeDuration) + 0.1f);
    }

    // ---------------------------------------------------------------- animation

    private void Update()
    {
        AdvanceFrames(Time.deltaTime);
    }

    /// <summary>Idle — крутится бесконечно, медленнее остальных анимаций.</summary>
    public void PlayIdle()
    {
        if (character == null)
            return;

        PlayFrames(character.idleFrames, true, null, null);
        currentFps = Mathf.Max(0.01f, framesPerSecond * Mathf.Max(0.01f, idleSpeedMultiplier));
    }

    /// <summary>
    /// Атака (когда у игрока кончился таймер). onMidpoint — середина анимации.
    /// </summary>
    public void PlayAttack(Action onMidpoint, Action onComplete)
    {
        if (!HasAttackAnimation)
        {
            onMidpoint?.Invoke();
            onComplete?.Invoke();
            return;
        }

        PlayFrames(character.attackFrames, false, onComplete, onMidpoint);
    }

    /// <summary>Смерть врага: один проход кадров, затем onComplete.</summary>
    public void PlayDefeat(Action onComplete)
    {
        if (!HasDefeatAnimation)
        {
            onComplete?.Invoke();
            return;
        }

        PlayFrames(character.defeatFrames, false, onComplete, null);
    }

    private void PlayFrames(Sprite[] clip, bool loop, Action onComplete, Action onMidpoint)
    {
        frames = clip != null && clip.Length > 0 ? clip : null;
        loopFrames = loop;
        frameIndex = 0;
        frameTimer = 0f;
        currentFps = Mathf.Max(0.01f, framesPerSecond);
        midpointFired = false;
        midpointCallback = onMidpoint;
        completeCallback = onComplete;

        if (frames != null && spriteRenderer != null)
            spriteRenderer.sprite = frames[0];
    }

    private void AdvanceFrames(float deltaTime)
    {
        if (frames == null || frames.Length == 0 || spriteRenderer == null)
            return;

        frameTimer -= deltaTime;

        if (frameTimer > 0f)
            return;

        frameTimer += 1f / Mathf.Max(0.01f, currentFps);
        frameIndex++;

        if (frameIndex >= frames.Length)
        {
            if (loopFrames)
            {
                frameIndex = 0;
            }
            else
            {
                spriteRenderer.sprite = frames[frames.Length - 1];

                Action done = completeCallback;
                completeCallback = null;
                midpointCallback = null;
                frames = null;

                done?.Invoke();
                return;
            }
        }

        spriteRenderer.sprite = frames[frameIndex];

        // Середина клипа — для вспышки урона по таймеру.
        if (!midpointFired && midpointCallback != null && frameIndex >= frames.Length / 2)
        {
            midpointFired = true;

            Action midpoint = midpointCallback;
            midpointCallback = null;
            midpoint?.Invoke();
        }
    }

    public void TakeDamage(int damage)
    {
        Hp = Mathf.Max(0, Hp - damage);
        Flash();
    }

    /// <summary>
    /// Белая вспышка через параметр материала _HitEffectBlend.
    /// </summary>
    public void Flash()
    {
        if (currentMaterial == null)
            return;

        if (colorHitRoutine != null)
            StopCoroutine(colorHitRoutine);

        colorHitRoutine = StartCoroutine(ColorHit());
    }

    private IEnumerator ColorHit()
    {
        if (currentMaterial == null)
            yield break;

        currentMaterial.DOFloat(0f, "_HitEffectBlend", 0f).SetEase(Ease.Linear);
        yield return null;
        currentMaterial.DOFloat(0.5f, "_HitEffectBlend", 0.05f).SetEase(Ease.Linear);
        yield return HitBlendWait;
        currentMaterial.DOFloat(0f, "_HitEffectBlend", 0.05f).SetEase(Ease.Linear);
        yield return HitBlendWait;
        currentMaterial.DOFloat(0.5f, "_HitEffectBlend", 0.05f).SetEase(Ease.Linear);
        yield return HitBlendWait;
        currentMaterial.DOFloat(0f, "_HitEffectBlend", 0.05f).SetEase(Ease.Linear);
    }

    public void MoveTo(Vector3 localPosition, float duration, Vector3 scale)
    {
        transform.DOKill();

        if (duration <= 0f)
        {
            transform.localPosition = localPosition;
            transform.localScale = scale;
            return;
        }

        transform.DOLocalMove(localPosition, duration).SetEase(Ease.OutQuad);
        transform.DOScale(scale, duration).SetEase(Ease.OutQuad);
    }

    private void StopHitRoutine()
    {
        if (colorHitRoutine != null)
        {
            StopCoroutine(colorHitRoutine);
            colorHitRoutine = null;
        }

        if (currentMaterial != null)
        {
            currentMaterial.DOKill();
            currentMaterial.SetFloat("_HitEffectBlend", 0f);
        }
    }

    private void OnDisable()
    {
        StopHitRoutine();
    }

    private void OnDestroy()
    {
        // Снимаем твины трансформа, иначе DOTween держит ссылки на уничтоженный объект.
        transform.DOKill();

        if (currentMaterial != null)
        {
            currentMaterial.DOKill();
            Destroy(currentMaterial);
            currentMaterial = null;
        }
    }
}
