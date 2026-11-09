using UnityEngine;

/// <summary>
/// Набор кадров анимаций одного врага: Idle (всегда), Attack (урон по таймеру), Defeat (смерть).
/// </summary>
[System.Serializable]
public class CharacterData
{
    [Tooltip("Имя персонажа (папка в Assets/Sprites/Characters).")]
    public string id;

    [Tooltip("Кадры Idle — проигрываются по кругу.")]
    public Sprite[] idleFrames;

    [Tooltip("Кадры Attack — атака по герою, когда кончился таймер.")]
    public Sprite[] attackFrames;

    [Tooltip("Кадры Defeat — смерть врага.")]
    public Sprite[] defeatFrames;

    public bool HasIdle => idleFrames != null && idleFrames.Length > 0;
    public bool HasAttack => attackFrames != null && attackFrames.Length > 0;
    public bool HasDefeat => defeatFrames != null && defeatFrames.Length > 0;
}

/// <summary>
/// Библиотека всех врагов с кадрами анимаций.
/// Лежит в Resources ("CharacterLibrary"), поэтому грузится без ссылок в сцене.
/// </summary>
[CreateAssetMenu(menuName = "Spiral/Character Library", fileName = "CharacterLibrary")]
public class CharacterLibrary : ScriptableObject
{
    public CharacterData[] characters;
}
