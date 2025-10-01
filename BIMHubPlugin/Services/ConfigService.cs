using System;
using System.IO;
using Newtonsoft.Json;

namespace BIMHubPlugin.Services
{
    public class PluginConfig
    {
        public string ApiBaseUrl { get; set; } = "http://bimhub.kazgor.kz:5058/api";
        public string ApiToken { get; set; } = "";
        public int CacheSizeMB { get; set; } = 500;
        public int RequestTimeoutSeconds { get; set; } = 300;
        public int DefaultPageSize { get; set; } = 12;
        public bool EnableLogging { get; set; } = true;
        public string CacheFolder { get; set; } = "";
        public string LogFolder { get; set; } = "";
        public string Language { get; set; } = "ru";
        public bool CheckForUpdates { get; set; } = true;
        public int CacheTTLDays { get; set; } = 7;
    }

    public static class ConfigService
    {
        private static readonly string ConfigFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BIMHubPlugin"
        );

        private static readonly string ConfigPath = Path.Combine(ConfigFolder, "config.json");
        private static PluginConfig _cachedConfig;
        private static readonly object _lockObject = new object();

        public static PluginConfig LoadConfig()
        {
            lock (_lockObject)
            {
                if (_cachedConfig != null)
                    return _cachedConfig;

                try
                {
                    if (!Directory.Exists(ConfigFolder))
                    {
                        Directory.CreateDirectory(ConfigFolder);
                    }

                    if (File.Exists(ConfigPath))
                    {
                        string json = File.ReadAllText(ConfigPath);
                        _cachedConfig = JsonConvert.DeserializeObject<PluginConfig>(json);
                        ValidateConfig(_cachedConfig);
                        return _cachedConfig;
                    }
                    else
                    {
                        _cachedConfig = CreateDefaultConfig();
                        SaveConfig(_cachedConfig);
                        return _cachedConfig;
                    }
                }
                catch
                {
                    _cachedConfig = CreateDefaultConfig();
                    try
                    {
                        SaveConfig(_cachedConfig);
                    }
                    catch { }
                    return _cachedConfig;
                }
            }
        }

        public static void SaveConfig(PluginConfig config)
        {
            lock (_lockObject)
            {
                try
                {
                    if (config == null)
                        throw new ArgumentNullException(nameof(config));

                    ValidateConfig(config);

                    if (!Directory.Exists(ConfigFolder))
                    {
                        Directory.CreateDirectory(ConfigFolder);
                    }

                    string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                    File.WriteAllText(ConfigPath, json);

                    _cachedConfig = config;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Ошибка сохранения конфигурации: {ex.Message}", ex);
                }
            }
        }

        public static string GetCacheFolder()
        {
            var config = LoadConfig();
            
            if (!string.IsNullOrWhiteSpace(config.CacheFolder))
            {
                return config.CacheFolder;
            }

            return Path.Combine(ConfigFolder, "Cache");
        }

        public static string GetLogFolder()
        {
            var config = LoadConfig();
            
            if (!string.IsNullOrWhiteSpace(config.LogFolder))
            {
                return config.LogFolder;
            }

            return Path.Combine(ConfigFolder, "Logs");
        }

        private static PluginConfig CreateDefaultConfig()
        {
            return new PluginConfig
            {
                ApiBaseUrl = "https://localhost:7001",
                ApiToken = "",
                CacheSizeMB = 500,
                RequestTimeoutSeconds = 300,
                DefaultPageSize = 12,
                EnableLogging = true,
                CacheFolder = "",
                LogFolder = "",
                Language = "ru",
                CheckForUpdates = true,
                CacheTTLDays = 7
            };
        }

        private static void ValidateConfig(PluginConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (string.IsNullOrWhiteSpace(config.ApiBaseUrl))
            {
                config.ApiBaseUrl = "https://localhost:7001";
            }

            config.ApiBaseUrl = config.ApiBaseUrl.TrimEnd('/');

            if (config.CacheSizeMB <= 0)
            {
                config.CacheSizeMB = 500;
            }

            if (config.CacheSizeMB > 10240)
            {
                config.CacheSizeMB = 10240;
            }

            if (config.RequestTimeoutSeconds <= 0)
            {
                config.RequestTimeoutSeconds = 300;
            }

            if (config.DefaultPageSize <= 0)
            {
                config.DefaultPageSize = 12;
            }

            if (config.DefaultPageSize > 100)
            {
                config.DefaultPageSize = 100;
            }

            if (config.CacheTTLDays <= 0)
            {
                config.CacheTTLDays = 7;
            }

            if (string.IsNullOrWhiteSpace(config.Language))
            {
                config.Language = "ru";
            }
        }
    }
}