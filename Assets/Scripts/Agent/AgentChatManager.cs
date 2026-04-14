using System;
using System.Collections.Generic;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ChatLog
{
    public string instruct;
    public string answer;
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
        instruct = inst;
        answer = res.answer;
        commands = res.commands;
    }

    public override string ToString()
    {
        string str = instruct + "\n";
        str = str + answer + "\n";
        foreach (AgentCommand cmd in commands) str = str + cmd.ToString() + "\n";
        str += flag;
        return str;
    }
}

public class AgentChatManager : MonoBehaviour
{
    AgentInstructionManager _instructionMng;

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
        _instructionMng.Chat(input, Instantiate(_chatBoxAgent, _chatHistory));
    }
}
