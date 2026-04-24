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
            NetworkManager.Instance.Post<SteamAuthRequest, SteamAuthResponse>(
                url: APIConfig.User.SteamLogin,
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
                url: APIConfig.LLM.SendChatLog,
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
                url: APIConfig.Health.Check,
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
            GameStateSnapshot snapshot,
            Action<SnapshotUploadResponse> onSuccess,
            Action<string> onError = null)
        {
#if UNITY_EDITOR
            Debug.Log($"[Editor Only] Skip POST {APIConfig.Game.Snapshots}\n{JsonConvert.SerializeObject(snapshot, Formatting.Indented)}");
            onSuccess?.Invoke(new SnapshotUploadResponse
            {
                id = "editor-preview",
                userId = snapshot.userId,
                createdAt = DateTime.UtcNow.ToString("o"),
                tileCount = snapshot.tiles != null ? snapshot.tiles.Length : 0
            });
#else
            NetworkManager.Instance.Post<GameStateSnapshot, SnapshotUploadResponse>(
                url: APIConfig.Game.Snapshots,
                requestData: snapshot,
                onSuccess,
                onError ?? (errorMsg =>
                {
                    Debug.LogError($"스냅샷 업로드 실패: {errorMsg}");
                }),
                includeAuthHeader: true
            );
#endif
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
