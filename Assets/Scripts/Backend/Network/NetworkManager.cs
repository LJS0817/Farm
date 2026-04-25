using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

[Serializable]
public class PlayerId
{
    public string userId = "Unity";
    public string sessionId = "SESSION-732d990b";
    public string issuedKey = "UNITY-TEST-001";
}

[Serializable]
public class ServerResponse
{
    public string id;
    public string message;
}

public class NetworkManager : MonoBehaviour
{
    public GameObject connectLoadingUI;
    [SerializeField] private int requestTimeoutSeconds = 15;
    private const string AccessTokenPlayerPrefsKey = "backend.accessToken";
    private const string SessionIdPlayerPrefsKey = "backend.sessionId";
    private const string UserIdPlayerPrefsKey = "backend.userId";

    // 1. 어디서든 접근 가능한 싱글톤 인스턴스
    private static NetworkManager _instance;
    public static NetworkManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // 인스턴스가 없으면 새로 생성하고 파괴되지 않도록 설정
                GameObject go = new GameObject("NetworkManager");
                _instance = go.AddComponent<NetworkManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    PlayerId _playerId;
    private string _accessToken = string.Empty;
    private int _pendingRequestCount;

    private void Awake()
    {
        // 씬 이동 시 중복 생성 방지
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        _instance = this;
        _playerId = new PlayerId();
        _accessToken = PlayerPrefs.GetString(AccessTokenPlayerPrefsKey, string.Empty);
        _playerId.userId = PlayerPrefs.GetString(UserIdPlayerPrefsKey, ResolveDefaultUserId());
        _playerId.sessionId = PlayerPrefs.GetString(SessionIdPlayerPrefsKey, _playerId.sessionId);
        SetConnectLoadingVisible(false);
        DontDestroyOnLoad(this.gameObject);
    }

    public PlayerId GetPlayerId() { return _playerId; }
    public string GetAccessToken() { return _accessToken; }

    public void SetAccessToken(string accessToken)
    {
        _accessToken = accessToken ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_accessToken))
        {
            PlayerPrefs.DeleteKey(AccessTokenPlayerPrefsKey);
        }
        else
        {
            PlayerPrefs.SetString(AccessTokenPlayerPrefsKey, _accessToken);
        }

        PlayerPrefs.Save();
    }

    public void SetSessionId(string sessionId)
    {
        _playerId.sessionId = string.IsNullOrWhiteSpace(sessionId) ? string.Empty : sessionId.Trim();

        if (string.IsNullOrWhiteSpace(_playerId.sessionId))
        {
            PlayerPrefs.DeleteKey(SessionIdPlayerPrefsKey);
        }
        else
        {
            PlayerPrefs.SetString(SessionIdPlayerPrefsKey, _playerId.sessionId);
        }

        PlayerPrefs.Save();
    }

    public void SetUserId(string userId)
    {
        _playerId.userId = string.IsNullOrWhiteSpace(userId) ? ResolveDefaultUserId() : userId.Trim();

        if (string.IsNullOrWhiteSpace(_playerId.userId))
        {
            PlayerPrefs.DeleteKey(UserIdPlayerPrefsKey);
        }
        else
        {
            PlayerPrefs.SetString(UserIdPlayerPrefsKey, _playerId.userId);
        }

        PlayerPrefs.Save();
    }

    private string ResolveDefaultUserId()
    {
#if UNITY_EDITOR
        return "Unity";
#else
        return "Unity";
#endif
    }

    // -------------------------------------------------------------
    // GET 요청 (T: 서버로부터 받을 응답 클래스 타입)
    // -------------------------------------------------------------
    public void Get<T>(string url, Action<T> onSuccess, Action<string> onError = null, bool includeAuthHeader = false, bool showLoadingUI = false)
    {
        StartCoroutine(GetRoutine(url, onSuccess, onError, includeAuthHeader, showLoadingUI));
    }

    private IEnumerator GetRoutine<T>(string url, Action<T> onSuccess, Action<string> onError, bool includeAuthHeader, bool showLoadingUI)
    {
        if (showLoadingUI)
        {
            BeginNetworkRequest();
        }

        try
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = Mathf.Max(1, requestTimeoutSeconds);
                AddAuthorizationHeaderIfNeeded(request, includeAuthHeader);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    TryInvokeErrorCallback(onError, request.error);
                }
                else
                {
                    try
                    {
                        T resultData = JsonConvert.DeserializeObject<T>(request.downloadHandler.text);
                        TryInvokeSuccessCallback(onSuccess, resultData, onError);
                    }
                    catch (Exception e)
                    {
                        TryInvokeErrorCallback(onError, $"JSON 파싱 에러: {e.Message}\n원본 데이터: {request.downloadHandler.text}");
                    }
                }
            }
        }
        finally
        {
            if (showLoadingUI)
            {
                EndNetworkRequest();
            }
        }
    }

    // -------------------------------------------------------------
    // POST 요청 (TReq: 보낼 데이터 타입, TRes: 받을 데이터 타입)
    // -------------------------------------------------------------
    public void Post<TReq, TRes>(string url, TReq requestData, Action<TRes> onSuccess, Action<string> onError = null, bool includeAuthHeader = false, bool showLoadingUI = false)
    {
        StartCoroutine(PostRoutine(url, requestData, onSuccess, onError, includeAuthHeader, showLoadingUI));
    }

    private IEnumerator PostRoutine<TReq, TRes>(string url, TReq requestData, Action<TRes> onSuccess, Action<string> onError, bool includeAuthHeader, bool showLoadingUI)
    {
        // C# 객체를 JSON 문자열로 자동 직렬화
        string jsonData = JsonConvert.SerializeObject(requestData);
        if (showLoadingUI)
        {
            BeginNetworkRequest();
        }

        try
        {
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = Mathf.Max(1, requestTimeoutSeconds);
                request.SetRequestHeader("Content-Type", "application/json");
                AddAuthorizationHeaderIfNeeded(request, includeAuthHeader);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    TryInvokeErrorCallback(onError, request.error);
                }
                else
                {
                    try
                    {
                        TRes resultData = JsonConvert.DeserializeObject<TRes>(request.downloadHandler.text);
                        TryInvokeSuccessCallback(onSuccess, resultData, onError);
                    }
                    catch (Exception e)
                    {
                        TryInvokeErrorCallback(onError, $"JSON 파싱 에러: {e.Message}\n원본 데이터: {request.downloadHandler.text}");
                    }
                }
            }
        }
        finally
        {
            if (showLoadingUI)
            {
                EndNetworkRequest();
            }
        }
    }

    private void AddAuthorizationHeaderIfNeeded(UnityWebRequest request, bool includeAuthHeader)
    {
        if (!includeAuthHeader || string.IsNullOrWhiteSpace(_accessToken))
        {
            return;
        }

        request.SetRequestHeader("Authorization", $"Bearer {_accessToken}");
    }

    private void BeginNetworkRequest()
    {
        _pendingRequestCount++;
        SetConnectLoadingVisible(true);
    }

    private void EndNetworkRequest()
    {
        _pendingRequestCount = Mathf.Max(0, _pendingRequestCount - 1);

        if (_pendingRequestCount == 0)
        {
            SetConnectLoadingVisible(false);
        }
    }

    private void SetConnectLoadingVisible(bool isVisible)
    {
        if (connectLoadingUI == null)
        {
            return;
        }

        connectLoadingUI.SetActive(isVisible);
    }

    private void TryInvokeErrorCallback(Action<string> onError, string errorMessage)
    {
        try
        {
            onError?.Invoke(errorMessage);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[NetworkManager] Error callback threw an exception: {exception}");
        }
    }

    private void TryInvokeSuccessCallback<T>(Action<T> onSuccess, T resultData, Action<string> onError)
    {
        try
        {
            onSuccess?.Invoke(resultData);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[NetworkManager] Success callback threw an exception: {exception}");
            TryInvokeErrorCallback(onError, $"Success callback exception: {exception.Message}");
        }
    }
}
