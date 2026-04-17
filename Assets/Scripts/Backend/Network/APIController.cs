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
