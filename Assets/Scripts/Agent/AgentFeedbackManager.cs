using UnityEngine;

public class AgentFeedbackManager : MonoBehaviour
{
    [SerializeField]
    ChatFeedback _feedbackUI;

    public void ShowFeedbackUI(string instruct) { _feedbackUI.ShowFeedbackUI(instruct); }
}
