using System;
using System.Text;
using UnityEngine;

#if STEAMWORKS_NET
using Steamworks;
#endif

public class SteamService : MonoBehaviour
{
    private const string DefaultWebApiIdentity = "farmverse";

    public static SteamService Instance { get; private set; }

    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool requestWebApiTicketOnInitialize = true;
    [SerializeField] private bool loginToBackendOnTicketReceived = true;
    [SerializeField] private string webApiIdentity = DefaultWebApiIdentity;

    public bool IsInitialized { get; private set; }
    public string SteamId { get; private set; } = string.Empty;
    public string PersonaName { get; private set; } = string.Empty;
    public string LastWebApiTicketHex { get; private set; } = string.Empty;
    public bool IsSteamRuntimeAvailable
    {
        get
        {
#if UNITY_EDITOR
            return false;
#else
            return true;
#endif
        }
    }

    public event Action<string> WebApiTicketReceived;
    public event Action<string> WebApiTicketRequestFailed;

#if STEAMWORKS_NET
    private Callback<GetTicketForWebApiResponse_t> webApiTicketResponseCallback;
    private HAuthTicket activeWebApiTicket = HAuthTicket.Invalid;
    private string pendingWebApiIdentity = DefaultWebApiIdentity;
#endif

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
            CancelWebApiTicket();
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

    public bool RequestWebApiTicket(string identity = DefaultWebApiIdentity)
    {
#if STEAMWORKS_NET
        if (!IsInitialized)
        {
            const string errorMessage = "Steam is not initialized, so a Web API ticket cannot be requested.";
            Debug.LogWarning(errorMessage);
            WebApiTicketRequestFailed?.Invoke(errorMessage);
            return false;
        }

        string resolvedIdentity = string.IsNullOrWhiteSpace(identity)
            ? DefaultWebApiIdentity
            : identity.Trim();

        CancelWebApiTicket();

        pendingWebApiIdentity = resolvedIdentity;
        LastWebApiTicketHex = string.Empty;
        activeWebApiTicket = SteamUser.GetAuthTicketForWebApi(resolvedIdentity);

        if (activeWebApiTicket == HAuthTicket.Invalid)
        {
            string errorMessage = $"Failed to request Steam Web API ticket for identity '{resolvedIdentity}'.";
            Debug.LogError(errorMessage);
            WebApiTicketRequestFailed?.Invoke(errorMessage);
            return false;
        }

        Debug.Log($"Requested Steam Web API ticket. identity={resolvedIdentity}, handle={activeWebApiTicket}");
        return true;
#else
        const string errorMessage = "STEAMWORKS_NET is not defined, so a Web API ticket cannot be requested.";
        Debug.LogWarning(errorMessage);
        WebApiTicketRequestFailed?.Invoke(errorMessage);
        return false;
#endif
    }

    public void CancelWebApiTicket()
    {
#if STEAMWORKS_NET
        if (activeWebApiTicket != HAuthTicket.Invalid)
        {
            SteamUser.CancelAuthTicket(activeWebApiTicket);
            activeWebApiTicket = HAuthTicket.Invalid;
        }
#endif

        LastWebApiTicketHex = string.Empty;
    }

    private void InitializeSteam()
    {
#if UNITY_EDITOR
        IsInitialized = false;
        SteamId = string.Empty;
        PersonaName = string.Empty;
        LastWebApiTicketHex = string.Empty;
        Debug.Log("Steam initialization skipped in the Unity Editor. Use a Steam build to test Steam login.");
#elif STEAMWORKS_NET
        try
        {
            IsInitialized = SteamAPI.Init();

            if (!IsInitialized)
            {
                Debug.LogWarning("SteamAPI.Init failed. Launch the game from Steam or verify the Steamworks.NET setup.");
                return;
            }

            webApiTicketResponseCallback = Callback<GetTicketForWebApiResponse_t>.Create(OnGetTicketForWebApiResponse);
            RefreshUserData();
            Debug.Log($"Steam initialized. PersonaName={PersonaName}, SteamId={SteamId}");

            if (requestWebApiTicketOnInitialize)
            {
                RequestWebApiTicket(webApiIdentity);
            }
        }
        catch (Exception ex)
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

#if STEAMWORKS_NET
    private void OnGetTicketForWebApiResponse(GetTicketForWebApiResponse_t callback)
    {
        if (callback.m_hAuthTicket != activeWebApiTicket)
        {
            return;
        }

        if (callback.m_eResult != EResult.k_EResultOK)
        {
            string errorMessage = $"Steam Web API ticket request failed. Result={callback.m_eResult}, identity={pendingWebApiIdentity}";
            Debug.LogError(errorMessage);
            activeWebApiTicket = HAuthTicket.Invalid;
            LastWebApiTicketHex = string.Empty;
            WebApiTicketRequestFailed?.Invoke(errorMessage);
            return;
        }

        if (callback.m_rgubTicket == null || callback.m_cubTicket <= 0)
        {
            string errorMessage = $"Steam Web API ticket callback returned no ticket bytes. identity={pendingWebApiIdentity}";
            Debug.LogError(errorMessage);
            activeWebApiTicket = HAuthTicket.Invalid;
            LastWebApiTicketHex = string.Empty;
            WebApiTicketRequestFailed?.Invoke(errorMessage);
            return;
        }

        LastWebApiTicketHex = ConvertBytesToHex(callback.m_rgubTicket, callback.m_cubTicket);
        Debug.Log($"Steam Web API ticket received. identity={pendingWebApiIdentity}, byteLength={callback.m_cubTicket}");
        WebApiTicketReceived?.Invoke(LastWebApiTicketHex);

        if (loginToBackendOnTicketReceived)
        {
            LoginToBackendWithSteamTicket(LastWebApiTicketHex, pendingWebApiIdentity);
        }
    }

    private static string ConvertBytesToHex(byte[] bytes, int length)
    {
        if (bytes == null || length <= 0)
        {
            return string.Empty;
        }

        int safeLength = Mathf.Min(length, bytes.Length);
        StringBuilder builder = new StringBuilder(safeLength * 2);

        for (int i = 0; i < safeLength; i++)
        {
            builder.Append(bytes[i].ToString("x2"));
        }

        return builder.ToString();
    }

    private void LoginToBackendWithSteamTicket(string ticketHex, string identity)
    {
        if (string.IsNullOrWhiteSpace(ticketHex))
        {
            Debug.LogError("Steam Web API ticket is empty, so the backend login request was skipped.");
            return;
        }

        SteamAuthRequest request = new SteamAuthRequest
        {
            ticket = ticketHex,
            identity = string.IsNullOrWhiteSpace(identity) ? DefaultWebApiIdentity : identity,
            personaName = PersonaName
        };

        APIController.Auth.LoginWithSteam(
            request,
            onSuccess: response =>
            {
                if (response == null)
                {
                    Debug.LogError("Steam backend login returned a null response.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(response.accessToken))
                {
                    Debug.LogError("Steam backend login succeeded but accessToken was empty.");
                    return;
                }

                NetworkManager.Instance.SetAccessToken(response.accessToken);
                Debug.Log(
                    $"Steam backend login success. appUserId={response.appUserId}, steamId={response.steamId}, displayName={response.displayName}");
            },
            onError: error =>
            {
                Debug.LogError($"Steam backend login failed: {error}");
            });
    }
#endif
}
