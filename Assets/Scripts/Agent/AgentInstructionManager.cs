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
    LLMCharacter _agent;
    ChatBox _curChatBoxAgent;

    string _instruction;
    string _answer;

    const string _errorMsg = "죄송합니다. 알아듣지 못했습니다.";

    private void Start()
    {
        _actor = GetComponent<AgentInstructionManager>();
        _feedback = GetComponent<AgentFeedbackManager>();
        _actionController = GetComponent<AgentActionController>();
        
        _instruction = "";
        _answer = "";
    }

    void HandleReply(string replySoFar)
    {
        _answer = replySoFar;
        //_curChatBoxAgent.SetText(_answer);
    }

    void ReplyCompleted()
    {
        string answer = _actor.ParseLLMResponse(_answer);
        _curChatBoxAgent.SetText(answer);
        _curChatBoxAgent = null;
        _answer = "";
    }

    //public void Chat(string input, string finalInput, GameObject agentChatBox)
    //{
    //    Chat(input, finalInput, agentChatBox, true);
    //}

    public void Chat(string input, string finalInput, GameObject agentChatBox, bool useOntology=true)
    {
        _instruction = input;
        string prompt = finalInput;

        if (useOntology && OntologyManager.Instance != null)
        {
            prompt = OntologyManager.Instance.BuildEnhancedPrompt(prompt);
            Debug.Log("온톨로지 접근");
        }

        _agent.Chat(prompt, HandleReply, ReplyCompleted);

        _curChatBoxAgent = agentChatBox.GetComponent<ChatBox>();
        _curChatBoxAgent.SetText("생각중...");
    }

    public string ParseLLMResponse(string jsonString)
    {
        jsonString = jsonString.Replace("```json", "").Replace("```", "").Trim();

        try
        {
            AgentResponse response = JsonConvert.DeserializeObject<AgentResponse>(jsonString);
            if (response != null)
            {
                if (response.answer.Equals("")) return _errorMsg;
                if (response.commands != null && response.commands.Count > 0)
                {
                    _actionController.ReceiveCommands(response.commands, (List<AgentCommand> cmd) =>
                    {
                        _feedback.ShowFeedbackUI(new ChatLog(_instruction, new AgentResponse(response.answer, cmd)));
                    });
                }
                else
                {
                    // 대화 시 대화 내용 서버 전송
                    APIController.Chat.SendLog(new ChatLog(_instruction, new AgentResponse(response.answer, new List<AgentCommand>(0))),
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
}
