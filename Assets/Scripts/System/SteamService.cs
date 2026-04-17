using UnityEngine;

#if STEAMWORKS_NET
using Steamworks;
#endif

public class SteamService : MonoBehaviour
{
    public static SteamService Instance { get; private set; }

    [SerializeField] private bool dontDestroyOnLoad = true;

    public bool IsInitialized { get; private set; }
    public string SteamId { get; private set; } = string.Empty;
    public string PersonaName { get; private set; } = string.Empty;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        InitializeSteam();
    }

    private void Update()
    {
#if STEAMWORKS_NET
        if (IsInitialized)
        {
            SteamAPI.RunCallbacks();
        }
#endif
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            ShutdownSteam();
            Instance = null;
        }
    }

    public void RefreshUserData()
    {
#if STEAMWORKS_NET
        if (!IsInitialized)
        {
            SteamId = string.Empty;
            PersonaName = string.Empty;
            return;
        }

        CSteamID steamId = SteamUser.GetSteamID();
        SteamId = steamId.m_SteamID.ToString();
        PersonaName = SteamFriends.GetPersonaName();
#else
        SteamId = string.Empty;
        PersonaName = string.Empty;
#endif
    }

    private void InitializeSteam()
    {
#if STEAMWORKS_NET
        try
        {
            IsInitialized = SteamAPI.Init();

            if (!IsInitialized)
            {
                Debug.LogWarning("SteamAPI.Init failed. Launch the game from Steam or verify the Steamworks.NET setup.");
                return;
            }

            RefreshUserData();
            Debug.Log($"Steam initialized. PersonaName={PersonaName}, SteamId={SteamId}");
        }
        catch (System.Exception ex)
        {
            IsInitialized = false;
            Debug.LogError($"Steam initialization failed: {ex.Message}");
        }
#else
        IsInitialized = false;
        Debug.LogWarning("STEAMWORKS_NET is not defined. Import Steamworks.NET and add the scripting define symbol first.");
#endif
    }

    private void ShutdownSteam()
    {
#if STEAMWORKS_NET
        if (!IsInitialized)
        {
            return;
        }

        SteamAPI.Shutdown();
        IsInitialized = false;
#endif
    }
}
