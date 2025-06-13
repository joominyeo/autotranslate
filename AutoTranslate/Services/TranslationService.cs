using System.Net.Http;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AutoTranslate.Core;
using System.Net;

namespace AutoTranslate.Services
{
    public class TranslationService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
        private readonly ConfigurationManager _configManager;
        private DateTime _lastRequestTime = DateTime.MinValue;
        private readonly TimeSpan _rateLimitDelay = TimeSpan.FromMilliseconds(100);

        public TranslationService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", _userAgent);
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _configManager = new ConfigurationManager();
        }

        public async Task<TranslationResult> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
        {
            var result = new TranslationResult();
            
            if (string.IsNullOrWhiteSpace(text))
            {
                result.Success = false;
                result.ErrorMessage = "Input text is empty";
                return result;
            }

            if (text.Length > 5000)
            {
                result.Success = false;
                result.ErrorMessage = "Text too long (max 5000 characters)";
                return result;
            }

            try
            {
                // Rate limiting
                await EnforceRateLimitAsync();

                // Detect source language if auto
                var detectedSourceLang = sourceLanguage;
                if (sourceLanguage == "auto")
                {
                    detectedSourceLang = await DetectLanguageAsync(text);
                    result.DetectedSourceLanguage = detectedSourceLang;
                }

                // Skip translation if source and target are the same
                if (detectedSourceLang == targetLanguage)
                {
                    result.Success = true;
                    result.TranslatedText = text;
                    result.SourceLanguage = detectedSourceLang;
                    result.TargetLanguage = targetLanguage;
                    result.Method = "No translation needed";
                    return result;
                }

                // Try Google Translate first
                try
                {
                    var translation = await TranslateWithGoogleAsync(text, detectedSourceLang, targetLanguage);
                    result.Success = true;
                    result.TranslatedText = translation;
                    result.SourceLanguage = detectedSourceLang;
                    result.TargetLanguage = targetLanguage;
                    result.Method = "Google Translate";
                    return result;
                }
                catch (Exception googleEx)
                {
                    result.ErrorMessage = $"Google Translate failed: {googleEx.Message}";
                    
                    // Fallback to LibreTranslate
                    try
                    {
                        var translation = await TranslateWithLibreTranslateAsync(text, detectedSourceLang, targetLanguage);
                        result.Success = true;
                        result.TranslatedText = translation;
                        result.SourceLanguage = detectedSourceLang;
                        result.TargetLanguage = targetLanguage;
                        result.Method = "LibreTranslate";
                        result.ErrorMessage = null;
                        return result;
                    }
                    catch (Exception libreEx)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"All translation services failed. Google: {googleEx.Message}, LibreTranslate: {libreEx.Message}";
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Translation failed: {ex.Message}";
                return result;
            }
        }

        private async Task EnforceRateLimitAsync()
        {
            var timeSinceLastRequest = DateTime.Now - _lastRequestTime;
            if (timeSinceLastRequest < _rateLimitDelay)
            {
                await Task.Delay(_rateLimitDelay - timeSinceLastRequest);
            }
            _lastRequestTime = DateTime.Now;
        }

        public async Task<string> DetectLanguageAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "auto";

            try
            {
                // Use client-side heuristics first
                var heuristicResult = LanguageManager.DetectLanguageFromText(text);
                if (heuristicResult != "auto")
                    return heuristicResult;

                // Use Google Translate detection
                var encodedText = HttpUtility.UrlEncode(text.Substring(0, Math.Min(text.Length, 100)));
                var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl=en&dt=t&q={encodedText}";

                var response = await _httpClient.GetStringAsync(url);
                var jsonArray = JArray.Parse(response);
                
                if (jsonArray.Count > 2 && jsonArray[2] != null)
                {
                    return jsonArray[2].ToString();
                }
                
                return "auto";
            }
            catch
            {
                return LanguageManager.DetectLanguageFromText(text);
            }
        }

        private async Task<string> TranslateWithGoogleAsync(string text, string sourceLanguage, string targetLanguage)
        {
            try
            {
                var config = _configManager.LoadConfiguration();
                
                // Check if we have an API key for official Google Translate API
                if (!string.IsNullOrWhiteSpace(config.GoogleTranslateApiKey))
                {
                    return await TranslateWithOfficialGoogleApiAsync(text, sourceLanguage, targetLanguage, config.GoogleTranslateApiKey);
                }
                
                // Fallback to public endpoint
                var encodedText = HttpUtility.UrlEncode(text);
                var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sourceLanguage}&tl={targetLanguage}&dt=t&q={encodedText}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Referer", "https://translate.google.com/");
                
                var response = await _httpClient.SendAsync(request);
                
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    throw new TranslationException("Rate limit exceeded. Please wait a moment before trying again.");
                }
                
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrEmpty(responseContent))
                    throw new TranslationException("Empty response from Google Translate");

                // Parse the JSON response
                var jsonArray = JArray.Parse(responseContent);
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

                var result = translatedText.ToString();
                if (string.IsNullOrWhiteSpace(result))
                    throw new TranslationException("Google Translate returned empty translation");
                    
                return result;
            }
            catch (JsonException ex)
            {
                throw new TranslationException($"Failed to parse Google Translate response: {ex.Message}", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new TranslationException($"Network error contacting Google Translate: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new TranslationException("Google Translate request timed out", ex);
            }
        }

        private async Task<string> TranslateWithOfficialGoogleApiAsync(string text, string sourceLanguage, string targetLanguage, string apiKey)
        {
            var url = $"https://translation.googleapis.com/language/translate/v2?key={apiKey}";
            
            var requestBody = new
            {
                q = text,
                source = sourceLanguage == "auto" ? null : sourceLanguage,
                target = targetLanguage,
                format = "text"
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new TranslationException("Invalid Google Translate API key");
            }
            
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new TranslationException("Google Translate API access forbidden. Check your API key and billing settings.");
            }
            
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<GoogleTranslateApiResponse>(responseContent);

            return result?.Data?.Translations?.FirstOrDefault()?.TranslatedText ?? string.Empty;
        }

        private async Task<string> TranslateWithLibreTranslateAsync(string text, string sourceLanguage, string targetLanguage)
        {
            try
            {
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
                
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    throw new TranslationException("LibreTranslate rate limit exceeded. Please wait a moment before trying again.");
                }
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new TranslationException($"LibreTranslate API error ({response.StatusCode}): {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<LibreTranslateResponse>(responseContent);

                if (string.IsNullOrWhiteSpace(result?.TranslatedText))
                    throw new TranslationException("LibreTranslate returned empty translation");

                return result.TranslatedText;
            }
            catch (JsonException ex)
            {
                throw new TranslationException($"Failed to parse LibreTranslate response: {ex.Message}", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new TranslationException($"Network error contacting LibreTranslate: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new TranslationException("LibreTranslate request timed out", ex);
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public class TranslationResult
    {
        public bool Success { get; set; }
        public string TranslatedText { get; set; } = string.Empty;
        public string SourceLanguage { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
        public string? DetectedSourceLanguage { get; set; }
        public string Method { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    public class GoogleTranslateApiResponse
    {
        [JsonProperty("data")]
        public GoogleTranslateData? Data { get; set; }
    }

    public class GoogleTranslateData
    {
        [JsonProperty("translations")]
        public GoogleTranslation[]? Translations { get; set; }
    }

    public class GoogleTranslation
    {
        [JsonProperty("translatedText")]
        public string TranslatedText { get; set; } = string.Empty;
        
        [JsonProperty("detectedSourceLanguage")]
        public string? DetectedSourceLanguage { get; set; }
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