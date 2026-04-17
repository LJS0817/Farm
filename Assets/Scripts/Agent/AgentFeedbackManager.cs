using UnityEngine;

public class AgentFeedbackManager : MonoBehaviour
{
    [SerializeField]
    ChatFeedback _feedbackUI;

    public void ShowFeedbackUI(ChatLog chat) { _feedbackUI.ShowFeedbackUI(chat); }
    public void OnFeedbackButtonClicked(int feedbackFlag) { _feedbackUI.OnButtonClick(feedbackFlag); }
}