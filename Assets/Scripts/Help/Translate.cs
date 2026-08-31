using UnityEngine;
using UnityEngine.UI;
using Assets.SimpleLocalization;
using TMPro;

public class Translate : MonoBehaviour
{
    Text _text;
    TextMeshProUGUI _textMesh;
    string startValue;
    bool isTextPro = false;
    public Font asianFont, startFont;
    int startFontSize;

    private void OnEnable()
    {
        TranslateText();
    }

    private void Awake()
    {
        try
        {
            _text = GetComponent<Text>();
            startFontSize = _text.fontSize;
            startValue = _text.text;
        }
        catch
        {
            _textMesh = GetComponent<TextMeshProUGUI>();
            isTextPro = true;
            startValue = _textMesh.text;
        }
    }

    private void Start()
    {
        TranslateText();
    }

    public void TranslateText()
    {
        if (FBPP.GetString("Language") == "")
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Russian:
                    LocalizationManager.Language = "Russian";
                    break;
                case SystemLanguage.English:
                    LocalizationManager.Language = "English";
                    break;
                case SystemLanguage.Chinese:
                    LocalizationManager.Language = "Chinese";
                    break;
                case SystemLanguage.ChineseSimplified:
                    LocalizationManager.Language = "Chinese";
                    break;
                case SystemLanguage.ChineseTraditional:
                    LocalizationManager.Language = "Chinese";
                    break;
                case SystemLanguage.Spanish:
                    LocalizationManager.Language = "Spanish";
                    break;
                case SystemLanguage.Portuguese:
                    LocalizationManager.Language = "Portuguese";
                    break;
                case SystemLanguage.French:
                    LocalizationManager.Language = "French";
                    break;
                case SystemLanguage.German:
                    LocalizationManager.Language = "German";
                    break;
                case SystemLanguage.Turkish:
                    LocalizationManager.Language = "Turkish";
                    break;
                case SystemLanguage.Japanese:
                    LocalizationManager.Language = "Japanese";
                    break;
                case SystemLanguage.Korean:
                    LocalizationManager.Language = "Korean";
                    break;
                case SystemLanguage.Polish:
                    LocalizationManager.Language = "Polish";
                    break;
                case SystemLanguage.Italian:
                    LocalizationManager.Language = "Italian";
                    break;
                default:
                    LocalizationManager.Language = "English";
                    break;
            }
        }
        else
        {
            LocalizationManager.Language = FBPP.GetString("Language");
        }
        if (!isTextPro)
        {
            _text.text = startValue;
            try 
            {
                if (LocalizationManager.Language == "Korean" || LocalizationManager.Language == "Japanese" || LocalizationManager.Language == "Chinese")
                {
                    _text.font = asianFont;
                    _text.fontSize = Mathf.RoundToInt(startFontSize * 1.5f);
                    _text.resizeTextMaxSize = Mathf.RoundToInt(startFontSize * 1.5f);
                }
                else
                {
                    _text.font = startFont;
                    _text.fontSize = startFontSize;
                    _text.resizeTextMaxSize = startFontSize;
                }
                _text.text = LocalizationManager.Localize(_text.text);
                _text.text = _text.text.Replace("<br>", "\n");
            }
            catch { /*Debug.Log("no language");*/ }
        }
        else
        {
            _textMesh.text = startValue;
            try 
            {
                _textMesh.text = LocalizationManager.Localize(_textMesh.text);
            }
            catch { Debug.Log(_textMesh.text); }
        }
    }
}