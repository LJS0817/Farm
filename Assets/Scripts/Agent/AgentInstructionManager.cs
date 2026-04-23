using LLMUnity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;


public class AgentInstructionManager : MonoBehaviour
{
    AgentInstructionManager _actor;
    AgentFeedbackManager _feedback;
    AgentActionController _actionController;

    [SerializeField]
    LLMAgent _agent;

    const string _errorMsg = "죄송합니다. 알아듣지 못했습니다.";

    bool _isShuttingDown;

    private void Start()
    {
        _actor = GetComponent<AgentInstructionManager>();
        _feedback = GetComponent<AgentFeedbackManager>();
        _actionController = GetComponent<AgentActionController>();
        
    }

    //public void Chat(string input, string finalInput, GameObject agentChatBox)
    //{
    //    Chat(input, finalInput, agentChatBox, true);
    //}

    public async void Chat(string input, string finalInput, GameObject agentChatBox, bool useOntology=true)
    {
        if (_isShuttingDown || _agent == null || agentChatBox == null)
        {
            return;
        }

        string prompt = finalInput;
        ChatBox chatBox = agentChatBox.GetComponent<ChatBox>();
        if (chatBox == null)
        {
            Debug.LogWarning("[AgentInstructionManager] Agent chat box is missing ChatBox component.", this);
            return;
        }

        if (useOntology && OntologyManager.Instance != null)
        {
            prompt = OntologyManager.Instance.BuildEnhancedPrompt(prompt);
            Debug.Log("온톨로지 접근");
        }

        chatBox.SetText("생각중...");

        string answerJson = "";
        try
        {
            answerJson = await _agent.Chat(prompt, replySoFar =>
            {
                answerJson = replySoFar;
            });
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AgentInstructionManager] LLM request failed: {ex.Message}", this);
        }

        if (_isShuttingDown || chatBox == null)
        {
            return;
        }

        string answer = _actor.ParseLLMResponse(input, answerJson);
        chatBox.SetText(answer);
    }

    public string ParseLLMResponse(string instruction, string jsonString)
    {
        if (string.IsNullOrWhiteSpace(jsonString))
        {
            return _errorMsg;
        }

        jsonString = jsonString.Replace("```json", "").Replace("```", "").Trim();

        try
        {
            AgentResponse response = JsonConvert.DeserializeObject<AgentResponse>(jsonString);
            if (response != null)
            {
                if (string.IsNullOrEmpty(response.answer)) return _errorMsg;
                if (response.commands != null && response.commands.Count > 0)
                {
                    _actionController.ReceiveCommands(response.commands, (List<AgentCommand> cmd) =>
                    {
                        _feedback.ShowFeedbackUI(new ChatLog(instruction, new AgentResponse(response.answer, cmd)));
                    });
                }
                else
                {
                    // 대화 시 대화 내용 서버 전송
                    APIController.Chat.SendLog(new ChatLog(instruction, new AgentResponse(response.answer, new List<AgentCommand>(0))),
                        onSuccess: (response) =>
                        {
                            Debug.Log($"저장 성공: {response.message}");
                        }
                    );
                    //Debug.Log("대화 : " + JsonConvert.SerializeObject(ChatLog), Formatting.Indented));
                }

                return response.answer;
            }
            else
            {
                return "JSON 파싱 실패";
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
            return _errorMsg;
        }
    }

    private void OnDestroy()
    {
        Shutdown();
    }

    void Shutdown()
    {
        if (_isShuttingDown) return;
        _isShuttingDown = true;
        _agent?.CancelRequests();
    }

    private void OnApplicationQuit()
    {
        Shutdown();
    }
}
