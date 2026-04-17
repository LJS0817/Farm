using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

[Serializable]
public class PlayerId
{
    public string userId = "USR-7bf68c13";
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
        DontDestroyOnLoad(this.gameObject);
    }

    public PlayerId GetPlayerId() { return _playerId; }

    // -------------------------------------------------------------
    // GET 요청 (T: 서버로부터 받을 응답 클래스 타입)
    // -------------------------------------------------------------
    public void Get<T>(string url, Action<T> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(GetRoutine(url, onSuccess, onError));
    }

    private IEnumerator GetRoutine<T>(string url, Action<T> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                onError?.Invoke(request.error);
            }
            else
            {
                try
                {
                    // 응답받은 JSON 텍스트를 바로 C# 객체로 변환하여 콜백 반환
                    T resultData = JsonConvert.DeserializeObject<T>(request.downloadHandler.text);
                    onSuccess?.Invoke(resultData);
                }
                catch (Exception e)
                {
                    onError?.Invoke($"JSON 파싱 에러: {e.Message}\n원본 데이터: {request.downloadHandler.text}");
                }
            }
        }
    }

    // -------------------------------------------------------------
    // POST 요청 (TReq: 보낼 데이터 타입, TRes: 받을 데이터 타입)
    // -------------------------------------------------------------
    public void Post<TReq, TRes>(string url, TReq requestData, Action<TRes> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(PostRoutine(url, requestData, onSuccess, onError));
    }

    private IEnumerator PostRoutine<TReq, TRes>(string url, TReq requestData, Action<TRes> onSuccess, Action<string> onError)
    {
        // C# 객체를 JSON 문자열로 자동 직렬화
        string jsonData = JsonConvert.SerializeObject(requestData);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                onError?.Invoke(request.error);
            }
            else
            {
                try
                {
                    // 응답 데이터를 다시 C# 객체로 역직렬화
                    TRes resultData = JsonConvert.DeserializeObject<TRes>(request.downloadHandler.text);
                    onSuccess?.Invoke(resultData);
                }
                catch (Exception e)
                {
                    onError?.Invoke($"JSON 파싱 에러: {e.Message}\n원본 데이터: {request.downloadHandler.text}");
                }
            }
        }
    }
}