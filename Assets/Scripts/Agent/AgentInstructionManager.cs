using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LLMUnity;
using Newtonsoft.Json;
using UnityEngine;

public class AgentInstructionManager : MonoBehaviour
{
    // 에이전트 파이프라인의 메인 오케스트레이터입니다.
    // 입력 -> 상호작용 분류 -> 필요 시 액션 계획 -> 엔진 검증 -> 답변 생성/실행 순서로 흐릅니다.
    [SerializeField] private LLMAgent _agent;
    [SerializeField] private AgentIntentClassifier _intentClassifier;
    [SerializeField] private AgentActionContextBuilder _contextBuilder;
    [SerializeField] private AgentActionValidator _validator;
    [SerializeField, TextArea(8, 18)] private string _actionPlanningSystemPrompt = "";

    private AgentFeedbackManager _feedback;
    private AgentActionController _actionController;
    private bool _isShuttingDown;
    private readonly SemaphoreSlim _planningLock = new SemaphoreSlim(1, 1);

    private const string _errorMsg = "죄송합니다. 잘 이해하지 못했어요.";

    private const string DefaultActionPlanningPrompt =
        "You map a farm-game user's request to function arguments for a pre-defined engine function.\n" +
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
        "- Never invent unsupported crops.\n" +
        "- Never output markdown or explanations.";

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

        if (string.IsNullOrWhiteSpace(_actionPlanningSystemPrompt))
        {
            _actionPlanningSystemPrompt = DefaultActionPlanningPrompt;
        }
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

        chatBox.SetText("생각중...");

        AgentResponse response = await BuildResponseAsync(input);
        if (_isShuttingDown || chatBox == null)
        {
            return;
        }

        string answer = HandleResponse(input, response);
        chatBox.SetText(answer);
    }

    private async Task<AgentResponse> BuildResponseAsync(string input)
    {
        if (_intentClassifier == null || _validator == null)
        {
            return new AgentResponse(_errorMsg, new List<AgentCommand>(0));
        }

        // 1차 분기: 이 입력이 일반 대화인지, 게임 명령인지 먼저 나눕니다.
        AgentInteractionType interactionType = await _intentClassifier.ClassifyInteractionAsync(input);

        if (interactionType == AgentInteractionType.Conversation)
        {
            string conversationReply = await _intentClassifier.GenerateReplyAsync(
                input,
                interactionType,
                AgentIntentType.GeneralChat,
                string.Empty,
                BuildInteractionValidationJson(AgentValidationStatus.Informational, "Conversation", string.Empty));

            if (string.IsNullOrWhiteSpace(conversationReply))
            {
                conversationReply = BuildConversationFallbackReply(input);
            }

            return new AgentResponse(conversationReply, new List<AgentCommand>(0));
        }

        AgentIntentType intent = await _intentClassifier.ClassifyIntentAsync(input);

        // 상위 분류와 세부 intent를 모두 신뢰하기 어려우면, 기계적으로 실패하지 않고 맥락에 맞게 되묻습니다.
        if (interactionType == AgentInteractionType.Unknown && intent == AgentIntentType.Unknown)
        {
            string clarificationReply = await _intentClassifier.GenerateClarificationReplyAsync(input);

            if (string.IsNullOrWhiteSpace(clarificationReply))
            {
                clarificationReply = "내가 무엇을 해주면 되는지 조금만 더 자세히 말해줄래?";
            }

            return new AgentResponse(clarificationReply, new List<AgentCommand>(0));
        }

        string planningContextJson = string.Empty;
        AgentFunctionArgumentsDto plannedArgs = AgentFunctionArgumentsDto.Default();

        if (IntentRequiresPlanning(intent))
        {
            // 월드 상태를 참조하는 명령은, 엔진이 만든 구조화된 JSON을 먼저 붙인 뒤 인자를 계획합니다.
            planningContextJson = _contextBuilder != null
                ? _contextBuilder.BuildPlanningContextJson(intent, input)
                : "{}";
            plannedArgs = await PlanArgumentsAsync(intent, input, planningContextJson);
        }

        // 실행 가능 여부의 최종 책임은 엔진이 집니다. LLM은 제안만 할 수 있고 검증은 우회할 수 없습니다.
        AgentValidationResult validation = _validator.Validate(intent, plannedArgs);
        string validationJson = _validator.BuildValidationJson(validation);

        string reply = await _intentClassifier.GenerateReplyAsync(input, AgentInteractionType.Command, intent, planningContextJson, validationJson);
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
            await _planningLock.WaitAsync();
            try
            {
                // 이 LLM은 액션 인자 계획 전용으로만 사용합니다. 이전 대화 문맥이 인자 결정에 섞이지 않도록 매번 히스토리를 비웁니다.
                _agent.systemPrompt = _actionPlanningSystemPrompt;
                await _agent.ClearHistory();

                string result = await _agent.Chat(prompt);

                await _agent.ClearHistory();
                AgentFunctionArgumentsDto dto = AgentLLMModelUtils.DeserializeJsonObject<AgentFunctionArgumentsDto>(result);
                return dto ?? AgentFunctionArgumentsDto.Default();
            }
            finally
            {
                _planningLock.Release();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AgentInstructionManager] Action planning failed: {ex.Message}", this);
            return AgentFunctionArgumentsDto.Default();
        }
    }

    private string HandleResponse(string instruction, AgentResponse response)
    {
        if (response == null || string.IsNullOrEmpty(response.answer))
        {
            return _errorMsg;
        }

        if (response.commands != null && response.commands.Count > 0)
        {
            if (_actionController == null)
            {
                return _errorMsg;
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

    private string BuildFallbackReply(AgentValidationResult validation)
    {
        if (validation == null)
        {
            return _errorMsg;
        }

        return validation.status switch
        {
            AgentValidationStatus.Executable => "알겠어요. 바로 해볼게요.",
            AgentValidationStatus.Informational => string.IsNullOrWhiteSpace(validation.infoMessage)
                ? "확인한 내용을 알려드릴게요."
                : validation.infoMessage,
            _ => string.IsNullOrWhiteSpace(validation.infoMessage)
                ? _errorMsg
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
