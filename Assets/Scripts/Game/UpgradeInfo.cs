using TMPro;
using UnityEngine;

/// <summary>
/// Одна линия прокачки: название, текущий уровень, цены и ссылки на сцену.
/// </summary>
[System.Serializable]
public class UpgradeInfo
{
    public const int MaxLevel = 10;

    [Tooltip("Название для инспектора.")]
    public string name;

    [Tooltip("Ключ локализации названия (например Localization.UpgradeDamage).")]
    public string localizationKey;

    [Tooltip("Зона этой прокачки в Window_update.")]
    public Transform zone;

    [Tooltip("Текст Cell, в котором показывается цена.")]
    public TMP_Text priceText;

    [Range(0, MaxLevel)]
    [Tooltip("Текущий уровень (0..10).")]
    public int level = 0;

    [Tooltip("Цены: prices[i] — стоимость перехода на уровень (i+1).")]
    public int[] prices = new int[MaxLevel];

    [Tooltip("Прибавка к параметру за один уровень (в единицах параметра).\n" +
             "Минус уменьшает параметр (скорость зон, скорость сжатия, скорость стрелки).")]
    public float effectPerLevel = 0f;

    // Исходный цвет текста цены (кешируется в рантайме, не сериализуется).
    [System.NonSerialized] public Color basePriceColor = Color.white;
    [System.NonSerialized] public bool priceColorCached;

    /// <summary>
    /// Значение параметра с учётом текущего уровня (0 уровень — базовое значение).
    /// </summary>
    public float Apply(float baseValue, float minValue = 0f)
    {
        return Mathf.Max(minValue, baseValue + level * effectPerLevel);
    }

    /// <summary>Цена уровня targetLevel (1..10).</summary>
    public int PriceForLevel(int targetLevel)
    {
        if (prices == null || targetLevel < 1 || targetLevel > prices.Length)
            return 0;

        return Mathf.Max(0, prices[targetLevel - 1]);
    }

    /// <summary>Цена следующего уровня (0, если уже максимум).</summary>
    public int NextPrice => level < MaxLevel ? PriceForLevel(level + 1) : 0;

    public bool IsMaxLevel => level >= MaxLevel;
}
