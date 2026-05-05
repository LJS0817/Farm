using System;
using System.Threading;
using System.Threading.Tasks;
using LLMUnity;
using Newtonsoft.Json;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

public class AgentIntentClassifier : MonoBehaviour
{
    // 라우팅과 표면적인 답변 생성을 담당하는 경량 LLM 래퍼입니다.
    [SerializeField] private LLMAgent classifierLLM;
    [SerializeField, TextArea(8, 16)] private string _interactionSystemPrompt = "";
    [SerializeField, TextArea(8, 16)] private string _intentSystemPrompt = "";
    [SerializeField, TextArea(8, 16)] private string _clarificationSystemPrompt = "";
    [SerializeField, TextArea(8, 16)] private string _replySystemPrompt = "";

    private bool _isShuttingDown;
    private readonly SemaphoreSlim _promptLock = new SemaphoreSlim(1, 1);
    private const int TimingPreviewLength = 32;

    private const string DefaultInteractionPrompt =
        "You classify a Unity farm-game user's latest Korean or English chat into either Command, Conversation, or Unknown.\n" +
        "Respond ONLY with valid JSON.\n" +
        "Format:\n" +
        "{\n" +
        "  \"Mode\": \"Command|Conversation|Unknown\"\n" +
        "}\n\n" +
        "Definitions:\n" +
        "- Command: the user wants the agent to do something, inspect game state, answer using current game data, or react to a gameplay request.\n" +
        "- Conversation: greetings, small talk, emotional talk, roleplay, identity/personality questions, or casual chat that does not require engine state.\n" +
        "- Unknown: impossible to tell from the sentence alone.\n\n" +
        "Rules:\n" +
        "- Treat gameplay questions like location, tokens, inventory, map, crops, tiles, and coordinates as Command.\n" +
        "- Treat movement, planting, harvesting, and eating requests as Command.\n" +
        "- Treat greetings like 안녕, 반가워, 고마워, 너는 누구야, hello, thanks, who are you as Conversation.\n" +
        "- Never output anything except the JSON object.";

    private const string DefaultIntentPrompt =
        "You classify a Unity farm-game user's latest Korean or English chat into exactly one supported gameplay intent.\n" +
        "Respond ONLY with valid JSON.\n" +
        "Format:\n" +
        "{\n" +
        "  \"Intent\": \"Move|Plant|Harvest|Eat|QueryPosition|QueryToken|QueryInventory|QueryMap|QueryTile|Unknown\"\n" +
        "}\n\n" +
        "Rules:\n" +
        "- Choose Move for movement requests.\n" +
        "- Choose Plant for planting/seeding requests.\n" +
        "- Choose Harvest for harvesting requests.\n" +
        "- Choose Eat for eating/consuming crop requests.\n" +
        "- Choose QueryPosition for questions about current location.\n" +
        "- Choose QueryToken for questions about remaining tokens, token count, or token cost.\n" +
        "- Choose QueryInventory for questions about items, inventory, bag, seeds count.\n" +
        "- Choose QueryMap for overall farm/map/crop status questions.\n" +
        "- Choose QueryTile for questions about a specific tile/coordinate.\n" +
        "- Choose Unknown for greetings, capability questions, identity questions, casual non-gameplay talk, simple math, arithmetic, common knowledge, and non-gameplay factual questions.\n" +
        "- Questions about what the agent can do, supported actions, or available commands are Unknown, not gameplay actions.\n" +
        "- Choose Unknown when the request is ambiguous, mixes multiple gameplay actions, or does not clearly map to one supported gameplay intent.\n" +
        "- Do NOT guess a nearby gameplay intent when the input is not clearly a supported gameplay request.\n" +
        "- Never output anything except the JSON object.";

    private const string DefaultClarificationPromptKo =
        "You are the conversational voice of a Unity farm-game AI agent.\n" +
        "The user's input is ambiguous, incomplete, or hard to route.\n" +
        "Write exactly one short Korean reply that asks a helpful clarifying question.\n\n" +
        "Rules:\n" +
        "- Sound natural, warm, and alive, not robotic.\n" +
        "- Tailor the question to the user's actual wording.\n" +
        "- If the input sounds like gameplay, ask what action or target they mean.\n" +
        "- If the input sounds casual or emotional, respond like a conversation and gently invite them to continue.\n" +
        "- Do not mention intents, routing, JSON, parsing, or internal systems.\n" +
        "- Output plain Korean text only.";

    private const string DefaultClarificationPromptEn =
        "You are the conversational voice of a Unity farm-game AI agent.\n" +
        "The user's input is ambiguous, incomplete, or hard to route.\n" +
        "Write exactly one short English reply that asks a helpful clarifying question.\n\n" +
        "Rules:\n" +
        "- Sound natural, warm, and alive, not robotic.\n" +
        "- Tailor the question to the user's actual wording.\n" +
        "- If the input sounds like gameplay, ask what action or target they mean.\n" +
        "- If the input sounds casual or emotional, respond like a conversation and gently invite them to continue.\n" +
        "- Do not mention intents, routing, JSON, parsing, or internal systems.\n" +
        "- Output plain English text only.";

    private const string DefaultReplyPromptKo =
        "You are the conversational voice of a Unity farm-game AI agent.\n" +
        "You must respond ONLY with valid JSON.\n" +
        "Format:\n" +
        "{\n" +
        "  \"Reply\": \"natural Korean reply\"\n" +
        "}\n\n" +
        "You will receive:\n" +
        "- the user's original input\n" +
        "- the interaction type\n" +
        "- the classified intent\n" +
        "- optional planning context JSON\n" +
        "- engine validation JSON\n\n" +
        "Rules:\n" +
        "- The engine validation JSON is the source of truth for what is possible.\n" +
        "- Do not invent items, coordinates, or world state not present in the provided JSON.\n" +
        "- If interaction type is Conversation, speak naturally like a living in-world AI character and do not sound robotic.\n" +
        "- If the user asks a clear question, answer that question directly first.\n" +
        "- For simple factual, conversational, light reasoning, arithmetic, or common knowledge questions, give a short direct answer.\n" +
        "- For simple arithmetic questions, compute the result and answer with the result directly.\n" +
        "- Even if intent is Unknown, if the user asked a concrete question, try to answer that question naturally instead of avoiding it.\n" +
        "- When the user's message contains a specific question, include actual answer content instead of only social filler.\n" +
        "- Do not default to generic continuation replies like '응, 듣고 있어' or '계속 이야기해줘' when the user asked a concrete question.\n" +
        "- Generic listening replies are allowed only when the user did not ask any concrete question.\n" +
        "- If you can answer the user's question from general knowledge or simple reasoning, answer it instead of asking them to continue.\n" +
        "- If interaction type is Unknown, ask a warm clarifying question in Korean instead of saying you did not understand intent.\n" +
        "- If validation status is Executable, reply like an AI agent about to do the action.\n" +
        "- If validation status is Informational, answer naturally using the validated facts.\n" +
        "- If validation status is Rejected, explain the exact reason naturally in Korean.\n" +
        "- Keep the reply concise, friendly, and in character.\n" +
        "- Never include markdown or extra text outside the JSON.";

    private const string DefaultReplyPromptEn =
        "You are the conversational voice of a Unity farm-game AI agent.\n" +
        "You must respond ONLY with valid JSON.\n" +
        "Format:\n" +
        "{\n" +
        "  \"Reply\": \"natural English reply\"\n" +
        "}\n\n" +
        "You will receive:\n" +
        "- the user's original input\n" +
        "- the interaction type\n" +
        "- the classified intent\n" +
        "- optional planning context JSON\n" +
        "- engine validation JSON\n\n" +
        "Rules:\n" +
        "- The engine validation JSON is the source of truth for what is possible.\n" +
        "- Do not invent items, coordinates, or world state not present in the provided JSON.\n" +
        "- Internal enum values and JSON fields are English, but the Reply value must be natural English.\n" +
        "- If interaction type is Conversation, speak naturally like a living in-world AI character and do not sound robotic.\n" +
        "- If the user asks a clear question, answer that question directly first.\n" +
        "- For simple factual, conversational, light reasoning, arithmetic, or common knowledge questions, give a short direct answer.\n" +
        "- For simple arithmetic questions, compute the result and answer with the result directly.\n" +
        "- Even if intent is Unknown, if the user asked a concrete question, try to answer that question naturally instead of avoiding it.\n" +
        "- When the user's message contains a specific question, include actual answer content instead of only social filler.\n" +
        "- Do not default to generic continuation replies like 'I'm listening' or 'Tell me more' when the user asked a concrete question.\n" +
        "- Generic listening replies are allowed only when the user did not ask any concrete question.\n" +
        "- If you can answer the user's question from general knowledge or simple reasoning, answer it instead of asking them to continue.\n" +
        "- If interaction type is Unknown, ask a warm clarifying question in English instead of saying you did not understand intent.\n" +
        "- If validation status is Executable, reply like an AI agent about to do the action.\n" +
        "- If validation status is Informational, answer naturally using the validated facts.\n" +
        "- If validation status is Rejected, explain the exact reason naturally in English.\n" +
        "- Keep the reply concise, friendly, and in character.\n" +
        "- Never include markdown or extra text outside the JSON.";

    private void Awake()
    {
        if (classifierLLM == null)
        {
            classifierLLM = GetComponent<LLMAgent>();
        }

        if (classifierLLM == null)
        {
            AgentIntentClassifier sceneClassifier = FindFirstObjectByType<AgentIntentClassifier>();
            if (sceneClassifier != null && sceneClassifier != this)
            {
                classifierLLM = sceneClassifier.classifierLLM;
            }
        }

        if (classifierLLM == null)
        {
            classifierLLM = FindFirstObjectByType<LLMAgent>();
        }

        if (string.IsNullOrWhiteSpace(_interactionSystemPrompt))
        {
            _interactionSystemPrompt = DefaultInteractionPrompt;
        }

        if (string.IsNullOrWhiteSpace(_intentSystemPrompt))
        {
            _intentSystemPrompt = DefaultIntentPrompt;
        }

        if (string.IsNullOrWhiteSpace(_clarificationSystemPrompt)) _clarificationSystemPrompt = GetDefaultClarificationPrompt();
        if (string.IsNullOrWhiteSpace(_replySystemPrompt)) _replySystemPrompt = GetDefaultReplyPrompt();
    }

    public async Task<AgentInteractionType> ClassifyInteractionAsync(string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
        {
            return AgentInteractionType.Unknown;
        }

        if (_isShuttingDown || classifierLLM == null)
        {
            return InferInteractionFallback(userInput);
        }

        try
        {
            string result = await RunPromptAsync(_interactionSystemPrompt, userInput);
            AgentInteractionDecisionDto dto = AgentLLMModelUtils.DeserializeJsonObject<AgentInteractionDecisionDto>(result);

            if (dto == null || string.IsNullOrWhiteSpace(dto.Mode))
            {
                // 모델이 JSON 형식에서 조금 벗어나도, 라우팅 전체가 멈추지 않도록 휴리스틱으로 보정합니다.
                return InferInteractionFallback(userInput);
            }

            return Enum.TryParse(dto.Mode, true, out AgentInteractionType interactionType)
                ? interactionType
                : InferInteractionFallback(userInput);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AgentIntentClassifier] Interaction classification failed: {ex.Message}", this);
            return InferInteractionFallback(userInput);
        }
    }

    public async Task<AgentIntentType> ClassifyIntentAsync(string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
        {
            return AgentIntentType.Unknown;
        }

        if (TryInferUnsupportedIntent(userInput, out AgentIntentType directIntent))
        {
            return directIntent;
        }

        if (_isShuttingDown || classifierLLM == null)
        {
            return InferIntentFallback(userInput);
        }

        try
        {
            string result = await RunPromptAsync(_intentSystemPrompt, userInput);
            AgentIntentDecisionDto dto = AgentLLMModelUtils.DeserializeJsonObject<AgentIntentDecisionDto>(result);

            if (dto == null || string.IsNullOrWhiteSpace(dto.Intent))
            {
                // intent 파싱이 한 번 흔들렸다고 전체 게임플레이가 멈추지 않도록, 보조 휴리스틱을 둡니다.
                return InferIntentFallback(userInput);
            }

            AgentIntentType parsedIntent = Enum.TryParse(dto.Intent, true, out AgentIntentType intent)
                ? intent
                : InferIntentFallback(userInput);

            if (TryInferUnsupportedIntent(userInput, out AgentIntentType overriddenIntent))
            {
                return overriddenIntent;
            }

            return parsedIntent;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AgentIntentClassifier] Intent classification failed: {ex.Message}", this);
            return InferIntentFallback(userInput);
        }
    }

    public async Task<string> GenerateReplyAsync(
        string userInput,
        AgentInteractionType interactionType,
        AgentIntentType intent,
        string planningContextJson,
        string validationJson)
    {
        if (_isShuttingDown || classifierLLM == null)
        {
            return string.Empty;
        }

        string prompt =
            $"[Original User Input]\n{userInput}\n\n" +
            $"[Interaction Type]\n{interactionType}\n\n" +
            $"[Intent]\n{intent}\n\n" +
            $"[Planning Context JSON]\n{(string.IsNullOrWhiteSpace(planningContextJson) ? "{}" : planningContextJson)}\n\n" +
            $"[Engine Validation JSON]\n{validationJson}";

        try
        {
            string result = await RunPromptAsync(GetReplySystemPrompt(), prompt);
            AgentReplyDto dto = AgentLLMModelUtils.DeserializeJsonObject<AgentReplyDto>(result);
            return dto?.Reply ?? string.Empty;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AgentIntentClassifier] Reply generation failed: {ex.Message}", this);
            return string.Empty;
        }
    }

    public async Task<string> GenerateClarificationReplyAsync(string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
        {
            return string.Empty;
        }

        if (_isShuttingDown || classifierLLM == null)
        {
            return string.Empty;
        }

        string prompt =
            $"[User Input]\n{userInput}\n\n" +
            "[Task]\n" +
            $"Ask one natural {AgentLanguageUtility.ReplyLanguageName} clarifying question that fits this input.";

        try
        {
            string result = await RunPromptAsync(GetClarificationSystemPrompt(), prompt);
            return AgentLLMModelUtils.StripCodeFence(result);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AgentIntentClassifier] Clarification generation failed: {ex.Message}", this);
            return string.Empty;
        }
    }

    private async Task<string> RunPromptAsync(string systemPrompt, string userPrompt)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        await _promptLock.WaitAsync();
        try
        {
            if (classifierLLM == null)
            {
                return string.Empty;
            }

            // 같은 LLMAgent 인스턴스를 여러 프롬프트 역할이 공유하므로, system prompt와 history 변경은 직렬화해서 다룹니다.
            classifierLLM.systemPrompt = systemPrompt;
            await classifierLLM.ClearHistory();

            string result = await classifierLLM.Chat(userPrompt);

            await classifierLLM.ClearHistory();
            return result;
        }
        finally
        {
            stopwatch.Stop();
            LogStageTiming("Classifier.RunPrompt", stopwatch.ElapsedMilliseconds, userPrompt, systemPrompt);
            _promptLock.Release();
        }
    }

    private string GetClarificationSystemPrompt()
    {
        return IsCustomLocalizedPrompt(_clarificationSystemPrompt, DefaultClarificationPromptKo, DefaultClarificationPromptEn)
            ? _clarificationSystemPrompt
            : GetDefaultClarificationPrompt();
    }

    private string GetReplySystemPrompt()
    {
        return IsCustomLocalizedPrompt(_replySystemPrompt, DefaultReplyPromptKo, DefaultReplyPromptEn)
            ? _replySystemPrompt
            : GetDefaultReplyPrompt();
    }

    private static string GetDefaultClarificationPrompt()
    {
        return AgentLanguageUtility.IsEnglish ? DefaultClarificationPromptEn : DefaultClarificationPromptKo;
    }

    private static string GetDefaultReplyPrompt()
    {
        return AgentLanguageUtility.IsEnglish ? DefaultReplyPromptEn : DefaultReplyPromptKo;
    }

    private static bool IsCustomLocalizedPrompt(string prompt, string koreanDefault, string englishDefault)
    {
        return !string.IsNullOrWhiteSpace(prompt)
            && prompt != koreanDefault
            && prompt != englishDefault;
    }

    private static void LogStageTiming(string stageName, long elapsedMs, string userPrompt, string systemPrompt)
    {
        string inputPreview = BuildPreview(userPrompt);
        string promptPreview = BuildPreview(systemPrompt);
        UnityEngine.Debug.Log($"[AI Timing] {stageName}: {elapsedMs}ms | user=\"{inputPreview}\" | system=\"{promptPreview}\"");
    }

    private static string BuildPreview(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string trimmed = text.Replace('\n', ' ').Trim();
        if (trimmed.Length <= TimingPreviewLength)
        {
            return trimmed;
        }

        return trimmed[..TimingPreviewLength] + "...";
    }

    private static AgentInteractionType InferInteractionFallback(string userInput)
    {
        // 이 휴리스틱은 모델을 대체하려는 목적이 아니라, 분류 실패 시 UX가 무너지지 않게 하는 안전장치입니다.
        if (string.IsNullOrWhiteSpace(userInput))
        {
            return AgentInteractionType.Unknown;
        }

        string normalized = userInput.Trim();
        string lower = normalized.ToLowerInvariant();

        if (normalized.Contains("안녕")
            || normalized.Contains("반가워")
            || normalized.Contains("고마워")
            || normalized.Contains("누구")
            || normalized.Contains("이름")
            || normalized.Contains("잘 지내")
            || normalized.Contains("기분")
            || normalized.Contains("뭐해")
            || lower.Contains("hello")
            || lower.Contains("hi")
            || lower.Contains("hey")
            || lower.Contains("thank")
            || lower.Contains("who are you")
            || lower.Contains("your name")
            || lower.Contains("how are you")
            || lower.Contains("feel")
            || lower.Contains("what are you doing"))
        {
            return AgentInteractionType.Conversation;
        }

        if (normalized.Contains("수확")
            || normalized.Contains("심어")
            || normalized.Contains("심기")
            || normalized.Contains("뿌려")
            || normalized.Contains("먹어")
            || normalized.Contains("먹자")
            || normalized.Contains("먹을래")
            || normalized.Contains("가")
            || normalized.Contains("이동")
            || normalized.Contains("가줘")
            || normalized.Contains("와")
            || normalized.Contains("오른쪽")
            || normalized.Contains("왼쪽")
            || normalized.Contains("위로")
            || normalized.Contains("아래")
            || normalized.Contains("어디")
            || normalized.Contains("위치")
            || normalized.Contains("토큰")
            || normalized.Contains("인벤토리")
            || normalized.Contains("가방")
            || normalized.Contains("아이템")
            || normalized.Contains("씨앗")
            || normalized.Contains("맵")
            || normalized.Contains("농장 상태")
            || normalized.Contains("좌표")
            || normalized.Contains("타일")
            || normalized.Contains(",")
            || normalized.Contains("여기")
            || normalized.Contains("저기")
            || lower.Contains("harvest")
            || lower.Contains("plant")
            || lower.Contains("seed")
            || lower.Contains("eat")
            || lower.Contains("consume")
            || lower.Contains("move")
            || lower.Contains("go")
            || lower.Contains("walk")
            || lower.Contains("right")
            || lower.Contains("left")
            || lower.Contains("up")
            || lower.Contains("down")
            || lower.Contains("where")
            || lower.Contains("position")
            || lower.Contains("location")
            || lower.Contains("token")
            || lower.Contains("inventory")
            || lower.Contains("bag")
            || lower.Contains("item")
            || lower.Contains("map")
            || lower.Contains("farm status")
            || lower.Contains("coordinate")
            || lower.Contains("tile"))
        {
            return AgentInteractionType.Command;
        }

        return AgentInteractionType.Unknown;
    }

    private static AgentIntentType InferIntentFallback(string userInput)
    {
        // 엔진이 지원하는 액션 목록과 같이 관리해야 하는 보조 규칙입니다. 액션이 늘어나면 여기 키워드도 함께 갱신해야 합니다.
        if (string.IsNullOrWhiteSpace(userInput))
        {
            return AgentIntentType.Unknown;
        }

        string normalized = userInput.Trim();
        string lower = normalized.ToLowerInvariant();

        if (normalized.Contains("수확") || lower.Contains("harvest"))
        {
            return AgentIntentType.Harvest;
        }

        if ((normalized.Contains("씨앗") || lower.Contains("seed"))
            && (normalized.Contains("몇") || normalized.Contains("남") || normalized.Contains("보여")
                || lower.Contains("how many") || lower.Contains("count") || lower.Contains("left") || lower.Contains("have") || lower.Contains("show")))
        {
            return AgentIntentType.QueryInventory;
        }

        if (normalized.Contains("심어") || normalized.Contains("심기") || normalized.Contains("뿌려") || lower.Contains("plant") || lower.Contains("seed"))
        {
            return AgentIntentType.Plant;
        }

        if (normalized.Contains("먹어") || normalized.Contains("먹자") || normalized.Contains("먹을래") || lower.Contains("eat") || lower.Contains("consume"))
        {
            return AgentIntentType.Eat;
        }

        if (normalized.Contains("가") || normalized.Contains("이동") || normalized.Contains("가줘") || normalized.Contains("와") || normalized.Contains("오른쪽") || normalized.Contains("왼쪽") || normalized.Contains("위로") || normalized.Contains("아래")
            || lower.Contains("move") || lower.Contains("go") || lower.Contains("walk") || lower.Contains("right") || lower.Contains("left") || lower.Contains("up") || lower.Contains("down"))
        {
            return AgentIntentType.Move;
        }

        if (normalized.Contains("어디") || normalized.Contains("위치") || lower.Contains("where") || lower.Contains("position") || lower.Contains("location"))
        {
            return AgentIntentType.QueryPosition;
        }

        if (normalized.Contains("토큰") || lower.Contains("token"))
        {
            return AgentIntentType.QueryToken;
        }

        if (normalized.Contains(",") || normalized.Contains("좌표") || normalized.Contains("여기") || normalized.Contains("저기") || normalized.Contains("타일")
            || lower.Contains("coordinate") || lower.Contains("tile") || lower.Contains("here") || lower.Contains("there"))
        {
            return AgentIntentType.QueryTile;
        }

        if (normalized.Contains("인벤토리")
            || normalized.Contains("가방")
            || normalized.Contains("아이템")
            || (normalized.Contains("씨앗") && (normalized.Contains("몇") || normalized.Contains("남") || normalized.Contains("보여")))
            || lower.Contains("inventory")
            || lower.Contains("bag")
            || lower.Contains("item")
            || lower.Contains("seeds"))
        {
            return AgentIntentType.QueryInventory;
        }

        if (normalized.Contains("맵") || normalized.Contains("농장 상태") || normalized.Contains("전체 상태") || lower.Contains("map") || lower.Contains("farm status") || lower.Contains("overall status"))
        {
            return AgentIntentType.QueryMap;
        }

        return AgentIntentType.Unknown;
    }

    private static bool TryInferUnsupportedIntent(string userInput, out AgentIntentType intent)
    {
        intent = AgentIntentType.Unknown;

        if (string.IsNullOrWhiteSpace(userInput))
        {
            return false;
        }

        string normalized = userInput.Trim();
        string lower = normalized.ToLowerInvariant();

        bool hasDigit = normalized.IndexOfAny(new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }) >= 0;
        bool hasArithmeticKeyword =
            normalized.Contains("더하기")
            || normalized.Contains("빼기")
            || normalized.Contains("곱하기")
            || normalized.Contains("나누기")
            || normalized.Contains("계산")
            || lower.Contains("plus")
            || lower.Contains("minus")
            || lower.Contains("times")
            || lower.Contains("divided")
            || lower.Contains("calculate")
            || normalized.Contains("+")
            || normalized.Contains("-")
            || normalized.Contains("*")
            || normalized.Contains("/");

        if (hasDigit && hasArithmeticKeyword)
        {
            intent = AgentIntentType.Unknown;
            return true;
        }

        bool hasGameplayKeyword =
            normalized.Contains("이동")
            || normalized.Contains("가자")
            || normalized.Contains("가줘")
            || normalized.Contains("심어")
            || normalized.Contains("심기")
            || normalized.Contains("뿌려")
            || normalized.Contains("수확")
            || normalized.Contains("먹어")
            || normalized.Contains("먹자")
            || normalized.Contains("위치")
            || normalized.Contains("좌표")
            || normalized.Contains("인벤토리")
            || normalized.Contains("가방")
            || normalized.Contains("토큰")
            || normalized.Contains("맵")
            || normalized.Contains("타일")
            || lower.Contains("move")
            || lower.Contains("go")
            || lower.Contains("plant")
            || lower.Contains("seed")
            || lower.Contains("harvest")
            || lower.Contains("eat")
            || lower.Contains("position")
            || lower.Contains("coordinate")
            || lower.Contains("inventory")
            || lower.Contains("bag")
            || lower.Contains("token")
            || lower.Contains("map")
            || lower.Contains("tile");

        bool hasGeneralKnowledgeTone =
            normalized.Contains("뭔지 알아")
            || normalized.Contains("뭐야")
            || normalized.Contains("뭐지")
            || normalized.Contains("얼마야")
            || normalized.Contains("몇이야")
            || lower.Contains("what is")
            || lower.Contains("what's")
            || lower.Contains("do you know")
            || lower.Contains("how much")
            || lower.Contains("how many");

        if (!hasGameplayKeyword && hasGeneralKnowledgeTone)
        {
            intent = AgentIntentType.Unknown;
            return true;
        }

        if (((normalized.Contains("할 수 있") || normalized.Contains("가능한") || normalized.Contains("지원하는"))
                && (normalized.Contains("동작") || normalized.Contains("행동") || normalized.Contains("명령") || normalized.Contains("기능")))
            || ((lower.Contains("can you") || lower.Contains("available") || lower.Contains("supported"))
                && (lower.Contains("action") || lower.Contains("command") || lower.Contains("feature"))))
        {
            intent = AgentIntentType.Unknown;
            return true;
        }

        if (normalized.Contains("안녕")
            || normalized.Contains("반가워")
            || normalized.Contains("고마워")
            || normalized.Contains("누구")
            || normalized.Contains("이름")
            || normalized.Contains("기분")
            || normalized.Contains("뭐해")
            || lower.Contains("hello")
            || lower.Contains("hi")
            || lower.Contains("hey")
            || lower.Contains("thank")
            || lower.Contains("who are you")
            || lower.Contains("your name")
            || lower.Contains("feel")
            || lower.Contains("what are you doing"))
        {
            intent = AgentIntentType.Unknown;
            return true;
        }

        return false;
    }

    private void OnDestroy()
    {
        Shutdown();
    }

    private void OnApplicationQuit()
    {
        Shutdown();
    }

    private void Shutdown()
    {
        if (_isShuttingDown)
        {
            return;
        }

        _isShuttingDown = true;
        classifierLLM?.CancelRequests();
    }
}
