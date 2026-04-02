using UnityEngine;

public class CharacterTouchInteraction : MonoBehaviour
{
    public GameObject chatUI;

    public void HandleClick()
    {
        Debug.Log("Character touched", this);

        if (chatUI == null)
        {
            Debug.LogWarning("chatUI reference is missing.", this);
            return;
        }

        RectTransform rectTransform = chatUI.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogWarning("chatUI does not have a RectTransform.", chatUI);
            return;
        }

        rectTransform.anchoredPosition = new Vector2(-30f, 30f);
    }

    public void HideChatUI()
    {
        if (chatUI == null)
        {
            return;
        }

        RectTransform rectTransform = chatUI.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchoredPosition = new Vector2(-3000f, 3000f);
    }
}
