using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Assets.SimpleLocalization
{
	/// <summary>
	/// Localization manager.
	/// </summary>
    public static class LocalizationManager
    {
		/// <summary>
		/// Fired when localization changed.
		/// </summary>
        public static event Action LocalizationChanged = () => { }; 

        private static readonly Dictionary<string, Dictionary<string, string>> Dictionary = new Dictionary<string, Dictionary<string, string>>();
        private static string _language = "English";

		/// <summary>
		/// Get or set language.
		/// </summary>
        public static string Language
        {
            get { return _language; }
            set { _language = value; LocalizationChanged(); }
        }

		/// <summary>
		/// Set default language.
		/// </summary>
        public static void AutoLanguage()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Russian:
                    Language = "Russian";
                    break;
                default:
                    Language = "English";
                    break;
            }
        }

        /// <summary>
		/// Read localization spreadsheets.
		/// </summary>
		public static void Read()
        {
            if (Dictionary.Count > 0) return;

            var textAssets = Resources.LoadAll<TextAsset>("Localization");

            foreach (var textAsset in textAssets)
            {
                var text = ReplaceMarkers(textAsset.text);
                var matches = Regex.Matches(text, "\"[\\s\\S]+?\"");

                foreach (Match match in matches)
                {
					text = text.Replace(match.Value, match.Value.Replace("\"", null).Replace(";", "[comma]").Replace("\n", "[newline]"));
                }

                var lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
				if (lines.Length == 0) return;

				var languages = lines[0].Split(';').Select(i => i.Trim()).ToList();

				for (var i = 1; i < languages.Count; i++)
                {
                    if (!Dictionary.ContainsKey(languages[i]))
                    {
                        Dictionary.Add(languages[i], new Dictionary<string, string>());
                    }
                }
				
                for (var i = 1; i < lines.Length; i++)
                {
					var columns = lines[i].Split(';').Select(j => j.Trim()).Select(j => j.Replace("[comma]", ";").Replace("[newline]", "\n")).ToList();
					if (columns.Count == 0) continue;

					var key = columns[0];
					if (string.IsNullOrEmpty(key)) continue;

                    for (var j = 1; j < languages.Count; j++)
                    {
                        // Строка может быть короче заголовка (Google Sheets обрезает пустые ячейки).
                        var value = j < columns.Count ? columns[j] : string.Empty;
                        var language = languages[j];

                        if (string.IsNullOrEmpty(language)) continue;
                        if (!Dictionary.ContainsKey(language)) continue;
                        if (Dictionary[language].ContainsKey(key)) continue;

                        Dictionary[language].Add(key, value);
                    }
                }
            }

            AutoLanguage();
        }

		/// <summary>
		/// Get localized value by localization key.
		/// </summary>
        public static string Localize(string localizationKey)
        {
            if (Dictionary.Count == 0)
            {
                Read();
            }

            string value;
            if (Dictionary.ContainsKey(Language) && Dictionary[Language].TryGetValue(localizationKey, out value))
                return value;

            // Фолбэк: английский, затем русский, затем сам ключ (без исключения).
            if (Dictionary.ContainsKey("English") && Dictionary["English"].TryGetValue(localizationKey, out value))
                return value;

            if (Dictionary.ContainsKey("Russian") && Dictionary["Russian"].TryGetValue(localizationKey, out value))
                return value;

            Debug.LogWarning("[Localization] Перевод не найден: " + localizationKey + " (" + Language + ")");
            return localizationKey;
        }

	    /// <summary>
	    /// Get localized value by localization key.
	    /// </summary>
		public static string Localize(string localizationKey, params object[] args)
        {
            var pattern = Localize(localizationKey);

            return string.Format(pattern, args);
        }

        /// <summary>
        /// Проверяет наличие перевода без предупреждения в консоль.
        /// </summary>
        public static bool TryLocalize(string localizationKey, out string value)
        {
            value = null;

            if (string.IsNullOrEmpty(localizationKey))
                return false;

            if (Dictionary.Count == 0)
                Read();

            if (Dictionary.ContainsKey(Language) && Dictionary[Language].TryGetValue(localizationKey, out value))
                return true;

            if (Dictionary.ContainsKey("English") && Dictionary["English"].TryGetValue(localizationKey, out value))
                return true;

            if (Dictionary.ContainsKey("Russian") && Dictionary["Russian"].TryGetValue(localizationKey, out value))
                return true;

            value = null;
            return false;
        }

        private static string ReplaceMarkers(string text)
        {
            return text.Replace("[Newline]", "\n");
        }
    }
}