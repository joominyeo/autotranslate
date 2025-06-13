using System.Net.Http;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AutoTranslate.Services
{
    public class TranslationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

        public TranslationService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", _userAgent);
        }

        public async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            try
            {
                // Use Google Translate API (free tier with limitations)
                return await TranslateWithGoogleAsync(text, sourceLanguage, targetLanguage);
            }
            catch (Exception ex)
            {
                // Fallback to LibreTranslate if Google fails
                try
                {
                    return await TranslateWithLibreTranslateAsync(text, sourceLanguage, targetLanguage);
                }
                catch
                {
                    throw new TranslationException($"Translation failed: {ex.Message}", ex);
                }
            }
        }

        private async Task<string> TranslateWithGoogleAsync(string text, string sourceLanguage, string targetLanguage)
        {
            // Using Google Translate's public API endpoint
            var encodedText = HttpUtility.UrlEncode(text);
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sourceLanguage}&tl={targetLanguage}&dt=t&q={encodedText}";

            var response = await _httpClient.GetStringAsync(url);
            
            if (string.IsNullOrEmpty(response))
                throw new TranslationException("Empty response from Google Translate");

            // Parse the JSON response
            var jsonArray = JArray.Parse(response);
            var translatedText = new StringBuilder();

            if (jsonArray[0] is JArray translations)
            {
                foreach (var translation in translations)
                {
                    if (translation is JArray translationArray && translationArray.Count > 0)
                    {
                        translatedText.Append(translationArray[0]?.ToString());
                    }
                }
            }

            return translatedText.ToString();
        }

        private async Task<string> TranslateWithLibreTranslateAsync(string text, string sourceLanguage, string targetLanguage)
        {
            // Using LibreTranslate public API (has usage limits)
            var url = "https://libretranslate.de/translate";
            
            var requestBody = new
            {
                q = text,
                source = sourceLanguage == "auto" ? "auto" : sourceLanguage,
                target = targetLanguage,
                format = "text"
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            
            if (!response.IsSuccessStatusCode)
                throw new TranslationException($"LibreTranslate API error: {response.StatusCode}");

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<LibreTranslateResponse>(responseContent);

            return result?.TranslatedText ?? string.Empty;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    public class LibreTranslateResponse
    {
        [JsonProperty("translatedText")]
        public string TranslatedText { get; set; } = string.Empty;
    }

    public class TranslationException : Exception
    {
        public TranslationException(string message) : base(message) { }
        public TranslationException(string message, Exception innerException) : base(message, innerException) { }
    }
}