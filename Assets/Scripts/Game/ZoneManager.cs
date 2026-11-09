using System.Collections;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Что даёт боевая зона при попадании.
/// </summary>
public enum CombatZoneType
{
    Attack,
    Heal,
    Gold,
    Energy,
    Timer,
    Trap
}

/// <summary>
/// Параметры зон для конкретного раунда.
/// </summary>
[System.Serializable]
public class ZoneRoundSettings
{
    public int initialZoneCount = 2;
    public int maxZoneCount = 5;
    public float spawnInterval = 2f;
    public float minZoneWidth = 30f;
    public float maxZoneWidth = 60f;
    public float minRotationSpeed = 0f;
    public float maxRotationSpeed = 0f;
    public float minShrinkSpeed = 0f;
    public float maxShrinkSpeed = 0f;

    [Tooltip("true — каждая зона крутится в случайную сторону, false — все в одну.")]
    public bool randomRotationDirection = true;

    [Tooltip("Номер раунда (1..30) — от него зависит, какие типы зон доступны.")]
    public int roundLevel = 1;
}

public class ZoneManager : MonoBehaviour
{
    [Header("References")]
    public GameObject zonePrefab;
    public Transform zonesParent;

    [Header("Fill Animation")]
    [Tooltip("За сколько секунд зона плавно появляется (fillAmount 0 -> ширина).")]
    public float appearDuration = 0.12f;

    [Tooltip("За сколько секунд зона плавно исчезает (fillAmount -> 0).")]
    public float disappearDuration = 0.12f;

    [Header("Цвета зон")]
    public Color attackColor = Color.white;
    public Color healColor = new Color(0.3f, 1f, 0.3f);
    public Color goldColor = new Color(1f, 0.85f, 0.1f);
    public Color energyColor = new Color(0.3f, 0.6f, 1f);
    public Color timerColor = new Color(0.7f, 0.35f, 1f);
    public Color trapColor = new Color(1f, 0.2f, 0.2f);

    [Header("Зоны: 0 — тип отключён")]
    public int healWeight = 1;
    public int goldWeight = 1;
    public int energyWeight = 1;
    public int timerWeight = 1;
    public int trapWeight = 1;

    [Header("Уровни разблокировки зон")]
    [Tooltip("С какого уровня раунда появляются зоны золота.")]
    public int goldUnlockLevel = 6;

    [Tooltip("С какого уровня раунда появляются зоны таймера.")]
    public int timerUnlockLevel = 11;

    [Tooltip("С какого уровня раунда появляются зоны энергии.")]
    public int energyUnlockLevel = 16;

    [Tooltip("С какого уровня раунда появляются зоны лечения.")]
    public int healUnlockLevel = 21;

    [Tooltip("С какого уровня раунда появляются зоны-ловушки.")]
    public int trapUnlockLevel = 26;

    private readonly List<ZoneData> zones = new List<ZoneData>();
    private readonly List<ZoneData> fadingZones = new List<ZoneData>();
    private ZoneRoundSettings settings = new ZoneRoundSettings();
    private Coroutine spawnRoutine;

    // Кеш ожидания между спавнами (без new WaitForSeconds каждый раз).
    private WaitForSeconds spawnWait;
    private float spawnWaitInterval = -1f;

    // Пробная зона для проверки перекрытий — переиспользуется, чтобы не мусорить в GC.
    private readonly ZoneData probeZone = new ZoneData(0f, 0f, null, null);

    // Сколько не-атакующих зон появилось подряд (максимум 3).
    private int nonAttackStreak;

    public List<ZoneData> Zones => zones;

    /// <summary>Сколько ещё будет исчезать зона, исчезающая прямо сейчас (0 — таких нет).</summary>
    public float FadingZoneTimeLeft
    {
        get
        {
            float left = 0f;

            for (int i = 0; i < fadingZones.Count; i++)
            {
                ZoneData zone = fadingZones[i];

                if (zone != null && zone.gameObject != null)
                    left = Mathf.Max(left, zone.FadingTimeLeft);
            }

            return left;
        }
    }
    public bool IsRunning { get; private set; }

    /// <summary>Пауза: зоны не вращаются/не сжимаются, спавн не идёт (без Time.timeScale = 0).</summary>
    public bool Paused { get; set; }

    private void Awake()
    {
        EnsureDefaults();
    }

    /// <summary>
    /// Если в сцене поля ещё не были сохранены — подставляем значения по умолчанию.
    /// </summary>
    private void EnsureDefaults()
    {
        if (attackColor.a <= 0f) attackColor = Color.white;
        if (healColor.a <= 0f) healColor = new Color(0.3f, 1f, 0.3f);
        if (goldColor.a <= 0f) goldColor = new Color(1f, 0.85f, 0.1f);
        if (energyColor.a <= 0f) energyColor = new Color(0.3f, 0.6f, 1f);
        if (timerColor.a <= 0f) timerColor = new Color(0.7f, 0.35f, 1f);
        if (trapColor.a <= 0f) trapColor = new Color(1f, 0.2f, 0.2f);

        if (healWeight + goldWeight + energyWeight + timerWeight + trapWeight <= 0)
        {
            healWeight = 1;
            goldWeight = 1;
            energyWeight = 1;
            timerWeight = 1;
            trapWeight = 1;
        }

        if (goldUnlockLevel + timerUnlockLevel + energyUnlockLevel + healUnlockLevel + trapUnlockLevel <= 0)
        {
            goldUnlockLevel = 6;
            timerUnlockLevel = 11;
            energyUnlockLevel = 16;
            healUnlockLevel = 21;
            trapUnlockLevel = 26;
        }
    }

    /// <summary>Цвет зоны по её типу.</summary>
    public Color GetColor(CombatZoneType type)
    {
        switch (type)
        {
            case CombatZoneType.Heal: return healColor;
            case CombatZoneType.Gold: return goldColor;
            case CombatZoneType.Energy: return energyColor;
            case CombatZoneType.Timer: return timerColor;
            case CombatZoneType.Trap: return trapColor;
            default: return attackColor;
        }
    }

    /// <summary>
    /// 50% — зона атаки, иначе случайная доступная на уровне не-атакующая зона.
    /// Веса из инспектора теперь работают как вкл/выкл (0 — тип отключён).
    /// Не больше 3 не-атакующих зон подряд.
    /// </summary>
    private CombatZoneType PickType()
    {
        int level = Mathf.Max(1, settings.roundLevel);

        // 4 не-атакующие зоны подряд запрещены — следующая только атака.
        if (nonAttackStreak >= 3)
            return CombatZoneType.Attack;

        CombatZoneType[] specialTypes =
        {
            CombatZoneType.Gold,
            CombatZoneType.Timer,
            CombatZoneType.Energy,
            CombatZoneType.Heal,
            CombatZoneType.Trap
        };

        int[] weights = { goldWeight, timerWeight, energyWeight, healWeight, trapWeight };

        int count = 0;

        for (int i = 0; i < specialTypes.Length; i++)
        {
            if (weights[i] > 0 && IsTypeAvailable(specialTypes[i], level))
                count++;
        }

        // Других зон на этом уровне нет — только атака.
        if (count == 0)
            return CombatZoneType.Attack;

        // 50% — атака.
        if (Random.value < 0.5f)
            return CombatZoneType.Attack;

        // 50% — случайная из доступных не-атакующих зон.
        int pick = Random.Range(0, count);

        for (int i = 0; i < specialTypes.Length; i++)
        {
            if (weights[i] <= 0 || !IsTypeAvailable(specialTypes[i], level))
                continue;

            if (pick-- == 0)
                return specialTypes[i];
        }

        return CombatZoneType.Attack;
    }

    /// <summary>С какого уровня доступен тип зоны.</summary>
    public int UnlockLevel(CombatZoneType type)
    {
        switch (type)
        {
            case CombatZoneType.Gold: return goldUnlockLevel;
            case CombatZoneType.Timer: return timerUnlockLevel;
            case CombatZoneType.Energy: return energyUnlockLevel;
            case CombatZoneType.Heal: return healUnlockLevel;
            case CombatZoneType.Trap: return trapUnlockLevel;
            default: return 1;
        }
    }

    public bool IsTypeAvailable(CombatZoneType type, int level)
    {
        return level >= UnlockLevel(type);
    }

    /// <summary>Локализованное название зоны.</summary>
    public static string GetZoneName(CombatZoneType type)
    {
        string key;
        string fallback;

        switch (type)
        {
            case CombatZoneType.Heal: key = "Localization.Heal"; fallback = "HEAL"; break;
            case CombatZoneType.Gold: key = "Localization.Gold"; fallback = "GOLD"; break;
            case CombatZoneType.Energy: key = "Localization.Energy"; fallback = "ENERGY"; break;
            case CombatZoneType.Timer: key = "Localization.Timer"; fallback = "TIMER"; break;
            case CombatZoneType.Trap: key = "Localization.Trap"; fallback = "TRAP"; break;
            default: key = "Localization.Attack"; fallback = "ATTACK"; break;
        }

        string value = LocalizationManager.Localize(key);

        return string.IsNullOrEmpty(value) || value == key ? fallback : value;
    }

    void Update()
    {
        if (!IsRunning || Paused)
            return;

        // Игровые зоны: вращение, сжатие, анимация появления.
        for (int i = zones.Count - 1; i >= 0; i--)
        {
            ZoneData zone = zones[i];

            if (zone == null || zone.gameObject == null)
            {
                zones.RemoveAt(i);
                continue;
            }

            if (zone.UpdateZone(Time.deltaTime))
            {
                zones.RemoveAt(i);

                // Если зона уже полностью погасла (сжатие до нуля) — сразу удаляем.
                if (zone.closing && zone.fillAmount <= 0.0001f)
                    RemoveZoneObject(zone);
                else
                    BeginFade(zone);
            }
        }

        // Исчезающие зоны: клики по ним уже не регистрируются (их нет в zones).
        for (int i = fadingZones.Count - 1; i >= 0; i--)
        {
            ZoneData zone = fadingZones[i];

            if (zone == null || zone.gameObject == null)
            {
                fadingZones.RemoveAt(i);
                continue;
            }

            if (zone.UpdateZone(Time.deltaTime))
            {
                fadingZones.RemoveAt(i);
                RemoveZoneObject(zone);
            }
        }
    }

    /// <summary>
    /// Плавно убирает зону с кольца: fillAmount уходит в 0, затем объект удаляется.
    /// </summary>
    private void BeginFade(ZoneData zone)
    {
        if (zone == null || zone.gameObject == null)
            return;

        zone.closing = true;
        zone.PlayFill(zone.fillAmount, 0f, disappearDuration);
        fadingZones.Add(zone);
    }

    /// <summary>
    /// Старт раунда: полностью пересоздаёт зоны с параметрами раунда.
    /// </summary>
    public void BeginRound(ZoneRoundSettings roundSettings)
    {
        StopRound();
        settings = roundSettings ?? new ZoneRoundSettings();
        nonAttackStreak = 0;
        IsRunning = true;

        int initial = Mathf.Clamp(settings.initialZoneCount, 0, settings.maxZoneCount);
        for (int i = 0; i < initial; i++)
            GenerateZone();

        spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    /// <summary>
    /// Останавливает раунд и убирает все зоны с кольца.
    /// </summary>
    public void StopRound()
    {
        IsRunning = false;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        ClearZones();
    }

    public void ClearZones()
    {
        foreach (var zone in zones)
            RemoveZoneObject(zone);

        zones.Clear();

        foreach (var zone in fadingZones)
            RemoveZoneObject(zone);

        fadingZones.Clear();
    }

    private IEnumerator SpawnRoutine()
    {
        while (IsRunning)
        {
            // WaitForSeconds кешируем, чтобы не создавать объект каждую итерацию.
            if (spawnWait == null || !Mathf.Approximately(spawnWaitInterval, settings.spawnInterval))
            {
                spawnWaitInterval = settings.spawnInterval;
                spawnWait = new WaitForSeconds(spawnWaitInterval);
            }

            yield return spawnWait;

            if (!IsRunning)
                break;

            // Во время паузы новые зоны не появляются.
            if (Paused)
                continue;

            if (zones.Count < settings.maxZoneCount)
                GenerateZone();
        }
    }

    private void GenerateZone()
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            float width = Random.Range(settings.minZoneWidth, settings.maxZoneWidth);
            float start = Random.Range(0f, 360f);

            // Переиспользуем пробную зону вместо new ZoneData на каждую попытку.
            probeZone.startAngle = start;
            probeZone.width = width;

            if (!HasOverlap(probeZone))
            {
                CreateZoneObject(start, width);
                return;
            }
        }

        // fallback: не смогли найти свободное место — ставим куда получилось
        float fallbackWidth = Random.Range(settings.minZoneWidth, settings.maxZoneWidth);
        float fallbackStart = Random.Range(0f, 360f);
        CreateZoneObject(fallbackStart, fallbackWidth);
    }

    private void CreateZoneObject(float startAngle, float width)
    {
        if (zonePrefab == null)
        {
            Debug.LogError("[ZoneManager] Не задан префаб зоны!");
            return;
        }

        GameObject obj = Instantiate(zonePrefab, zonesParent);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.rotation = Quaternion.Euler(0f, 0f, startAngle);

        Image image = obj.GetComponent<Image>();
        if (image == null)
        {
            Debug.LogError("[ZoneManager] Префаб зоны не содержит Image!");
            Destroy(obj);
            return;
        }

        Material mat = Instantiate(image.material);
        image.material = mat;
        mat.SetFloat("_FillAmount", 0f);

        float shrink = settings.maxShrinkSpeed > 0f
            ? Random.Range(settings.minShrinkSpeed, settings.maxShrinkSpeed)
            : 0f;

        // Все зоны симметричны: и появляются, и исчезают в обе стороны от центра.
        mat.SetFloat("_Symmetric", 1f);

        // Сектор рисуется от центра объекта — поворачиваем его на полширины.
        obj.transform.rotation = Quaternion.Euler(0f, 0f, startAngle + width * 0.5f);

        float rot = 0f;
        if (settings.maxRotationSpeed > 0f)
        {
            float sign = settings.randomRotationDirection && Random.value > 0.5f ? -1f : 1f;
            rot = Random.Range(settings.minRotationSpeed, settings.maxRotationSpeed) * sign;
        }

        CombatZoneType type = PickType();

        if (type == CombatZoneType.Attack)
            nonAttackStreak = 0;
        else
            nonAttackStreak++;

        image.color = GetColor(type);

        // Надпись зоны: у префаба стоит автоперевод на ATTACK — выключаем и пишем свой текст.
        TMP_Text label = obj.GetComponentInChildren<TMP_Text>(true);

        if (label != null)
        {
            Translate translate = label.GetComponent<Translate>();

            if (translate != null)
                translate.enabled = false;

            label.text = GetZoneName(type);

            // Шрифт по языку (Silver SDF для азиатских).
            Translate.ApplyFont(label);
        }

        ZoneData newZone = new ZoneData(startAngle, width, obj, mat, shrink, rot, type);

        // Зона не появляется мгновенно: fillAmount плавно растёт от 0 до нужной ширины.
        newZone.PlayFill(0f, width / 360f, appearDuration);

        zones.Add(newZone);
    }

    private void RemoveZoneObject(ZoneData zone)
    {
        if (zone == null)
            return;

        if (zone.material != null)
            Destroy(zone.material);

        if (zone.gameObject != null)
            Destroy(zone.gameObject);
    }

    private bool HasOverlap(ZoneData newZone)
    {
        for (int i = 0; i < zones.Count; i++)
        {
            if (ZonesOverlap(zones[i], newZone))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Проверка пересечения секторов без выделения памяти (без промежуточных списков).
    /// </summary>
    public bool ZonesOverlap(ZoneData a, ZoneData b)
    {
        if (a == null || b == null)
            return false;

        float aStart = NormalizeAngle(a.startAngle);
        float aEnd = aStart + Mathf.Max(0f, a.width);
        float bStart = NormalizeAngle(b.startAngle);
        float bEnd = bStart + Mathf.Max(0f, b.width);

        // Прямое сравнение и два сдвига на оборот — покрывают перенос через 0°.
        return SegmentsOverlap(aStart, aEnd, bStart, bEnd)
            || SegmentsOverlap(aStart, aEnd, bStart + 360f, bEnd + 360f)
            || SegmentsOverlap(aStart + 360f, aEnd + 360f, bStart, bEnd);
    }

    private static bool SegmentsOverlap(float a1, float a2, float b1, float b2) => a1 < b2 && b1 < a2;

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    public void RemoveZone(ZoneData zone)
    {
        if (zone == null || !zones.Remove(zone))
            return;

        // Из списка игровых зон убрана сразу — повторные клики по ней не проходят.
        BeginFade(zone);
    }

    /// <summary>
    /// Скрыть/показать все зоны на кольце (используется в паузе).
    /// </summary>
    public void SetZonesVisible(bool visible)
    {
        for (int i = 0; i < zones.Count; i++)
        {
            ZoneData zone = zones[i];

            if (zone != null && zone.gameObject != null && zone.gameObject.activeSelf != visible)
                zone.gameObject.SetActive(visible);
        }

        for (int i = 0; i < fadingZones.Count; i++)
        {
            ZoneData zone = fadingZones[i];

            if (zone != null && zone.gameObject != null && zone.gameObject.activeSelf != visible)
                zone.gameObject.SetActive(visible);
        }
    }
}