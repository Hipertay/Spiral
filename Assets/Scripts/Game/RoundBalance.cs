using UnityEngine;

/// <summary>
/// Полная настройка одного уровня: враги, HP, золото, зоны, скорости, таймер.
/// Редактируется в инспекторе GameManager без пересборки.
/// </summary>
[System.Serializable]
public class RoundBalance
{
    [Tooltip("Сколько врагов в раунде.")]
    public int enemyCount = 1;

    [Tooltip("HP каждого врага раунда.")]
    public int enemyHp = 30;

    [Tooltip("Золото за убийство одного врага.")]
    public int gold = 10;

    [Tooltip("Ширина сектора зоны в градусах (для _FillAmount разделить на 360).")]
    public float zoneWidth = 36f;

    [Tooltip("Максимальное число зон на кольце (старт всегда 2).")]
    public int zoneCount = 3;

    [Tooltip("Скорость вращения зон, градусов/с (0 — не вращаются).")]
    public float rotationSpeed;

    [Tooltip("Скорость сжатия зон, градусов/с (0 — не сжимаются).")]
    public float shrinkSpeed;

    [Tooltip("true — каждая зона крутится в случайную сторону, false — все в одну.")]
    public bool randomRotationDirection;

    [Tooltip("Скорость стрелки, градусов/с.")]
    public float arrowSpeed = 90f;

    [Tooltip("Интервал появления новых зон, с.")]
    public float spawnInterval = 2.5f;

    [Tooltip("Длительность раунда, с (0 — берётся roundDuration).")]
    public float roundDuration = 30f;
}
