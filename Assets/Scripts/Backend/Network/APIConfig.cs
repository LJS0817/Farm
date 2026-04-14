public static class APIConfig
{
    // 1. 서버 환경 설정 (개발용 서버 vs 실제 서비스용 서버)
    public enum ServerEnv { Dev, Live }

    // 이 값만 Live로 바꾸면 프로젝트 전체의 API 주소가 한 번에 실제 서버로 바뀝니다!
    public static ServerEnv CurrentEnvironment = ServerEnv.Dev;

    // 2. Base URL 관리
    private static string BaseUrl
    {
        get
        {
            return "https://dev-api.yourgame.com/v1";
        }
    }

    public static class User
    {
        public static string Login => $"{BaseUrl}/user/login";

        public static string Profile(int userId) => $"{BaseUrl}/user/profile/{userId}";
    }

    public static class Farm
    {
        public static string Save => $"{BaseUrl}/farm/save";
        public static string Load => $"{BaseUrl}/farm/load";
        public static string Harvest => $"{BaseUrl}/farm/harvest";
    }

    public static class LLM
    {
        public static string SendChatLog => $"{BaseUrl}/ai/command";
    }
}