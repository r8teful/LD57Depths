using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Assets.SimpleLocalization.Scripts
{
	/// <summary>
	/// Localization manager.
	/// </summary>
    public static class LocalizationManager
    {
		/// <summary>
		/// Fired when localization changed.
		/// </summary>
        public static event Action OnLocalizationChanged = () => { }; 

        public static Dictionary<string, Dictionary<string, string>> Dictionary = new(); // <Language, <Key, Translation>>
        private static string _language = "English";

		/// <summary>
		/// Get or set language.
		/// </summary>
        public static string Language
        {
            get => _language;
            set { _language = value; OnLocalizationChanged(); }
        }

		/// <summary>
		/// Set default language.
		/// </summary>
        public static void AutoLanguage()
        {
            Language = "English";
        }

        /// <summary>
        /// Read localization spreadsheets.
        /// </summary>
        public static void Read()
        {
            if (Dictionary.Count > 0) return;

            var keys = new List<string>();

            foreach (var sheet in LocalizationSettings.Instance.Sheets)
            {
                var textAsset = sheet.TextAsset;
                var lines = GetLines(textAsset.text);
				var languages = lines[0].Split(',').Select(i => i.Trim()).ToList();

                if (languages.Count != languages.Distinct().Count())
                {
                    Debug.LogError($"Duplicated languages found in `{sheet.Name}`. This sheet is not loaded.");
                    continue;
                }

                for (var i = 1; i < languages.Count; i++)
                {
                    if (!Dictionary.ContainsKey(languages[i]))
                    {
                        Dictionary.Add(languages[i], new Dictionary<string, string>());
                    }
                }

                for (var i = 1; i < lines.Count; i++)
                {
                    var columns = GetColumns(lines[i]);
                    var key = columns[0];

                    if (key == "") continue;

                    if (keys.Contains(key))
                    {
                        Debug.LogError($"Duplicated key `{key}` found in `{sheet.Name}`. This key is not loaded.");
                        continue;
                    }

                    keys.Add(key);

                    for (var j = 1; j < languages.Count; j++)
                    {
                        if (Dictionary[languages[j]].ContainsKey(key))
                        {
                            Debug.LogError($"Duplicated key `{key}` in `{sheet.Name}`.");
                        }
                        else
                        {
                            Dictionary[languages[j]].Add(key, columns[j]);
                        }
                    }
                }
            }

            AutoLanguage();
        }

        /// <summary>
        /// Check if a key exists in localization.
        /// </summary>
        public static bool HasKey(string localizationKey)
        {
            return Dictionary.ContainsKey(Language) && Dictionary[Language].ContainsKey(localizationKey);
        }

        /// <summary>
        /// Get localized value by localization key.
        /// </summary>
        public static bool TryLocalize(string localizationKey, out string localizedText) {
            localizedText = string.Empty;

            if (string.IsNullOrEmpty(localizationKey))
                return false;

            if (Dictionary.Count == 0)
                Read();

            if (!Dictionary.TryGetValue(Language, out var languageTable))
                throw new KeyNotFoundException("Language not found: " + Language);

            if (languageTable.TryGetValue(localizationKey, out var translated) &&
                !string.IsNullOrEmpty(translated)) {
                localizedText = translated;
                return true;
            }

            Debug.LogWarning($"Translation not found: {localizationKey} ({Language}).");

            if (Dictionary.TryGetValue("English", out var englishTable) &&
                englishTable.TryGetValue(localizationKey, out translated) &&
                !string.IsNullOrEmpty(translated)) {
                localizedText = translated;
                return false;
            }
            return false;
        }

        /// <summary>
        /// Get localized value by localization key.
        /// </summary>
        public static bool TryLocalize(string localizationKey, out string localizedText, params object[] args)
        {
            localizedText = string.Empty;
            if (TryLocalize(localizationKey, out var text)) {
                localizedText =  string.Format(text, args);
                return true;
            }
            return false;
        }

        public static List<string> GetLines(string text)
        {
            text = text.Replace("\r\n", "\n").Replace("\"\"", "[_quote_]");
            
            var matches = Regex.Matches(text, "\"[\\s\\S]+?\"");

            foreach (Match match in matches)
            {
                text = text.Replace(match.Value, match.Value.Replace("\"", null).Replace(",", "[_comma_]").Replace("\n", "[_newline_]"));
            }

            // Making uGUI line breaks to work in asian texts.
            text = text.Replace("。", "。 ").Replace("、", "、 ").Replace("：", "： ").Replace("！", "！ ").Replace("（", " （").Replace("）", "） ").Trim();

            return text.Split('\n').Where(i => i != "").ToList();
        }

        public static List<string> GetColumns(string line)
        {
            return line.Split(',').Select(j => j.Trim()).Select(j => j.Replace("[_quote_]", "\"").Replace("[_comma_]", ",").Replace("[_newline_]", "\n")).ToList();
        }
    }
}