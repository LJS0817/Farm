using System;
using System.Collections.Generic;
using LLMUnity;
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
    // 채팅 UI 진입점입니다. 입력 수집과 말풍선 생성까지만 맡고, AI 파이프라인은 하위 매니저에 위임합니다.
    private LLM _llmController;
    private AgentInstructionManager _instructionMng;

    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private Button _submitButton;
    [SerializeField] private Transform _chatHistory;
    [SerializeField] private GameObject _chatBoxAgent;
    [SerializeField] private GameObject _chatBoxPlayer;

    private void Awake()
    {
        _instructionMng = GetComponent<AgentInstructionManager>();
        _llmController = GetComponent<LLM>();

       // Debug.Log(SystemInfo.operatingSystem);
     

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

        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        if (!TokenManager.Instance.TrySpendQuestionToken())
        {
            return;
        }

        GameObject playerChatBox = Instantiate(_chatBoxPlayer, _chatHistory);
        playerChatBox.GetComponent<ChatBox>().SetText(input);

        // 에이전트 말풍선은 먼저 만들어 두고, 파이프라인 진행에 따라 "생각중..." -> 최종 답변으로 갱신합니다.
        GameObject agentChatBox = Instantiate(_chatBoxAgent, _chatHistory);
        if (_instructionMng != null)
        {
            _instructionMng.HandleUserInput(input, agentChatBox);
        }
        else if (agentChatBox.TryGetComponent(out ChatBox chatBox))
        {
            chatBox.SetText("에이전트 초기화에 실패했습니다.");
        }
    }

    public void ClearChatHistory()
    {
        if (_chatHistory != null)
        {
            for (int i = _chatHistory.childCount - 1; i >= 0; i--)
            {
                GameObject child = _chatHistory.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        if (_inputField != null)
        {
            _inputField.text = string.Empty;
            _inputField.ActivateInputField();
            _inputField.Select();
        }
    }
}
