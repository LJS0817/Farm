using LLMUnity;
using System;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

public class AgentChatManager : MonoBehaviour
{
    [SerializeField]
    AgentInstructionManager _actor;

    [SerializeField]
    TMP_InputField _inputField;
    [SerializeField]
    Button _submitButton;

    [SerializeField]
    Transform _chatHistory;

    [SerializeField]
    GameObject _chatBoxAgent;
    ChatBox _curChatBoxAgent;

    [SerializeField]
    GameObject _chatBoxPlayer;

    LLMAgent _agent;

    string _agentAnswer;

    void Awake()
    {
        _agent = GetComponent<LLMAgent>();

        _inputField.onSubmit.AddListener(SubmitInput);
        _submitButton.onClick.AddListener(SubmitInput);
        _agentAnswer = "";
    }

    void HandleReply(string replySoFar)
    {
        _agentAnswer = replySoFar;
    }

    void ReplyCompleted()
    {
        _curChatBoxAgent.SetText(_actor.ParseLLMResponse(_agentAnswer));
        _curChatBoxAgent = null;
        _agentAnswer = "";
    }

    private void SubmitInput()
    {
        SubmitInput(_inputField.text);
    }

    private void SubmitInput(string input)
    {
        _inputField.text = "";
        _inputField.ActivateInputField();
        _inputField.Select();
        if (input.Length < 1 || input.Trim().Length < 1) return;

        GameObject obj = Instantiate(_chatBoxPlayer, _chatHistory);
        obj.GetComponent<ChatBox>().SetText(input);

        _agent.Chat(input, HandleReply, ReplyCompleted);
        _curChatBoxAgent = Instantiate(_chatBoxAgent, _chatHistory).GetComponent<ChatBox>();
        _curChatBoxAgent.SetText("생각중...");
    }
}
