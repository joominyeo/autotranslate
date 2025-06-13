using System.Collections.Generic;

namespace AutoTranslate.Core
{
    public class LanguageManager
    {
        private static readonly Dictionary<string, Language> _languages = new()
        {
            { "auto", new Language("auto", "Auto-detect", "🌐") },
            { "af", new Language("af", "Afrikaans", "🇿🇦") },
            { "sq", new Language("sq", "Albanian", "🇦🇱") },
            { "am", new Language("am", "Amharic", "🇪🇹") },
            { "ar", new Language("ar", "Arabic", "🇸🇦") },
            { "hy", new Language("hy", "Armenian", "🇦🇲") },
            { "az", new Language("az", "Azerbaijani", "🇦🇿") },
            { "eu", new Language("eu", "Basque", "🇪🇸") },
            { "be", new Language("be", "Belarusian", "🇧🇾") },
            { "bn", new Language("bn", "Bengali", "🇧🇩") },
            { "bs", new Language("bs", "Bosnian", "🇧🇦") },
            { "bg", new Language("bg", "Bulgarian", "🇧🇬") },
            { "ca", new Language("ca", "Catalan", "🇪🇸") },
            { "ceb", new Language("ceb", "Cebuano", "🇵🇭") },
            { "ny", new Language("ny", "Chichewa", "🇲🇼") },
            { "zh", new Language("zh", "Chinese (Simplified)", "🇨🇳") },
            { "zh-TW", new Language("zh-TW", "Chinese (Traditional)", "🇹🇼") },
            { "co", new Language("co", "Corsican", "🇫🇷") },
            { "hr", new Language("hr", "Croatian", "🇭🇷") },
            { "cs", new Language("cs", "Czech", "🇨🇿") },
            { "da", new Language("da", "Danish", "🇩🇰") },
            { "nl", new Language("nl", "Dutch", "🇳🇱") },
            { "en", new Language("en", "English", "🇺🇸") },
            { "eo", new Language("eo", "Esperanto", "🌍") },
            { "et", new Language("et", "Estonian", "🇪🇪") },
            { "tl", new Language("tl", "Filipino", "🇵🇭") },
            { "fi", new Language("fi", "Finnish", "🇫🇮") },
            { "fr", new Language("fr", "French", "🇫🇷") },
            { "fy", new Language("fy", "Frisian", "🇳🇱") },
            { "gl", new Language("gl", "Galician", "🇪🇸") },
            { "ka", new Language("ka", "Georgian", "🇬🇪") },
            { "de", new Language("de", "German", "🇩🇪") },
            { "el", new Language("el", "Greek", "🇬🇷") },
            { "gu", new Language("gu", "Gujarati", "🇮🇳") },
            { "ht", new Language("ht", "Haitian Creole", "🇭🇹") },
            { "ha", new Language("ha", "Hausa", "🇳🇬") },
            { "haw", new Language("haw", "Hawaiian", "🇺🇸") },
            { "iw", new Language("iw", "Hebrew", "🇮🇱") },
            { "he", new Language("he", "Hebrew", "🇮🇱") },
            { "hi", new Language("hi", "Hindi", "🇮🇳") },
            { "hmn", new Language("hmn", "Hmong", "🇱🇦") },
            { "hu", new Language("hu", "Hungarian", "🇭🇺") },
            { "is", new Language("is", "Icelandic", "🇮🇸") },
            { "ig", new Language("ig", "Igbo", "🇳🇬") },
            { "id", new Language("id", "Indonesian", "🇮🇩") },
            { "ga", new Language("ga", "Irish", "🇮🇪") },
            { "it", new Language("it", "Italian", "🇮🇹") },
            { "ja", new Language("ja", "Japanese", "🇯🇵") },
            { "jw", new Language("jw", "Javanese", "🇮🇩") },
            { "kn", new Language("kn", "Kannada", "🇮🇳") },
            { "kk", new Language("kk", "Kazakh", "🇰🇿") },
            { "km", new Language("km", "Khmer", "🇰🇭") },
            { "ko", new Language("ko", "Korean", "🇰🇷") },
            { "ku", new Language("ku", "Kurdish (Kurmanji)", "🇹🇷") },
            { "ky", new Language("ky", "Kyrgyz", "🇰🇬") },
            { "lo", new Language("lo", "Lao", "🇱🇦") },
            { "la", new Language("la", "Latin", "🏛️") },
            { "lv", new Language("lv", "Latvian", "🇱🇻") },
            { "lt", new Language("lt", "Lithuanian", "🇱🇹") },
            { "lb", new Language("lb", "Luxembourgish", "🇱🇺") },
            { "mk", new Language("mk", "Macedonian", "🇲🇰") },
            { "mg", new Language("mg", "Malagasy", "🇲🇬") },
            { "ms", new Language("ms", "Malay", "🇲🇾") },
            { "ml", new Language("ml", "Malayalam", "🇮🇳") },
            { "mt", new Language("mt", "Maltese", "🇲🇹") },
            { "mi", new Language("mi", "Maori", "🇳🇿") },
            { "mr", new Language("mr", "Marathi", "🇮🇳") },
            { "mn", new Language("mn", "Mongolian", "🇲🇳") },
            { "my", new Language("my", "Myanmar (Burmese)", "🇲🇲") },
            { "ne", new Language("ne", "Nepali", "🇳🇵") },
            { "no", new Language("no", "Norwegian", "🇳🇴") },
            { "or", new Language("or", "Odia", "🇮🇳") },
            { "ps", new Language("ps", "Pashto", "🇦🇫") },
            { "fa", new Language("fa", "Persian", "🇮🇷") },
            { "pl", new Language("pl", "Polish", "🇵🇱") },
            { "pt", new Language("pt", "Portuguese", "🇵🇹") },
            { "pa", new Language("pa", "Punjabi", "🇮🇳") },
            { "ro", new Language("ro", "Romanian", "🇷🇴") },
            { "ru", new Language("ru", "Russian", "🇷🇺") },
            { "sm", new Language("sm", "Samoan", "🇼🇸") },
            { "gd", new Language("gd", "Scots Gaelic", "🏴󠁧󠁢󠁳󠁣󠁴󠁿") },
            { "sr", new Language("sr", "Serbian", "🇷🇸") },
            { "st", new Language("st", "Sesotho", "🇱🇸") },
            { "sn", new Language("sn", "Shona", "🇿🇼") },
            { "sd", new Language("sd", "Sindhi", "🇵🇰") },
            { "si", new Language("si", "Sinhala", "🇱🇰") },
            { "sk", new Language("sk", "Slovak", "🇸🇰") },
            { "sl", new Language("sl", "Slovenian", "🇸🇮") },
            { "so", new Language("so", "Somali", "🇸🇴") },
            { "es", new Language("es", "Spanish", "🇪🇸") },
            { "su", new Language("su", "Sundanese", "🇮🇩") },
            { "sw", new Language("sw", "Swahili", "🇰🇪") },
            { "sv", new Language("sv", "Swedish", "🇸🇪") },
            { "tg", new Language("tg", "Tajik", "🇹🇯") },
            { "ta", new Language("ta", "Tamil", "🇮🇳") },
            { "te", new Language("te", "Telugu", "🇮🇳") },
            { "th", new Language("th", "Thai", "🇹🇭") },
            { "tr", new Language("tr", "Turkish", "🇹🇷") },
            { "uk", new Language("uk", "Ukrainian", "🇺🇦") },
            { "ur", new Language("ur", "Urdu", "🇵🇰") },
            { "ug", new Language("ug", "Uyghur", "🇨🇳") },
            { "uz", new Language("uz", "Uzbek", "🇺🇿") },
            { "vi", new Language("vi", "Vietnamese", "🇻🇳") },
            { "cy", new Language("cy", "Welsh", "🏴󠁧󠁢󠁷󠁬󠁳󠁿") },
            { "xh", new Language("xh", "Xhosa", "🇿🇦") },
            { "yi", new Language("yi", "Yiddish", "🇮🇱") },
            { "yo", new Language("yo", "Yoruba", "🇳🇬") },
            { "zu", new Language("zu", "Zulu", "🇿🇦") }
        };

        public static IEnumerable<Language> GetAllLanguages()
        {
            return _languages.Values.OrderBy(l => l.Name);
        }

        public static IEnumerable<Language> GetTranslatableLanguages()
        {
            return _languages.Values.Where(l => l.Code != "auto").OrderBy(l => l.Name);
        }

        public static Language? GetLanguage(string code)
        {
            _languages.TryGetValue(code, out var language);
            return language;
        }

        public static string GetLanguageName(string code)
        {
            var language = GetLanguage(code);
            return language?.Name ?? code;
        }

        public static bool IsValidLanguageCode(string code)
        {
            return _languages.ContainsKey(code);
        }

        public static IEnumerable<Language> GetPopularLanguages()
        {
            var popularCodes = new[] { "auto", "en", "es", "fr", "de", "it", "pt", "ru", "ja", "ko", "zh", "ar", "hi" };
            return popularCodes.Select(code => _languages[code]);
        }

        public static string DetectLanguageFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "auto";

            // Simple heuristic-based language detection
            // In a production app, you'd use a proper language detection library
            
            // Check for common patterns
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"[\u4e00-\u9fff]"))
                return "zh"; // Chinese characters
                
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"[\u3040-\u309f\u30a0-\u30ff]"))
                return "ja"; // Japanese hiragana/katakana
                
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"[\uac00-\ud7af]"))
                return "ko"; // Korean hangul
                
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"[\u0600-\u06ff]"))
                return "ar"; // Arabic
                
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"[\u0400-\u04ff]"))
                return "ru"; // Cyrillic (Russian)
                
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"[\u0900-\u097f]"))
                return "hi"; // Hindi
                
            // For Latin-based languages, return auto to let the API detect
            return "auto";
        }
    }

    public class Language
    {
        public string Code { get; }
        public string Name { get; }
        public string Flag { get; }

        public Language(string code, string name, string flag)
        {
            Code = code;
            Name = name;
            Flag = flag;
        }

        public string DisplayName => $"{Flag} {Name}";

        public override string ToString() => DisplayName;
        
        public override bool Equals(object? obj)
        {
            return obj is Language language && Code == language.Code;
        }

        public override int GetHashCode()
        {
            return Code.GetHashCode();
        }
    }
}