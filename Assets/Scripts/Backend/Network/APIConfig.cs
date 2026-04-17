public static class APIConfig
{
    // 2. Base URL 관리
    private static string BaseUrl
    {
        get
        {
            return "http://15.164.210.205:443/api/v1";
        }
    }

    public static class User
    {
        public static string Login => $"{BaseUrl}/game/auth/test-login";

        //public static string Profile(int userId) => $"{BaseUrl}/user/profile/{userId}";
    }

    //public static class Farm
    //{
    //    public static string Save => $"{BaseUrl}/farm/save";
    //    public static string Load => $"{BaseUrl}/farm/load";
    //    public static string Harvest => $"{BaseUrl}/farm/harvest";
    //}

    public static class LLM
    {
        public static string SendChatLog => $"{BaseUrl}/game/conversations";
    }
}