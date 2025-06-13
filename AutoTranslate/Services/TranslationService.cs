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
        private readonly TranslationCache _cache;
        private readonly TranslationHistory _history;
        private readonly UsageStatistics _statistics;
        private DateTime _lastRequestTime = DateTime.MinValue;
        private readonly TimeSpan _rateLimitDelay = TimeSpan.FromMilliseconds(100);

        public TranslationService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", _userAgent);
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _configManager = new ConfigurationManager();
            _cache = new TranslationCache();
            _history = new TranslationHistory();
            _statistics = new UsageStatistics();
            
            // Load timeout from configuration
            try
            {
                var config = _configManager.LoadConfiguration();
                _httpClient.Timeout = TimeSpan.FromSeconds(config.TranslationTimeoutSeconds);
            }
            catch
            {
                _httpClient.Timeout = TimeSpan.FromSeconds(10);
            }
        }

        public async Task<TranslationResult> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
        {
            var startTime = DateTime.Now;
            var result = new TranslationResult();
            
            try
            {
                Logger.Debug($"Translation request: '{text.Substring(0, Math.Min(50, text.Length))}...' ({sourceLanguage} -> {targetLanguage})");
                
                // Validate input
                if (string.IsNullOrWhiteSpace(text))
                {
                    result.Success = false;
                    result.ErrorMessage = "Input text is empty";
                    _statistics.RecordTranslation(sourceLanguage, targetLanguage, 0, false);
                    return result;
                }

                var config = _configManager.LoadConfiguration();
                if (text.Length > config.MaxTextLength)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Text too long (max {config.MaxTextLength} characters)";
                    _statistics.RecordTranslation(sourceLanguage, targetLanguage, text.Length, false);
                    return result;
                }

                // Detect source language if auto
                var detectedSourceLang = sourceLanguage;
                if (sourceLanguage == "auto")
                {
                    detectedSourceLang = await DetectLanguageAsync(text);
                    result.DetectedSourceLanguage = detectedSourceLang;
                    Logger.Debug($"Detected language: {detectedSourceLang}");
                }

                // Skip translation if source and target are the same
                if (detectedSourceLang == targetLanguage)
                {
                    result.Success = true;
                    result.TranslatedText = text;
                    result.SourceLanguage = detectedSourceLang;
                    result.TargetLanguage = targetLanguage;
                    result.Method = "No translation needed";
                    
                    _statistics.RecordTranslation(detectedSourceLang, targetLanguage, text.Length, true);
                    _history.AddTranslation(text, text, detectedSourceLang, targetLanguage);
                    
                    Logger.Info("Translation skipped - same source and target language");
                    return result;
                }

                // Check cache first
                if (_cache.TryGetTranslation(text, detectedSourceLang, targetLanguage, out var cachedEntry))
                {
                    result.Success = true;
                    result.TranslatedText = cachedEntry.TranslatedText;
                    result.SourceLanguage = detectedSourceLang;
                    result.TargetLanguage = targetLanguage;
                    result.Method = "Cache";
                    
                    _statistics.RecordTranslation(detectedSourceLang, targetLanguage, text.Length, true);
                    _history.AddTranslation(text, cachedEntry.TranslatedText, detectedSourceLang, targetLanguage);
                    
                    Logger.Info("Translation served from cache");
                    return result;
                }

                // Perform translation with retry logic
                var maxRetries = config.MaxRetryAttempts;
                Exception lastException = null;

                for (int attempt = 1; attempt <= maxRetries + 1; attempt++)
                {
                    try
                    {
                        if (attempt > 1)
                        {
                            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 2)); // Exponential backoff
                            Logger.Warning($"Translation attempt {attempt} after {delay.TotalSeconds}s delay");
                            await Task.Delay(delay);
                        }

                        // Rate limiting
                        await EnforceRateLimitAsync();

                        string translation;
                        string method;

                        // Try Google Translate first
                        try
                        {
                            translation = await TranslateWithGoogleAsync(text, detectedSourceLang, targetLanguage);
                            method = "Google Translate";
                        }
                        catch (Exception googleEx)
                        {
                            Logger.Warning($"Google Translate failed: {googleEx.Message}");
                            
                            // Fallback to LibreTranslate
                            translation = await TranslateWithLibreTranslateAsync(text, detectedSourceLang, targetLanguage);
                            method = "LibreTranslate (Fallback)";
                        }

                        // Success!
                        result.Success = true;
                        result.TranslatedText = translation;
                        result.SourceLanguage = detectedSourceLang;
                        result.TargetLanguage = targetLanguage;
                        result.Method = method;

                        // Add to cache and history
                        _cache.AddTranslation(text, translation, detectedSourceLang, targetLanguage);
                        _history.AddTranslation(text, translation, detectedSourceLang, targetLanguage);
                        _statistics.RecordTranslation(detectedSourceLang, targetLanguage, text.Length, true);

                        var duration = DateTime.Now - startTime;
                        Logger.Info($"Translation successful via {method} in {duration.TotalMilliseconds:F0}ms (attempt {attempt})");
                        return result;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        Logger.Warning($"Translation attempt {attempt} failed: {ex.Message}");

                        if (attempt <= maxRetries && ErrorHandler.ShouldRetry(ex, attempt, maxRetries))
                        {
                            continue; // Retry
                        }
                        else
                        {
                            break; // Don't retry
                        }
                    }
                }

                // All attempts failed
                result.Success = false;
                result.ErrorMessage = lastException?.Message ?? "Translation failed after all retry attempts";
                _statistics.RecordTranslation(detectedSourceLang, targetLanguage, text.Length, false);
                
                var totalDuration = DateTime.Now - startTime;
                Logger.Error($"Translation failed after {maxRetries + 1} attempts in {totalDuration.TotalSeconds:F1}s", lastException);
                
                ErrorHandler.HandleException(lastException, "Translation", false);
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Unexpected error: {ex.Message}";
                _statistics.RecordTranslation(sourceLanguage, targetLanguage, text?.Length ?? 0, false);
                
                Logger.Error("Unexpected error in TranslateAsync", ex);
                ErrorHandler.HandleException(ex, "Translation");
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
            _cache?.Dispose();
            GC.SuppressFinalize(this);
        }

        public TranslationCache GetCache() => _cache;
        public TranslationHistory GetHistory() => _history;
        public UsageStatistics GetStatistics() => _statistics;
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