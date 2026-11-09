using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Плавное появление/исчезновение зоны через параметр материала _FillAmount.
/// Работает с зонами, расставленными в сцене (Play / Fight / Exit / Settings / Menu).
/// Пока стрелка внутри зоны - сектор чуть расширяется и надпись становится жёлтой.
/// </summary>
[DisallowMultipleComponent]
public class ZoneFade : MonoBehaviour
{
    [Tooltip("Время появления/исчезновения зоны, сек.")]
    public float duration = 0.12f;

    [Tooltip("Целевое значение _FillAmount (ширина сектора). 0 — взять из материала.")]
    public float targetFill;

    [Range(0f, 1f)]
    [Tooltip("Множитель ширины сектора (для ползунков: доля громкости 0..1).")]
    public float fillScale = 1f;

    [Header("Highlight")]
    [Tooltip("На сколько процентов расширяется сектор, пока стрелка в зоне.")]
    public float hoverFillPercent = 3.3f;

    [Tooltip("Цвет надписи зоны, пока стрелка в зоне.")]
    public Color hoverLabelColor = new Color(1f, 0.85f, 0.1f);

    [Tooltip("Цвет надписи выбранного пункта (например, текущее разрешение).")]
    public Color selectedLabelColor = new Color(0.4f, 1f, 0.4f);

    [Tooltip("Масштаб надписи зоны, пока стрелка в зоне.")]
    public float hoverLabelScale = 1.1f;

    [Tooltip("Расширять сектор при наведении. Для зон со своим заполнением (ZoneMusicBack / ZoneSoundBack) — выключить.")]
    public bool hoverFillEnabled = true;

    [Tooltip("Альфа зоны, когда она недоступна (например, прокачка на максимуме).")]
    [Range(0f, 1f)]
    public float dimmedAlpha = 0.5f;

    private Image image;
    private Material material;
    private TMP_Text label;
    private Color normalLabelColor = Color.white;
    private bool labelColorCached;
    private float normalImageAlpha = 1f;
    private bool imageAlphaCached;
    private bool dimmed;
    private Vector3 normalLabelScale = Vector3.one;
    private bool labelScaleCached;
    private bool labelScaling;
    private float labelScaleFrom;
    private float labelScaleTo;
    private float labelScaleTime;
    private float labelScaleDuration = 0.12f;
    private bool highlighted;
    private bool selected;
    private float currentFill;
    private float animFrom;
    private float animTo;
    private float animTime;
    private float animDuration = 0.12f;
    private bool animating;
    private bool hideWhenDone;

    private float CurrentTarget => targetFill * fillScale * (highlighted && hoverFillEnabled ? 1f + hoverFillPercent / 100f : 1f);

    // Кешированные id свойств шейдера: HasProperty/SetFloat без строк каждый кадр.
    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");
    private static readonly int SymmetricId = Shader.PropertyToID("_Symmetric");
    private float appliedFill = float.NaN;

    /// <summary>Максимальный fill зоны (сектор целиком).</summary>
    public float MaxFill => targetFill;

    /// <summary>Сектор растёт симметрично от центра (режим _Symmetric материала).</summary>
    public bool SymmetricFill { get; private set; } = true;

    /// <summary>Ширина сектора зоны в градусах (по текущему _FillAmount).</summary>
    public float FillDegrees => targetFill * 360f;

    /// <summary>Надпись зоны (Text (TMP) внутри Tittle).</summary>
    public TMP_Text Label => label;

    /// <summary>Отметить зону как выбранную — надпись красится в selectedLabelColor.</summary>
    public void SetSelected(bool on)
    {
        Init();

        if (selected == on)
            return;

        selected = on;
        ApplyLabelColor();
    }

    private void Awake()
    {
        Init();
    }

    /// <summary>
    /// Кеширует Image, создаёт личный экземпляр материала и запоминает целевую ширину.
    /// </summary>
    public void Init()
    {
        if (image == null)
            image = GetComponent<Image>();

        if (image == null)
            image = GetComponentInChildren<Image>(true);

        if (image == null)
            return;

        if (material == null)
        {
            Material source = image.material;

            if (source != null)
            {
                material = new Material(source);
                image.material = material;
            }
        }

        if (targetFill <= 0.0001f && material != null && material.HasProperty(FillAmountId))
            targetFill = material.GetFloat(FillAmountId);

        // Режим роста сектора берём из материала.
        if (material != null && material.HasProperty(SymmetricId))
            SymmetricFill = material.GetFloat(SymmetricId) > 0.5f;

        if (targetFill <= 0.0001f)
            targetFill = 0.15f;

        if (label == null)
            label = FindLabel();

        if (image != null && !imageAlphaCached)
        {
            normalImageAlpha = image.color.a;
            imageAlphaCached = true;
        }

        if (label != null && !labelColorCached)
        {
            normalLabelColor = label.color;
            labelColorCached = true;
        }

        if (label != null && !labelScaleCached)
        {
            normalLabelScale = label.rectTransform.localScale;
            labelScaleCached = true;
        }

        if (!animating && currentFill <= 0.0001f)
        {
            currentFill = gameObject.activeSelf ? targetFill : 0f;
            Apply();
        }
    }

    private TMP_Text FindLabel()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text text in texts)
        {
            // Cell — отдельный текст с ценой прокачки, его не трогаем.
            if (text != null && text.gameObject.name != "Cell")
                return text;
        }

        return null;
    }

    /// <summary>
    /// Показать зону: материал заполняется от 0 до ширины сектора.
    /// </summary>
    public void Show(bool instant = false)
    {
        Init();

        hideWhenDone = false;
        labelScaling = false;
        SetLabelScale(normalLabelScale.x);

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (instant)
        {
            animating = false;
            SetFill(CurrentTarget);
            return;
        }

        SetFill(0f);
        Play(CurrentTarget);
    }

    /// <summary>
    /// Погасить/восстановить зону по альфе и отключить подсветку.
    /// </summary>
    public void SetDimmed(bool on)
    {
        Init();

        if (dimmed == on)
            return;

        dimmed = on;
        ApplyImageAlpha();

        if (dimmed && highlighted)
            SetHighlight(false);
    }

    private void ApplyImageAlpha()
    {
        if (image == null)
            return;

        Color color = image.color;
        color.a = dimmed ? normalImageAlpha * Mathf.Clamp01(dimmedAlpha) : normalImageAlpha;
        image.color = color;
    }

    /// <summary>
    /// Задаёт долю от максимального заполнения (0..1) без учёта подсветки.
    /// </summary>
    public void SetFillScale(float scale, bool instant = false)
    {
        Init();

        fillScale = Mathf.Clamp01(scale);

        if (instant || !gameObject.activeSelf)
        {
            animating = false;
            hideWhenDone = false;
            SetFill(gameObject.activeSelf ? CurrentTarget : 0f);
            return;
        }

        Play(CurrentTarget);
    }

    /// <summary>
    /// Подсветка, когда стрелка находится в зоне: сектор расширяется, надпись желтеет.
    /// </summary>
    public void SetHighlight(bool on)
    {
        Init();

        // Погашенная зона (прокачка недоступна) не подсвечивается.
        if (on && dimmed)
            return;

        if (highlighted == on)
            return;

        highlighted = on;
        ApplyLabelColor();

        if (!gameObject.activeSelf || hideWhenDone)
            return;

        PlayLabelScale(on ? hoverLabelScale : normalLabelScale.x);
        Play(CurrentTarget);
    }

    /// <summary>
    /// Скрыть зону: материал гасится до 0, затем объект выключается.
    /// </summary>
    public void Hide(bool instant = false)
    {
        Init();

        highlighted = false;
        ApplyLabelColor();
        labelScaling = false;
        SetLabelScale(normalLabelScale.x);

        if (instant || !gameObject.activeSelf)
        {
            animating = false;
            hideWhenDone = false;
            SetFill(0f);

            if (gameObject.activeSelf)
                gameObject.SetActive(false);

            return;
        }

        Play(0f, true);
    }

    private void Play(float to, bool hideAfter = false)
    {
        animFrom = currentFill;
        animTo = to;
        animTime = 0f;
        animDuration = Mathf.Max(0.0001f, duration);
        animating = true;
        hideWhenDone = hideAfter;
    }

    private void Update()
    {
        // UI-анимации зон не зависят от Time.timeScale (иначе в паузе они бы замерзали).
        float dt = Time.unscaledDeltaTime;

        if (animating)
        {
            animTime += dt;

            float t = Mathf.Clamp01(animTime / animDuration);
            SetFill(Mathf.Lerp(animFrom, animTo, t));

            if (t >= 1f)
            {
                animating = false;

                if (hideWhenDone)
                {
                    hideWhenDone = false;
                    gameObject.SetActive(false);
                }
            }
        }

        if (labelScaling)
        {
            labelScaleTime += dt;

            float t = Mathf.Clamp01(labelScaleTime / labelScaleDuration);
            SetLabelScale(Mathf.Lerp(labelScaleFrom, labelScaleTo, t));

            if (t >= 1f)
                labelScaling = false;
        }
    }

    private void PlayLabelScale(float to)
    {
        if (label == null)
            return;

        labelScaleFrom = label.rectTransform.localScale.x;
        labelScaleTo = to;
        labelScaleTime = 0f;
        labelScaleDuration = Mathf.Max(0.0001f, duration);
        labelScaling = true;
    }

    private void SetLabelScale(float scale)
    {
        if (label == null)
            return;

        label.rectTransform.localScale = new Vector3(scale, scale, 1f);
    }

    private void SetFill(float value)
    {
        currentFill = value;
        Apply();
    }

    private void ApplyLabelColor()
    {
        if (label == null)
            return;

        if (highlighted)
            label.color = hoverLabelColor;
        else if (selected)
            label.color = selectedLabelColor;
        else
            label.color = normalLabelColor;
    }

    private void Apply()
    {
        if (material == null || !material.HasProperty(FillAmountId))
            return;

        // Пропускаем запись в материал, если значение не изменилось.
        if (Mathf.Approximately(appliedFill, currentFill))
            return;

        appliedFill = currentFill;
        material.SetFloat(FillAmountId, currentFill);
    }

    private void OnDestroy()
    {
        if (material != null)
            Destroy(material);
    }
}
