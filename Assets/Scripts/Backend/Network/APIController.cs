using Newtonsoft.Json;
using System;
using UnityEngine;

public static class APIController
{
    public static class Auth
    {
        public static void LoginWithSteam(
            SteamAuthRequest request,
            Action<SteamAuthResponse> onSuccess,
            Action<string> onError = null)
        {
            // Steam 티켓을 백엔드에 보내 JWT accessToken을 발급받는다.
            NetworkManager.Instance.Post<SteamAuthRequest, SteamAuthResponse>(
                urlFactory: () => APIConfig.User.SteamLogin,
                requestData: request,
                onSuccess,
                onError ?? (errorMsg =>
                {
                    Debug.LogError($"Steam 로그인 실패: {errorMsg}");
                })
            );
        }
    }

    public static class Chat
    {
        public static void SendLog(ChatLog log, Action<ServerResponse> onSuccess, Action<string> onError = null)
        {
            PlayerId pId = NetworkManager.Instance.GetPlayerId();
            ConversationLogRequest requestData = new ConversationLogRequest
            {
                sessionId = pId.sessionId,
                userCommand = log.userCommand,
                aiReply = log.aiReply,
                commands = log.commands,
                flag = log.flag
            };

#if UNITY_EDITOR
            Debug.Log($"[Editor Only] Skip POST {APIConfig.LLM.SendChatLog}\n{JsonConvert.SerializeObject(requestData, Formatting.Indented)}");
            onSuccess?.Invoke(new ServerResponse
            {
                id = "editor-preview",
                message = "Editor mode: conversation log request skipped."
            });
#else
            NetworkManager.Instance.Post<ConversationLogRequest, ServerResponse>(
                urlFactory: () => APIConfig.LLM.SendChatLog,
                requestData: requestData,
                onSuccess,
                onError ?? (errorMsg =>
                {
                    Debug.LogError($"대화 로그 전송 실패: {errorMsg}");
                }),
                includeAuthHeader: true
            );
#endif
        }
    }

    public static class Health
    {
        public static void Check(Action<HealthCheckResponse> onSuccess, Action<string> onError = null)
        {
            NetworkManager.Instance.Get<HealthCheckResponse>(
                urlFactory: () => APIConfig.Health.Check,
                onSuccess,
                onError ?? (errorMsg =>
                {
                    Debug.LogError($"헬스체크 실패: {errorMsg}");
                })
            );
        }
    }

    public static class Game
    {
        public static void SendSnapshot(
            GameSnapshotSaveRequest snapshot,
            Action<SnapshotUploadResponse> onSuccess,
            Action<string> onError = null)
        {
            // 저장 요청은 accessToken 인증과 함께 현재 게임 상태를 업로드한다.
            NetworkManager.Instance.Post<GameSnapshotSaveRequest, SnapshotUploadResponse>(
                urlFactory: () => APIConfig.Game.Snapshots,
                requestData: snapshot,
                onSuccess,
                onError ?? (errorMsg =>
                {
                    Debug.LogError($"스냅샷 업로드 실패: {errorMsg}");
                }),
                includeAuthHeader: true,
                showLoadingUI: true
            );
        }

        public static void GetLatestSnapshot(
            Action<LatestSnapshotResponse> onSuccess,
            Action<string> onError = null)
        {
            // 최신 저장본 1개를 가져와 현재 씬 상태를 복원하는 데 사용한다.
            NetworkManager.Instance.Get<LatestSnapshotResponse>(
                urlFactory: () => APIConfig.Game.LatestSnapshot,
                onSuccess,
                onError ?? (errorMsg =>
                {
                    Debug.LogError($"최신 스냅샷 불러오기 실패: {errorMsg}");
                }),
                includeAuthHeader: true,
                showLoadingUI: true
            );
        }
    }

    //public static class Farm
    //{
    //    public static void SaveFarmState(FarmData data, Action<ServerResponse> onSuccess) { ... }
    //    public static void LoadFarmState(Action<FarmData> onSuccess) { ... }
    //}

    //public static class User
    //{
    //    public static void Login(string id, string pw, Action<UserProfile> onSuccess) { ... }
    //}
}

[Serializable]
public class SteamAuthRequest
{
    public string ticket;
    public string identity;
    public string personaName;
}

[Serializable]
public class SteamAuthResponse
{
    public string accessToken;
    public string appUserId;
    public string steamId;
    public string displayName;
    public string sessionId;
}

[Serializable]
public class ConversationLogRequest
{
    public string sessionId;
    public string userCommand;
    public string aiReply;
    public System.Collections.Generic.List<AgentCommand> commands;
    public int flag;
}

[Serializable]
public class GameSnapshotSaveRequest
{
    // 문서 기준 저장 요청 바디만 분리해 둔 DTO다.
    public int currentToken;
    public int gold;
    public int farmLevel;
    public int farmNowExp;
    public TileStateDto[] tiles;
    public InventoryItemDto[] inventory;
}

[Serializable]
public class LatestSnapshotResponse
{
    // 최신 저장본 조회 응답을 그대로 받기 위한 DTO다.
    public bool hasSnapshot;
    public string id;
    public string userId;
    public string sessionId;
    public int currentToken;
    public int token;
    public int gold;
    public int farmLevel;
    public int farmNowExp;
    public int slotId;
    public string savedAt;
    public string message;
    public TileStateDto[] tiles;
    public InventoryItemDto[] inventory;
}
