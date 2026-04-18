using LLMUnity;
using System;
using System.Collections.Generic;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ChatLog
{
    public string userCommand;
    public string aiReply;
    public List<AgentCommand> commands;
    /// <summary>
    /// 피드백 플래그
    /// <para>0 = None</para>
    /// <para>1 = Yes</para>
    /// <para>2 = No</para>
    /// </summary>
    public int flag = 0;

    public ChatLog() { }
    public ChatLog(string inst, AgentResponse res)
    {
        userCommand = inst;
        aiReply = res.answer;
        commands = res.commands;
    }

    public override string ToString()
    {
        string str = userCommand + "\n";
        str = str + aiReply + "\n";
        foreach (AgentCommand cmd in commands) str = str + cmd.ToString() + "\n";
        str += flag;
        return str;
    }
}

[DefaultExecutionOrder(-100)]
public class AgentChatManager : MonoBehaviour
{
    LLM _llmController;
    AgentInstructionManager _instructionMng;

    [SerializeField]
    AgentIntentClassifier _classifier;

    [SerializeField]
    TMP_InputField _inputField;
    [SerializeField]
    Button _submitButton;

    [SerializeField]
    Transform _chatHistory;

    [SerializeField]
    GameObject _chatBoxAgent;

    [SerializeField]
    GameObject _chatBoxPlayer;

    void Awake()
    {
        _instructionMng = GetComponent<AgentInstructionManager>();
        _llmController = GetComponent<LLM>();

        Debug.Log(SystemInfo.operatingSystem);
        if (SystemInfo.operatingSystem.Contains("Mac")) _llmController.numGPULayers = 0;

        _inputField.onSubmit.AddListener(SubmitInput);
        _submitButton.onClick.AddListener(SubmitInput);
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
        _classifier.GetFinalPrompt(input, (rst) =>
        {
            Debug.Log($"[라우팅 결과] AI 판별: {rst.Item2}");

            // 분류기가 완성해준 finalPrompt를 메인 AI에게 그대로 전달
            _instructionMng.Chat(rst.Item1, rst.Item2, Instantiate(_chatBoxAgent, _chatHistory));
        });
    }
}
