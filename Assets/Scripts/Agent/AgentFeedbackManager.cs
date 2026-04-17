using UnityEngine;

public class AgentFeedbackManager : MonoBehaviour
{
    [SerializeField]
    ChatFeedback _feedbackUI;

    public void ShowFeedbackUI(ChatLog chat) { _feedbackUI.ShowFeedbackUI(chat); }
    public void OnFeedbackButtonClicked(bool isYes) { _feedbackUI.OnButtonClick(isYes ? 1 : 2); }
}