using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
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
    [Serializable]
    private class RemoteConfigPayload
    {
        public string apiBaseUrl;
    }

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
    private bool _isApiConfigLoaded;
    private bool _isApiConfigLoading;

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
        APIConfig.InitializeFromCache();
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
        // 인증 토큰은 이후 Save/Load 요청의 Authorization 헤더에 재사용된다.
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
        // 세션 식별값은 대화 로그 등 세션성 요청에 함께 실어 보낸다.
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
        // 백엔드가 내려준 사용자 식별값을 로컬에 보관한다.
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
        // 요청별로 인증 헤더 사용 여부와 로딩 UI 노출 여부를 선택할 수 있다.
        StartCoroutine(GetRoutine(() => url, onSuccess, onError, includeAuthHeader, showLoadingUI));
    }

    public void Get<T>(Func<string> urlFactory, Action<T> onSuccess, Action<string> onError = null, bool includeAuthHeader = false, bool showLoadingUI = false)
    {
        StartCoroutine(GetRoutine(urlFactory, onSuccess, onError, includeAuthHeader, showLoadingUI));
    }

    private IEnumerator GetRoutine<T>(Func<string> urlFactory, Action<T> onSuccess, Action<string> onError, bool includeAuthHeader, bool showLoadingUI)
    {
        yield return EnsureApiConfigLoaded();
        string url = urlFactory != null ? urlFactory.Invoke() : string.Empty;

        // SaveData/GetData처럼 명시적으로 요청한 경우에만 전체 로딩 UI를 켠다.
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
        // POST도 GET과 같은 규칙으로 인증/로딩 UI를 제어한다.
        StartCoroutine(PostRoutine(() => url, requestData, onSuccess, onError, includeAuthHeader, showLoadingUI));
    }

    public void Post<TReq, TRes>(Func<string> urlFactory, TReq requestData, Action<TRes> onSuccess, Action<string> onError = null, bool includeAuthHeader = false, bool showLoadingUI = false)
    {
        StartCoroutine(PostRoutine(urlFactory, requestData, onSuccess, onError, includeAuthHeader, showLoadingUI));
    }

    private IEnumerator PostRoutine<TReq, TRes>(Func<string> urlFactory, TReq requestData, Action<TRes> onSuccess, Action<string> onError, bool includeAuthHeader, bool showLoadingUI)
    {
        yield return EnsureApiConfigLoaded();
        string url = urlFactory != null ? urlFactory.Invoke() : string.Empty;

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
        // 스냅샷 저장/불러오기는 accessToken 기반 인증이 필수다.
        if (!includeAuthHeader || string.IsNullOrWhiteSpace(_accessToken))
        {
            return;
        }

        request.SetRequestHeader("Authorization", $"Bearer {_accessToken}");
    }

    private void BeginNetworkRequest()
    {
        // 동시 요청이 겹칠 수 있으므로 카운트 기반으로 로딩 UI를 관리한다.
        _pendingRequestCount++;
        SetConnectLoadingVisible(true);
    }

    private void EndNetworkRequest()
    {
        _pendingRequestCount = Mathf.Max(0, _pendingRequestCount - 1);

        // 마지막 요청이 끝났을 때만 로딩 UI를 닫는다.
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
        // 콜백 내부 예외가 나더라도 로딩 UI 정리가 끊기지 않게 감싼다.
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

    private IEnumerator EnsureApiConfigLoaded()
    {
        if (_isApiConfigLoaded)
        {
            yield break;
        }

        while (_isApiConfigLoading)
        {
            yield return null;
        }

        if (_isApiConfigLoaded)
        {
            yield break;
        }

        _isApiConfigLoading = true;

        try
        {
            using (UnityWebRequest request = UnityWebRequest.Get(APIConfig.RemoteConfigUrl))
            {
                request.timeout = Mathf.Max(1, requestTimeoutSeconds);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string resolvedBaseUrl = TryResolveRemoteBaseUrl(request.downloadHandler.text);

                    if (!string.IsNullOrWhiteSpace(resolvedBaseUrl))
                    {
                        APIConfig.SetRuntimeBaseUrl(resolvedBaseUrl, persistToCache: true);
                        Debug.Log($"[NetworkManager] Loaded API base URL from remote config: {APIConfig.CurrentBaseUrl}");
                    }
                    else
                    {
                        Debug.LogWarning("[NetworkManager] Remote config did not contain a valid apiBaseUrl. Using cached/default base URL.");
                    }
                }
                else
                {
                    Debug.LogWarning($"[NetworkManager] Failed to load remote API config: {request.error}. Using cached/default base URL: {APIConfig.CurrentBaseUrl}");
                }
            }
        }
        finally
        {
            _isApiConfigLoaded = true;
            _isApiConfigLoading = false;
        }
    }

    private string TryResolveRemoteBaseUrl(string rawConfigText)
    {
        if (string.IsNullOrWhiteSpace(rawConfigText))
        {
            return null;
        }

        string trimmed = rawConfigText.Trim();

        try
        {
            RemoteConfigPayload payload = JsonConvert.DeserializeObject<RemoteConfigPayload>(trimmed);
            if (!string.IsNullOrWhiteSpace(payload?.apiBaseUrl))
            {
                return payload.apiBaseUrl;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[NetworkManager] Failed to parse remote config JSON directly: {exception.Message}");
        }

        Match match = Regex.Match(trimmed, "\"apiBaseUrl\"\\s*:\\s*\"?(?<value>[^\"\\s,}\\]]+)\"?");
        if (match.Success)
        {
            return match.Groups["value"].Value;
        }

        return null;
    }
}
