using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace AutoTranslate.Core
{
    public class TranslationCacheEntry
    {
        public string OriginalText { get; set; } = string.Empty;
        public string TranslatedText { get; set; } = string.Empty;
        public string SourceLanguage { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
        public DateTime CachedAt { get; set; }
        public DateTime LastAccessed { get; set; }
        public int AccessCount { get; set; } = 1;
    }

    public class TranslationCache
    {
        private readonly ConcurrentDictionary<string, TranslationCacheEntry> _cache = new();
        private readonly string _cacheFilePath;
        private readonly Timer _cleanupTimer;
        private readonly object _fileLock = new object();
        private readonly ConfigurationManager _configManager;

        public TranslationCache()
        {
            _configManager = new ConfigurationManager();
            var cacheDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AutoTranslate");
            
            _cacheFilePath = Path.Combine(cacheDirectory, "translation_cache.json");
            
            LoadCache();
            
            // Setup cleanup timer to run every hour
            _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
            
            Logger.Info($"Translation cache initialized with {_cache.Count} entries");
        }

        public bool TryGetTranslation(string text, string sourceLanguage, string targetLanguage, out TranslationCacheEntry entry)
        {
            entry = null;
            
            try
            {
                var config = _configManager.LoadConfiguration();
                if (!config.EnableTextCaching)
                {
                    return false;
                }

                var key = GenerateCacheKey(text, sourceLanguage, targetLanguage);
                
                if (_cache.TryGetValue(key, out entry))
                {
                    // Check if entry is still valid
                    var cacheExpireMinutes = config.CacheExpireMinutes;
                    if (DateTime.Now - entry.CachedAt > TimeSpan.FromMinutes(cacheExpireMinutes))
                    {
                        _cache.TryRemove(key, out _);
                        entry = null;
                        return false;
                    }

                    // Update access statistics
                    entry.LastAccessed = DateTime.Now;
                    entry.AccessCount++;
                    
                    Logger.Debug($"Cache hit for translation: {text.Substring(0, Math.Min(50, text.Length))}...");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error retrieving from translation cache", ex);
            }

            return false;
        }

        public void AddTranslation(string originalText, string translatedText, string sourceLanguage, string targetLanguage)
        {
            try
            {
                var config = _configManager.LoadConfiguration();
                if (!config.EnableTextCaching)
                {
                    return;
                }

                var key = GenerateCacheKey(originalText, sourceLanguage, targetLanguage);
                var entry = new TranslationCacheEntry
                {
                    OriginalText = originalText,
                    TranslatedText = translatedText,
                    SourceLanguage = sourceLanguage,
                    TargetLanguage = targetLanguage,
                    CachedAt = DateTime.Now,
                    LastAccessed = DateTime.Now
                };

                _cache.AddOrUpdate(key, entry, (k, oldEntry) => entry);
                
                Logger.Debug($"Added translation to cache: {originalText.Substring(0, Math.Min(50, originalText.Length))}...");
                
                // Save cache periodically (every 10 new entries)
                if (_cache.Count % 10 == 0)
                {
                    Task.Run(SaveCache);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error adding translation to cache", ex);
            }
        }

        private string GenerateCacheKey(string text, string sourceLanguage, string targetLanguage)
        {
            var input = $"{text}|{sourceLanguage}|{targetLanguage}";
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hash);
        }

        private void LoadCache()
        {
            try
            {
                lock (_fileLock)
                {
                    if (File.Exists(_cacheFilePath))
                    {
                        var json = File.ReadAllText(_cacheFilePath);
                        var entries = JsonConvert.DeserializeObject<Dictionary<string, TranslationCacheEntry>>(json);
                        
                        if (entries != null)
                        {
                            foreach (var kvp in entries)
                            {
                                _cache.TryAdd(kvp.Key, kvp.Value);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading translation cache", ex);
            }
        }

        private void SaveCache()
        {
            try
            {
                lock (_fileLock)
                {
                    var cacheData = _cache.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    var json = JsonConvert.SerializeObject(cacheData, Formatting.Indented);
                    File.WriteAllText(_cacheFilePath, json);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving translation cache", ex);
            }
        }

        private void CleanupExpiredEntries(object state)
        {
            try
            {
                var config = _configManager.LoadConfiguration();
                var expireMinutes = config.CacheExpireMinutes;
                var cutoffTime = DateTime.Now.AddMinutes(-expireMinutes);
                
                var expiredKeys = _cache
                    .Where(kvp => kvp.Value.CachedAt < cutoffTime)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _cache.TryRemove(key, out _);
                }

                if (expiredKeys.Count > 0)
                {
                    Logger.Info($"Cleaned up {expiredKeys.Count} expired cache entries");
                    Task.Run(SaveCache);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error during cache cleanup", ex);
            }
        }

        public void ClearCache()
        {
            try
            {
                _cache.Clear();
                SaveCache();
                Logger.Info("Translation cache cleared");
            }
            catch (Exception ex)
            {
                Logger.Error("Error clearing cache", ex);
            }
        }

        public int GetCacheSize() => _cache.Count;

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            SaveCache();
        }
    }

    public class TranslationHistory
    {
        private readonly List<TranslationHistoryEntry> _history = new();
        private readonly string _historyFilePath;
        private readonly object _fileLock = new object();
        private const int MaxHistoryEntries = 1000;

        public TranslationHistory()
        {
            var historyDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AutoTranslate");
            
            _historyFilePath = Path.Combine(historyDirectory, "translation_history.json");
            LoadHistory();
        }

        public void AddTranslation(string originalText, string translatedText, string sourceLanguage, string targetLanguage)
        {
            try
            {
                var entry = new TranslationHistoryEntry
                {
                    OriginalText = originalText,
                    TranslatedText = translatedText,
                    SourceLanguage = sourceLanguage,
                    TargetLanguage = targetLanguage,
                    Timestamp = DateTime.Now
                };

                lock (_history)
                {
                    _history.Insert(0, entry); // Add to beginning for most recent first
                    
                    // Keep only the most recent entries
                    if (_history.Count > MaxHistoryEntries)
                    {
                        _history.RemoveRange(MaxHistoryEntries, _history.Count - MaxHistoryEntries);
                    }
                }

                Task.Run(SaveHistory);
            }
            catch (Exception ex)
            {
                Logger.Error("Error adding to translation history", ex);
            }
        }

        public List<TranslationHistoryEntry> GetRecentTranslations(int count = 50)
        {
            lock (_history)
            {
                return _history.Take(count).ToList();
            }
        }

        public void ClearHistory()
        {
            try
            {
                lock (_history)
                {
                    _history.Clear();
                }
                SaveHistory();
                Logger.Info("Translation history cleared");
            }
            catch (Exception ex)
            {
                Logger.Error("Error clearing history", ex);
            }
        }

        private void LoadHistory()
        {
            try
            {
                lock (_fileLock)
                {
                    if (File.Exists(_historyFilePath))
                    {
                        var json = File.ReadAllText(_historyFilePath);
                        var entries = JsonConvert.DeserializeObject<List<TranslationHistoryEntry>>(json);
                        
                        if (entries != null)
                        {
                            lock (_history)
                            {
                                _history.Clear();
                                _history.AddRange(entries);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading translation history", ex);
            }
        }

        private void SaveHistory()
        {
            try
            {
                lock (_fileLock)
                {
                    List<TranslationHistoryEntry> historyToSave;
                    lock (_history)
                    {
                        historyToSave = new List<TranslationHistoryEntry>(_history);
                    }
                    
                    var json = JsonConvert.SerializeObject(historyToSave, Formatting.Indented);
                    File.WriteAllText(_historyFilePath, json);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving translation history", ex);
            }
        }
    }

    public class TranslationHistoryEntry
    {
        public string OriginalText { get; set; } = string.Empty;
        public string TranslatedText { get; set; } = string.Empty;
        public string SourceLanguage { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}