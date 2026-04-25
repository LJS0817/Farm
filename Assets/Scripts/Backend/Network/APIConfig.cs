using UnityEngine;

public static class APIConfig
{
    private const string DefaultBaseUrl = "http://43.200.182.44/api/v1";
    private const string RemoteBaseUrlConfigUrl = "https://raw.githubusercontent.com/carbuncle3203/farmverse-runtime-config/refs/heads/main/farmverse-config.json";
    private const string CachedBaseUrlPlayerPrefsKey = "backend.cachedBaseUrl";

    private static string _runtimeBaseUrl;
    private static bool _isInitialized;

    public static string RemoteConfigUrl => RemoteBaseUrlConfigUrl;

    public static string CurrentBaseUrl
    {
        get
        {
            EnsureInitialized();
            return _runtimeBaseUrl;
        }
    }

    public static void InitializeFromCache()
    {
        if (_isInitialized)
        {
            return;
        }

        string cachedBaseUrl = PlayerPrefs.GetString(CachedBaseUrlPlayerPrefsKey, DefaultBaseUrl);
        _runtimeBaseUrl = NormalizeBaseUrl(cachedBaseUrl);
        _isInitialized = true;
    }

    public static void SetRuntimeBaseUrl(string baseUrl, bool persistToCache)
    {
        _runtimeBaseUrl = NormalizeBaseUrl(baseUrl);
        _isInitialized = true;

        if (!persistToCache)
        {
            return;
        }

        PlayerPrefs.SetString(CachedBaseUrlPlayerPrefsKey, _runtimeBaseUrl);
        PlayerPrefs.Save();
    }

    private static void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        InitializeFromCache();
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        string normalized = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl.Trim();
        normalized = normalized.Trim('"').TrimEnd('/');

        if (!normalized.Contains("://"))
        {
            normalized = $"http://{normalized}";
        }

        if (!normalized.EndsWith("/api/v1"))
        {
            normalized = $"{normalized}/api/v1";
        }

        return normalized;
    }

    public static class User
    {
        public static string Login => $"{CurrentBaseUrl}/game/auth/test-login";
        public static string SteamLogin => $"{CurrentBaseUrl}/game/auth/steam";
    }

    public static class LLM
    {
        public static string SendChatLog => $"{CurrentBaseUrl}/game/conversations";
    }

    public static class Health
    {
        public static string Check => $"{CurrentBaseUrl}/health";
    }

    public static class Game
    {
        public static string Snapshots => $"{CurrentBaseUrl}/game/snapshots";
        public static string LatestSnapshot => $"{CurrentBaseUrl}/game/snapshots/latest";
    }
}
