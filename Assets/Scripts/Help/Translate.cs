using UnityEngine;
using Assets.SimpleLocalization;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Локализация текста по ключу из ClickLocalization.txt.
/// Если поле "Key" пустое, ключ подбирается автоматически по тексту объекта:
/// "FIGHT" -> "Localization.Fight", "MENU" -> "Localization.Menu".
/// Шрифт меняется по языку: Silver SDF для азиатских, стартовый (Simplified Gothic) для остальных.
/// </summary>
[DisallowMultipleComponent]
public class Translate : MonoBehaviour
{
    [Tooltip("Ключ из ClickLocalization.txt, например Localization.Fight. Пусто — ключ подбирается по тексту.")]
    public string key;

    [Header("Fonts (TMP)")]
    [Tooltip("Шрифт для обычных языков (Simplified Gothic Regular SDF).")]
    public TMP_FontAsset startFont;

    [Tooltip("Шрифт для азиатских языков (Silver SDF).")]
    public TMP_FontAsset asianFont;

    TMP_Text _textMesh;
    string startValue = string.Empty;
    bool cached = false;

    // Все активные переводы — чтобы после смены языка перевести всё сразу.
    private static readonly List<Translate> Active = new List<Translate>();

    /// <summary>Стартовый шрифт (для не-азиатских языков).</summary>
    public static TMP_FontAsset NormalFont { get; private set; }

    /// <summary>Шрифт для азиатских языков.</summary>
    public static TMP_FontAsset AsianFont { get; private set; }

    /// <summary>
    /// Ставит шрифт по текущему языку (Silver SDF для азиатских, стартовый для остальных).
    /// </summary>
    public static void ApplyFont(TMP_Text text)
    {
        if (text == null)
            return;

        TMP_FontAsset font = IsAsian(LocalizationManager.Language) && AsianFont != null ? AsianFont : NormalFont;

        if (font == null || text.font == font)
            return;

        text.font = font;
        text.fontSharedMaterial = font.material;
    }

    /// <summary>
    /// Переводит все активные тексты заново (вызывается после смены языка).
    /// </summary>
    public static void RefreshAll()
    {
        for (int i = Active.Count - 1; i >= 0; i--)
        {
            Translate translate = Active[i];

            if (translate == null)
            {
                Active.RemoveAt(i);
                continue;
            }

            translate.TranslateText();
        }
    }

    private void OnEnable()
    {
        if (!Active.Contains(this))
            Active.Add(this);

        TranslateText();
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    private void Awake()
    {
        CacheText();
    }

    private void Start()
    {
        TranslateText();
    }

    public void TranslateText()
    {
        CacheText();

        if (_textMesh == null)
            return;

        EnsureLanguage();

        // Шрифты запоминаем статически: ими пользуются и надписи, что пишет GameManager.
        if (startFont != null)
            NormalFont = startFont;

        if (asianFont != null)
            AsianFont = asianFont;

        _textMesh.text = Localize(ResolveSource()).Replace("<br>", "\n");

        ApplyFont(_textMesh);
    }

    private void CacheText()
    {
        if (cached)
            return;

        cached = true;

        _textMesh = GetComponent<TMP_Text>();
        startValue = _textMesh != null ? _textMesh.text : string.Empty;
    }

    private string ResolveSource()
    {
        if (!string.IsNullOrEmpty(key))
            return key;

        if (!string.IsNullOrEmpty(startValue))
            return startValue;

        return _textMesh != null ? _textMesh.text : string.Empty;
    }

    /// <summary>
    /// Порядок поиска: ключ из инспектора, текст как ключ, ключ Localization.Xxx по тексту.
    /// </summary>
    private string Localize(string source)
    {
        if (string.IsNullOrEmpty(source))
            return source;

        if (!string.IsNullOrEmpty(key) && LocalizationManager.TryLocalize(key, out string byKey))
            return byKey;

        if (LocalizationManager.TryLocalize(source, out string byText))
            return byText;

        string derivedKey = BuildKey(source);
        if (!string.IsNullOrEmpty(derivedKey) && LocalizationManager.TryLocalize(derivedKey, out string byDerived))
            return byDerived;

        return source;
    }

    /// <summary>
    /// "FIGHT" -> "Localization.Fight", "Vampirism 25%" -> "Localization.Vampirism".
    /// </summary>
    private static string BuildKey(string source)
    {
        string cleaned = source.Replace("<br>", " ").Trim();

        int end = 0;
        while (end < cleaned.Length && char.IsLetter(cleaned[end]))
            end++;

        if (end == 0)
            return null;

        string word = cleaned.Substring(0, end);

        return "Localization." + char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant();
    }

    private static void EnsureLanguage()
    {
        string saved = FBPP.GetString("Language");

        if (!string.IsNullOrEmpty(saved))
        {
            LocalizationManager.Language = saved;
            return;
        }

        switch (Application.systemLanguage)
        {
            case SystemLanguage.Russian: LocalizationManager.Language = "Russian"; break;
            case SystemLanguage.Spanish: LocalizationManager.Language = "Spanish"; break;
            case SystemLanguage.Portuguese: LocalizationManager.Language = "Portuguese"; break;
            case SystemLanguage.French: LocalizationManager.Language = "French"; break;
            case SystemLanguage.German: LocalizationManager.Language = "German"; break;
            case SystemLanguage.Turkish: LocalizationManager.Language = "Turkish"; break;
            case SystemLanguage.Japanese: LocalizationManager.Language = "Japanese"; break;
            case SystemLanguage.Korean: LocalizationManager.Language = "Korean"; break;
            case SystemLanguage.Polish: LocalizationManager.Language = "Polish"; break;
            case SystemLanguage.Chinese:
            case SystemLanguage.ChineseSimplified:
            case SystemLanguage.ChineseTraditional: LocalizationManager.Language = "Chinese"; break;
            default: LocalizationManager.Language = "English"; break;
        }
    }

    private static bool IsAsian(string language)
    {
        return language == "Korean" || language == "Japanese" || language == "Chinese";
    }
}