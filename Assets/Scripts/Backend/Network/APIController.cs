using Newtonsoft.Json;
using System;
using UnityEngine;

public static class APIController
{
    public static class Chat
    {
        public static void SendLog(ChatLog log, Action<ServerResponse> onSuccess)
        {
            PlayerId pId = NetworkManager.Instance.GetPlayerId();
            object requestData = new
            {
                userId = pId.userId,
                sessionId = pId.sessionId,
                userCommand = log.userCommand,
                aiReply = log.aiReply,
                commands = log.commands
            };

            Debug.Log(JsonConvert.SerializeObject(requestData));

            //NetworkManager.Instance.Post<object, ServerResponse>(
            //    url: APIConfig.LLM.SendChatLog,
            //    requestData: requestData,
            //    onSuccess,
            //    onError: (errorMsg) =>
            //    {
            //        Debug.LogError($"통신 실패: {errorMsg}");
            //    }
            //);
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
            NetworkManager.Instance.Post<GameStateSnapshot, SnapshotUploadResponse>(
                url: APIConfig.Game.Snapshots,
                requestData: snapshot,
                onSuccess,
                onError ?? (errorMsg =>
                {
                    Debug.LogError($"스냅샷 업로드 실패: {errorMsg}");
                })
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
