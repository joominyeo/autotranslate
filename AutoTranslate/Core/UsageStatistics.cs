using System.IO;
using Newtonsoft.Json;

namespace AutoTranslate.Core
{
    public class UsageStatistics
    {
        private readonly string _statsFilePath;
        private readonly object _fileLock = new object();
        private UsageData _data;

        public UsageStatistics()
        {
            var statsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AutoTranslate");
            
            _statsFilePath = Path.Combine(statsDirectory, "usage_statistics.json");
            LoadStatistics();
        }

        public void RecordTranslation(string sourceLanguage, string targetLanguage, int textLength, bool wasSuccessful)
        {
            try
            {
                var today = DateTime.Today;
                
                lock (_data)
                {
                    _data.TotalTranslations++;
                    _data.TotalCharactersTranslated += textLength;
                    _data.LastUsed = DateTime.Now;

                    if (wasSuccessful)
                    {
                        _data.SuccessfulTranslations++;
                    }
                    else
                    {
                        _data.FailedTranslations++;
                    }

                    // Daily statistics
                    if (!_data.DailyStats.ContainsKey(today))
                    {
                        _data.DailyStats[today] = new DailyUsage();
                    }
                    
                    var dailyStats = _data.DailyStats[today];
                    dailyStats.TranslationCount++;
                    dailyStats.CharactersTranslated += textLength;

                    // Language pair statistics
                    var languagePair = $"{sourceLanguage}->{targetLanguage}";
                    if (!_data.LanguagePairStats.ContainsKey(languagePair))
                    {
                        _data.LanguagePairStats[languagePair] = 0;
                    }
                    _data.LanguagePairStats[languagePair]++;

                    // Cleanup old daily stats (keep only last 90 days)
                    var cutoffDate = DateTime.Today.AddDays(-90);
                    var oldKeys = _data.DailyStats.Keys.Where(date => date < cutoffDate).ToList();
                    foreach (var key in oldKeys)
                    {
                        _data.DailyStats.Remove(key);
                    }
                }

                Task.Run(SaveStatistics);
            }
            catch (Exception ex)
            {
                Logger.Error("Error recording usage statistics", ex);
            }
        }

        public void RecordApplicationStart()
        {
            try
            {
                lock (_data)
                {
                    _data.TotalApplicationStarts++;
                    _data.LastUsed = DateTime.Now;
                    
                    if (_data.FirstUsed == DateTime.MinValue)
                    {
                        _data.FirstUsed = DateTime.Now;
                    }
                }

                Task.Run(SaveStatistics);
            }
            catch (Exception ex)
            {
                Logger.Error("Error recording application start", ex);
            }
        }

        public UsageData GetStatistics()
        {
            lock (_data)
            {
                return new UsageData
                {
                    TotalTranslations = _data.TotalTranslations,
                    SuccessfulTranslations = _data.SuccessfulTranslations,
                    FailedTranslations = _data.FailedTranslations,
                    TotalCharactersTranslated = _data.TotalCharactersTranslated,
                    TotalApplicationStarts = _data.TotalApplicationStarts,
                    FirstUsed = _data.FirstUsed,
                    LastUsed = _data.LastUsed,
                    DailyStats = new Dictionary<DateTime, DailyUsage>(_data.DailyStats),
                    LanguagePairStats = new Dictionary<string, int>(_data.LanguagePairStats)
                };
            }
        }

        public int GetTranslationsToday()
        {
            var today = DateTime.Today;
            lock (_data)
            {
                return _data.DailyStats.ContainsKey(today) ? _data.DailyStats[today].TranslationCount : 0;
            }
        }

        public int GetTranslationsThisWeek()
        {
            var weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            lock (_data)
            {
                return _data.DailyStats
                    .Where(kvp => kvp.Key >= weekStart)
                    .Sum(kvp => kvp.Value.TranslationCount);
            }
        }

        public int GetTranslationsThisMonth()
        {
            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            lock (_data)
            {
                return _data.DailyStats
                    .Where(kvp => kvp.Key >= monthStart)
                    .Sum(kvp => kvp.Value.TranslationCount);
            }
        }

        public double GetSuccessRate()
        {
            lock (_data)
            {
                var total = _data.TotalTranslations;
                return total > 0 ? (double)_data.SuccessfulTranslations / total * 100 : 0;
            }
        }

        public string GetMostUsedLanguagePair()
        {
            lock (_data)
            {
                if (_data.LanguagePairStats.Count == 0) return "None";
                
                return _data.LanguagePairStats
                    .OrderByDescending(kvp => kvp.Value)
                    .First()
                    .Key;
            }
        }

        public TimeSpan GetUsageDuration()
        {
            lock (_data)
            {
                if (_data.FirstUsed == DateTime.MinValue) return TimeSpan.Zero;
                return DateTime.Now - _data.FirstUsed;
            }
        }

        private void LoadStatistics()
        {
            try
            {
                lock (_fileLock)
                {
                    if (File.Exists(_statsFilePath))
                    {
                        var json = File.ReadAllText(_statsFilePath);
                        _data = JsonConvert.DeserializeObject<UsageData>(json) ?? new UsageData();
                    }
                    else
                    {
                        _data = new UsageData();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading usage statistics", ex);
                _data = new UsageData();
            }
        }

        private void SaveStatistics()
        {
            try
            {
                lock (_fileLock)
                {
                    UsageData dataToSave;
                    lock (_data)
                    {
                        dataToSave = GetStatistics(); // Creates a copy
                    }
                    
                    var json = JsonConvert.SerializeObject(dataToSave, Formatting.Indented);
                    File.WriteAllText(_statsFilePath, json);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving usage statistics", ex);
            }
        }

        public void ResetStatistics()
        {
            try
            {
                lock (_data)
                {
                    _data = new UsageData();
                }
                SaveStatistics();
                Logger.Info("Usage statistics reset");
            }
            catch (Exception ex)
            {
                Logger.Error("Error resetting statistics", ex);
            }
        }
    }

    public class UsageData
    {
        public int TotalTranslations { get; set; }
        public int SuccessfulTranslations { get; set; }
        public int FailedTranslations { get; set; }
        public long TotalCharactersTranslated { get; set; }
        public int TotalApplicationStarts { get; set; }
        public DateTime FirstUsed { get; set; }
        public DateTime LastUsed { get; set; }
        public Dictionary<DateTime, DailyUsage> DailyStats { get; set; } = new();
        public Dictionary<string, int> LanguagePairStats { get; set; } = new();
    }

    public class DailyUsage
    {
        public int TranslationCount { get; set; }
        public long CharactersTranslated { get; set; }
    }
}