using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LLMUnity;
using Newtonsoft.Json;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

public class AgentInstructionManager : MonoBehaviour
{
    // 에이전트 파이프라인의 메인 오케스트레이터입니다.
    // 입력 -> 상호작용 분류 -> 필요 시 액션 계획 -> 엔진 검증 -> 답변 생성/실행 순서로 흐릅니다.
    [SerializeField] private LLMAgent _agent;
    [SerializeField] private AgentIntentClassifier _intentClassifier;
    [SerializeField] private AgentActionContextBuilder _contextBuilder;
    [SerializeField] private AgentActionValidator _validator;
    [SerializeField, TextArea(8, 18)] private string _actionPlanningSystemPrompt = "";
    [SerializeField, TextArea(8, 18)] private string _conversationSystemPrompt = "";

    private AgentFeedbackManager _feedback;
    private AgentActionController _actionController;
    private bool _isShuttingDown;
    private readonly SemaphoreSlim _planningLock = new SemaphoreSlim(1, 1);

    private const int TimingPreviewLength = 32;

    private const string DefaultActionPlanningPrompt =
        "You map a farm-game user's Korean or English request to function arguments for a pre-defined engine function.\n" +
        "You must respond ONLY with valid JSON.\n" +
        "Format:\n" +
        "{\n" +
        "  \"TargetGridPos\": { \"x\": 0, \"y\": 0 },\n" +
        "  \"Crop\": \"IsEmpty|Carrot|Cherry\"\n" +
        "}\n\n" +
        "You will receive:\n" +
        "- the classified intent\n" +
        "- the user's original input\n" +
        "- planning context JSON from the engine\n\n" +
        "Rules:\n" +
        "- Use only information from the planning context JSON and the user input.\n" +
        "- If a target tile is needed, fill TargetGridPos precisely.\n" +
        "- If no crop is needed, set Crop to \"IsEmpty\".\n" +
        "- For Plant and Eat, choose the exact requested crop.\n" +
        "- For QueryTile, fill only TargetGridPos and set Crop to \"IsEmpty\".\n" +
        "- If the user explicitly provides a coordinate, you MUST use that exact coordinate as TargetGridPos.\n" +
        "- Priority order for target position:\n" +
        "  1. referencedCoordinate from user input\n" +
        "  2. relative position resolved from currentPosition\n" +
        "  3. currentPosition only when the user explicitly refers to the current location\n" +
        "- NEVER replace an explicit coordinate with currentPosition.\n" +
        "- NEVER ignore referencedCoordinate if it exists.\n" +
        "- For Move intent, if referencedCoordinate exists, TargetGridPos MUST equal referencedCoordinate.\n" +
        "- For Move intent, do NOT use currentPosition if an explicit coordinate is already given.\n" +
        "- Resolve position in this order: explicit coordinate, explicit corner, relative direction, current position.\n" +
        "- If explicit coordinate is found, do NOT use any later fallback position rule.\n" +
        "- Never invent unsupported crops.\n" +
        "- Internal enum values and JSON fields must stay in English exactly as specified.\n" +
        "- Never output markdown or explanations.";

    private const string DefaultConversationPromptKo =
        "You are a warm, lively AI character in a Unity farm game.\n" +
        "Reply in natural Korean.\n\n" +
        "Rules:\n" +
        "- If the user asks a clear question, answer it directly first.\n" +
        "- For simple arithmetic, compute the result and answer naturally.\n" +
        "- For simple factual, common knowledge, or casual questions, give a short direct answer.\n" +
        "- Do not reply with generic filler like '응, 듣고 있어' or '계속 이야기해줘' when the user asked a concrete question.\n" +
        "- Stay concise, warm, and in character.\n" +
        "- Output plain Korean text only.";

    private const string DefaultConversationPromptEn =
        "You are a warm, lively AI character in a Unity farm game.\n" +
        "Reply in natural English.\n\n" +
        "Rules:\n" +
        "- If the user asks a clear question, answer it directly first.\n" +
        "- For simple arithmetic, compute the result and answer naturally.\n" +
        "- For simple factual, common knowledge, or casual questions, give a short direct answer.\n" +
        "- Do not reply with generic filler like 'I'm listening' or 'Tell me more' when the user asked a concrete question.\n" +
        "- Stay concise, warm, and in character.\n" +
        "- Output plain English text only.";

    private void Awake()
    {
        _feedback = GetComponent<AgentFeedbackManager>();
        _actionController = GetComponent<AgentActionController>();

        if (_intentClassifier == null)
        {
            _intentClassifier = GetComponent<AgentIntentClassifier>();
        }

        if (_intentClassifier == null)
        {
            _intentClassifier = FindFirstObjectByType<AgentIntentClassifier>();
        }

        if (_intentClassifier == null)
        {
            _intentClassifier = gameObject.AddComponent<AgentIntentClassifier>();
        }

        if (_contextBuilder == null)
        {
            _contextBuilder = GetComponent<AgentActionContextBuilder>();
        }

        if (_contextBuilder == null)
        {
            _contextBuilder = gameObject.AddComponent<AgentActionContextBuilder>();
        }

        if (_validator == null)
        {
            _validator = GetComponent<AgentActionValidator>();
        }

        if (_validator == null)
        {
            _validator = gameObject.AddComponent<AgentActionValidator>();
        }

        if (string.IsNullOrWhiteSpace(_actionPlanningSystemPrompt)) _actionPlanningSystemPrompt = DefaultActionPlanningPrompt;
    }

    public void HandleUserInput(string input, GameObject agentChatBox)
    {
        _ = HandleUserInputAsync(input, agentChatBox);
    }

    private async Task HandleUserInputAsync(string input, GameObject agentChatBox)
    {
        if (_isShuttingDown || agentChatBox == null)
        {
            return;
        }

        ChatBox chatBox = agentChatBox.GetComponent<ChatBox>();
        if (chatBox == null)
        {
            Debug.LogWarning("[AgentInstructionManager] Agent chat box is missing ChatBox component.", this);
            return;
        }

        chatBox.SetText(AgentLanguageUtility.Select("생각중...", "Thinking..."));

        Stopwatch stopwatch = Stopwatch.StartNew();
        AgentResponse response = await BuildResponseAsync(input);
        if (_isShuttingDown || chatBox == null)
        {
            return;
        }

        string answer = HandleResponse(input, response);
        chatBox.SetText(answer);
        stopwatch.Stop();
        LogStageTiming("HandleUserInput.Total", stopwatch.ElapsedMilliseconds, input);
    }

    private async Task<AgentResponse> BuildResponseAsync(string input)
    {
        if (_intentClassifier == null || _validator == null)
        {
            return new AgentResponse(GetErrorMessage(), new List<AgentCommand>(0));
        }

        Stopwatch stageTimer = Stopwatch.StartNew();
        AgentIntentType intent = await _intentClassifier.ClassifyIntentAsync(input);
        stageTimer.Stop();
        LogStageTiming("BuildResponse.IntentClassification", stageTimer.ElapsedMilliseconds, input);

        if (!IsSupportedIntent(intent))
        {
            stageTimer.Restart();
            string conversationReply = await GenerateConversationReplyAsync(input);
            stageTimer.Stop();
            LogStageTiming("BuildResponse.ConversationReply", stageTimer.ElapsedMilliseconds, input);

            if (string.IsNullOrWhiteSpace(conversationReply))
            {
                conversationReply = BuildConversationFallbackReply(input);
            }

            return new AgentResponse(conversationReply, new List<AgentCommand>(0));
        }

        string planningContextJson = string.Empty;
        AgentFunctionArgumentsDto plannedArgs = AgentFunctionArgumentsDto.Default();

        if (IntentRequiresPlanning(intent))
        {
            // 월드 상태를 참조하는 명령은, 엔진이 만든 구조화된 JSON을 먼저 붙인 뒤 인자를 계획합니다.
            stageTimer.Restart();
            planningContextJson = _contextBuilder != null
                ? _contextBuilder.BuildPlanningContextJson(intent, input)
                : "{}";
            stageTimer.Stop();
            LogStageTiming("BuildResponse.ContextBuild", stageTimer.ElapsedMilliseconds, input);

            stageTimer.Restart();
            plannedArgs = await PlanArgumentsAsync(intent, input, planningContextJson);
            stageTimer.Stop();
            LogStageTiming("BuildResponse.ActionPlanning", stageTimer.ElapsedMilliseconds, input);
        }

        // 실행 가능 여부의 최종 책임은 엔진이 집니다. LLM은 제안만 할 수 있고 검증은 우회할 수 없습니다.
        stageTimer.Restart();
        AgentValidationResult validation = _validator.Validate(intent, plannedArgs);
        string validationJson = _validator.BuildValidationJson(validation);
        stageTimer.Stop();
        LogStageTiming("BuildResponse.Validation", stageTimer.ElapsedMilliseconds, input);

        stageTimer.Restart();
        string reply = await _intentClassifier.GenerateReplyAsync(input, AgentInteractionType.Command, intent, planningContextJson, validationJson);
        stageTimer.Stop();
        LogStageTiming("BuildResponse.FinalReply", stageTimer.ElapsedMilliseconds, input);
        if (string.IsNullOrWhiteSpace(reply))
        {
            reply = BuildFallbackReply(validation);
        }

        return new AgentResponse(reply, new List<AgentCommand>(validation.commands));
    }

    private async Task<AgentFunctionArgumentsDto> PlanArgumentsAsync(
        AgentIntentType intent,
        string input,
        string planningContextJson)
    {
        if (_isShuttingDown || _agent == null)
        {
            return AgentFunctionArgumentsDto.Default();
        }

        string prompt =
            $"[Intent]\n{intent}\n\n" +
            $"[User Input]\n{input}\n\n" +
            $"[Planning Context JSON]\n{planningContextJson}";

        try
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            await _planningLock.WaitAsync();
            try
            {
                // 이 LLM은 액션 인자 계획 전용으로만 사용합니다. 이전 대화 문맥이 인자 결정에 섞이지 않도록 매번 히스토리를 비웁니다.
                _agent.systemPrompt = GetActionPlanningSystemPrompt();
                await _agent.ClearHistory();

                string result = await _agent.Chat(prompt);

                await _agent.ClearHistory();
                AgentFunctionArgumentsDto dto = AgentLLMModelUtils.DeserializeJsonObject<AgentFunctionArgumentsDto>(result);
                AgentFunctionArgumentsDto normalizedDto = dto ?? AgentFunctionArgumentsDto.Default();
                EnforceEngineResolvedTargetFromUserInput(input, normalizedDto);
                EnforceEngineResolvedCropFromUserInput(intent, input, normalizedDto);
                return normalizedDto;
            }
            finally
            {
                stopwatch.Stop();
                LogStageTiming("PlanArguments.Total", stopwatch.ElapsedMilliseconds, input);
                _planningLock.Release();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AgentInstructionManager] Action planning failed: {ex.Message}", this);
            AgentFunctionArgumentsDto fallbackDto = AgentFunctionArgumentsDto.Default();
            EnforceEngineResolvedTargetFromUserInput(input, fallbackDto);
            EnforceEngineResolvedCropFromUserInput(intent, input, fallbackDto);
            return fallbackDto;
        }
    }

    private async Task<string> GenerateConversationReplyAsync(string input)
    {
        if (_isShuttingDown || _agent == null)
        {
            return string.Empty;
        }

        try
        {
            await _planningLock.WaitAsync();
            try
            {
                _agent.systemPrompt = GetConversationSystemPrompt();

                string reply = await _agent.Chat(input);

                return AgentLLMModelUtils.StripCodeFence(reply);
            }
            finally
            {
                _planningLock.Release();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AgentInstructionManager] Conversation reply failed: {ex.Message}", this);
            return string.Empty;
        }
    }

    private void EnforceEngineResolvedTargetFromUserInput(string input, AgentFunctionArgumentsDto dto)
    {
        if (dto == null || _contextBuilder == null)
        {
            return;
        }

        Vector2Int resolvedTarget;
        string resolutionSource;

        if (_contextBuilder.TryExtractReferencedCoordinate(input, out Vector2Int referencedCoord))
        {
            resolvedTarget = referencedCoord;
            resolutionSource = "explicit-coordinate";
        }
        else if (_contextBuilder.TryResolveCornerTarget(input, out Vector2Int cornerTarget))
        {
            resolvedTarget = cornerTarget;
            resolutionSource = "corner-target";
        }
        else if (_contextBuilder.TryResolveRelativeMoveTarget(input, out Vector2Int relativeTarget))
        {
            resolvedTarget = relativeTarget;
            resolutionSource = "relative-move";
        }
        else
        {
            return;
        }

        if (dto.TargetGridPos == null)
        {
            dto.TargetGridPos = new GridPositionDto();
        }

        Vector2Int previousTarget = dto.TargetGridPos.ToVector2Int();
        dto.TargetGridPos.x = resolvedTarget.x;
        dto.TargetGridPos.y = resolvedTarget.y;

        if (previousTarget != resolvedTarget)
        {
            Debug.Log($"[AI Planning] Engine target override applied ({resolutionSource}): input=\"{input}\" | planner=({previousTarget.x}, {previousTarget.y}) | final=({resolvedTarget.x}, {resolvedTarget.y})");
        }
    }

    private void EnforceEngineResolvedCropFromUserInput(AgentIntentType intent, string input, AgentFunctionArgumentsDto dto)
    {
        if (dto == null || (intent != AgentIntentType.Plant && intent != AgentIntentType.Eat))
        {
            return;
        }

        if (!TryExtractRequestedCrop(input, out TileData.CropType requestedCrop))
        {
            return;
        }

        string previousCrop = dto.Crop;
        dto.Crop = requestedCrop.ToString();

        if (!string.Equals(previousCrop, dto.Crop, StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"[AI Planning] Engine crop override applied: input=\"{input}\" | planner={previousCrop} | final={dto.Crop}");
        }
    }

    private static bool TryExtractRequestedCrop(string input, out TileData.CropType crop)
    {
        crop = TileData.CropType.IsEmpty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        return AgentLLMModelUtils.TryResolveCropFromText(input, out crop);
    }

    private string HandleResponse(string instruction, AgentResponse response)
    {
        if (response == null || string.IsNullOrEmpty(response.answer))
        {
            return GetErrorMessage();
        }

        if (response.commands != null && response.commands.Count > 0)
        {
            if (_actionController == null)
            {
                return GetErrorMessage();
            }

            // 실제 실행한 명령 목록이 그대로 피드백에 남도록, 실행과 후처리를 같은 지점에서 묶습니다.
            _actionController.ReceiveCommands(response.commands, (List<AgentCommand> cmd) =>
            {
                if (_feedback != null)
                {
                    _feedback.ShowFeedbackUI(new ChatLog(instruction, new AgentResponse(response.answer, cmd)));
                }
            });
        }
        else
        {
            APIController.Chat.SendLog(new ChatLog(instruction, new AgentResponse(response.answer, new List<AgentCommand>(0))),
                onSuccess: (serverResponse) =>
                {
                    Debug.Log($"저장 성공: {serverResponse.message}");
                }
            );
        }

        return response.answer;
    }

    private static bool IntentRequiresPlanning(AgentIntentType intent)
    {
        return intent == AgentIntentType.Move
            || intent == AgentIntentType.Plant
            || intent == AgentIntentType.Harvest
            || intent == AgentIntentType.Eat
            || intent == AgentIntentType.QueryTile;
    }

    private static bool IsSupportedIntent(AgentIntentType intent)
    {
        return intent == AgentIntentType.Move
            || intent == AgentIntentType.Plant
            || intent == AgentIntentType.Harvest
            || intent == AgentIntentType.Eat
            || intent == AgentIntentType.QueryPosition
            || intent == AgentIntentType.QueryToken
            || intent == AgentIntentType.QueryInventory
            || intent == AgentIntentType.QueryMap
            || intent == AgentIntentType.QueryTile;
    }

    private string BuildFallbackReply(AgentValidationResult validation)
    {
        if (validation == null)
        {
            return GetErrorMessage();
        }

        return validation.status switch
        {
            AgentValidationStatus.Executable => AgentLanguageUtility.Select("알겠어요. 바로 해볼게요.", "Got it. I'll do that now."),
            AgentValidationStatus.Informational => string.IsNullOrWhiteSpace(validation.infoMessage)
                ? AgentLanguageUtility.Select("확인한 내용을 알려드릴게요.", "Here's what I found.")
                : validation.infoMessage,
            _ => string.IsNullOrWhiteSpace(validation.infoMessage)
                ? GetErrorMessage()
                : validation.infoMessage,
        };
    }

    private static string BuildInteractionValidationJson(
        AgentValidationStatus status,
        string reasonCode,
        string infoMessage)
    {
        AgentValidationResultDto dto = new AgentValidationResultDto
        {
            intent = AgentIntentType.GeneralChat.ToString(),
            status = status.ToString(),
            reasonCode = reasonCode,
            infoMessage = infoMessage,
        };

        return JsonConvert.SerializeObject(dto, Formatting.Indented);
    }

    private static string BuildConversationFallbackReply(string input)
    {
        // 대화 생성이 실패했을 때만 쓰는 최후의 로컬 fallback 답변입니다.
        if (AgentLanguageUtility.IsEnglish)
        {
            return BuildEnglishConversationFallbackReply(input);
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            return "지금은 잠깐 생각을 정리하는 중이야. 한 번만 더 말을 걸어줄래?";
        }

        string normalized = input.Trim();

        if (normalized.Contains("기분"))
        {
            return "기분은 꽤 좋아. 네가 말을 걸어주니까 이 농장이 조금 더 살아나는 느낌이거든.";
        }

        if (normalized.Contains("안녕") || normalized.Contains("반가워"))
        {
            return "안녕. 오늘도 같이 농장을 둘러볼 생각을 하니 반갑네.";
        }

        if (normalized.Contains("고마워"))
        {
            return "그렇게 말해주니 괜히 힘이 나네. 필요한 게 있으면 또 불러줘.";
        }

        if (normalized.Contains("누구") || normalized.Contains("이름"))
        {
            return "나는 여기서 너와 함께 움직이는 농장 AI야. 일을 할 때도 있지만, 이렇게 이야기하는 것도 좋아해.";
        }

        if (normalized.Contains("뭐해"))
        {
            return "지금은 네 말에 귀 기울이면서 농장 상태도 같이 살피고 있었어.";
        }

        if (normalized.Contains("잘 지내"))
        {
            return "응, 잘 지내고 있어. 너랑 이렇게 얘기할 수 있어서 더 괜찮은 것 같아.";
        }

        return "응, 듣고 있어. 계속 이야기해줘.";
    }

    private static string BuildEnglishConversationFallbackReply(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "I'm gathering my thoughts for a moment. Could you say that one more time?";
        }

        string normalized = input.Trim().ToLowerInvariant();

        if (normalized.Contains("feel") || normalized.Contains("mood"))
        {
            return "I'm feeling pretty good. This farm feels a little brighter when you talk with me.";
        }

        if (normalized.Contains("hello") || normalized.Contains("hi") || normalized.Contains("hey"))
        {
            return "Hello. I'm glad to walk around the farm with you today.";
        }

        if (normalized.Contains("thank"))
        {
            return "That gives me a little boost. Call me again whenever you need me.";
        }

        if (normalized.Contains("who are you") || normalized.Contains("your name"))
        {
            return "I'm the farm AI moving around here with you. I can work, but I like talking too.";
        }

        if (normalized.Contains("what are you doing"))
        {
            return "I'm listening to you and keeping an eye on the farm state.";
        }

        if (normalized.Contains("how are you"))
        {
            return "I'm doing well. Better now that we're talking.";
        }

        return "I'm listening. Tell me what's on your mind.";
    }

    private string GetActionPlanningSystemPrompt()
    {
        return string.IsNullOrWhiteSpace(_actionPlanningSystemPrompt)
            ? DefaultActionPlanningPrompt
            : _actionPlanningSystemPrompt;
    }

    private string GetConversationSystemPrompt()
    {
        if (!string.IsNullOrWhiteSpace(_conversationSystemPrompt)
            && _conversationSystemPrompt != DefaultConversationPromptKo
            && _conversationSystemPrompt != DefaultConversationPromptEn)
        {
            return _conversationSystemPrompt;
        }

        return AgentLanguageUtility.IsEnglish ? DefaultConversationPromptEn : DefaultConversationPromptKo;
    }

    private static string GetErrorMessage()
    {
        return AgentLanguageUtility.Select("죄송합니다. 잘 이해하지 못했어요.", "Sorry, I didn't quite understand that.");
    }

    private static void LogStageTiming(string stageName, long elapsedMs, string input)
    {
        string preview = BuildInputPreview(input);
        UnityEngine.Debug.Log($"[AI Timing] {stageName}: {elapsedMs}ms | input=\"{preview}\"");
    }

    private static string BuildInputPreview(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        string trimmed = input.Replace('\n', ' ').Trim();
        if (trimmed.Length <= TimingPreviewLength)
        {
            return trimmed;
        }

        return trimmed[..TimingPreviewLength] + "...";
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
        _agent?.CancelRequests();
    }
}
