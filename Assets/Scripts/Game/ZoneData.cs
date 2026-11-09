using UnityEngine;

[System.Serializable]
public class ZoneData
{
    public float startAngle;      // текущий начальный угол (градусы)
    public float width;           // текущая ширина (градусы)
    public GameObject gameObject;
    public Material material;

    [Tooltip("Что даёт зона при попадании.")]
    public CombatZoneType type = CombatZoneType.Attack;

    public float shrinkSpeed;     // скорость уменьшения (градусов/сек), 0 = не уменьшается
    public float rotationSpeed;   // скорость вращения (градусов/сек), 0 = не вращается

    // Центр сектора: зона симметрична (_Symmetric материала), края сходятся/расходятся к нему.
    [System.NonSerialized] public float centerAngle;

    [System.NonSerialized] public bool closing;       // зона исчезает (клики игнорируются)
    [System.NonSerialized] public float fillAmount;   // текущее значение _FillAmount

    [System.NonSerialized] private float animFrom;
    [System.NonSerialized] private float animTo;
    [System.NonSerialized] private float animTime;
    [System.NonSerialized] private float animDuration;
    [System.NonSerialized] private bool animating;
    [System.NonSerialized] private float appliedFill = float.NaN;

    // Кешированные id свойств шейдера (без строк каждый кадр).
    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");

    public ZoneData(float start, float w, GameObject obj, Material mat, float shrink = 0f, float rot = 0f, CombatZoneType zoneType = CombatZoneType.Attack)
    {
        startAngle = start;
        width = w;
        gameObject = obj;
        material = mat;
        shrinkSpeed = shrink;
        rotationSpeed = rot;
        type = zoneType;
        centerAngle = start + w * 0.5f;
        fillAmount = w / 360f;
    }

    /// <summary>Сколько осталось до конца анимации исчезновения зоны (сек).</summary>
    public float FadingTimeLeft => animating ? Mathf.Max(0f, animDuration - animTime) : 0f;

    /// <summary>
    /// Плавная анимация _FillAmount от from до to за duration секунд.
    /// </summary>
    public void PlayFill(float from, float to, float duration)
    {
        animFrom = from;
        animTo = to;
        animTime = 0f;
        animDuration = Mathf.Max(0.0001f, duration);
        animating = true;
        fillAmount = from;
        ApplyFill();
    }

    // Обновление состояния зоны. Возвращает true, если зону можно удалять.
    public bool UpdateZone(float deltaTime)
    {
        if (gameObject == null)
            return true;

        // Вращение: крутится центр сектора, начало пересчитывается из него.
        if (rotationSpeed != 0f)
            centerAngle = Normalize360(centerAngle + rotationSpeed * deltaTime);

        // Уменьшение ширины
        if (shrinkSpeed != 0f && !closing)
        {
            width -= shrinkSpeed * deltaTime;
            if (width <= 0f)
            {
                width = 0f;
                closing = true;
                PlayFill(fillAmount, 0f, 0.12f);
            }
        }

        // Симметричная зона: центр неподвижен, края сходятся к нему.
        // startAngle — левая граница, по ней считаются попадания.
        startAngle = Normalize360(centerAngle - width * 0.5f);
        gameObject.transform.rotation = Quaternion.Euler(0f, 0f, centerAngle);

        // Анимация видимости зоны
        if (animating)
        {
            animTime += deltaTime;
            float t = Mathf.Clamp01(animTime / animDuration);
            fillAmount = Mathf.Lerp(animFrom, animTo, t);

            if (t >= 1f)
                animating = false;
        }
        else if (!closing)
        {
            fillAmount = width / 360f;
        }

        ApplyFill();

        // Исчезающая зона удаляется, когда fillAmount дошёл до нуля.
        if (closing && !animating && fillAmount <= 0.0001f)
            return true;

        return false;
    }

    private static float Normalize360(float angle)
    {
        angle %= 360f;

        if (angle < 0f)
            angle += 360f;

        return angle;
    }

    private void ApplyFill()
    {
        if (material == null)
            return;

        // Не дёргаем материал, если значение не изменилось.
        if (Mathf.Approximately(appliedFill, fillAmount))
            return;

        appliedFill = fillAmount;
        material.SetFloat(FillAmountId, fillAmount);
    }
}