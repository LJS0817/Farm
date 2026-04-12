using LLMUnity;
using Newtonsoft.Json;
using System.Numerics;
using UnityEngine;

public class AgentInstructionManager : MonoBehaviour
{
    AgentInstructionManager _actor;
    AgentFeedbackManager _feedback;
    AgentActionController _actionController;

    LLMAgent _agent;
    ChatBox _curChatBoxAgent;

    string _instruction;
    string _answer;

    private void Start()
    {
        _actor = GetComponent<AgentInstructionManager>();
        _feedback = GetComponent<AgentFeedbackManager>();
        _actionController = GetComponent<AgentActionController>();
        
        _agent = GetComponent<LLMAgent>();

        _instruction = "";
        _answer = "";
    }

    void HandleReply(string replySoFar)
    {
        _answer = replySoFar;
    }

    void ReplyCompleted()
    {
        string answer = _actor.ParseLLMResponse(_answer);
        _curChatBoxAgent.SetText(answer);
        _curChatBoxAgent = null;
        _answer = "";
    }

    public void Chat(string input, GameObject agentChatBox)
    {
        _instruction = input;
        _agent.Chat(input, HandleReply, ReplyCompleted);

        _curChatBoxAgent = agentChatBox.GetComponent<ChatBox>();
        _curChatBoxAgent.SetText("생각중...");
    }

    public string ParseLLMResponse(string jsonString)
    {
        jsonString = jsonString.Replace("```json", "").Replace("```", "").Trim();

        AgentResponse response = JsonConvert.DeserializeObject<AgentResponse>(jsonString);
        if (response != null)
        {
            Debug.Log($"[LLM 답변]: {response.answer}");

            if (response.commands != null && response.commands.Count > 0)
            {
                _actionController.ReceiveCommands(response.commands, () => {
                    _feedback.ShowFeedbackUI(_instruction);
                });
            }

            return response.answer;
        }
        else
        {
            return "JSON 파싱 실패";
        }
    }
}
