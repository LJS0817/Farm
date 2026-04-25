using TMPro;
using UnityEngine;

public class GoldUI : MonoBehaviour
{
    [SerializeField] private GoldManager goldManager;
    [SerializeField] private TMP_Text goldText;

    private void Awake()
    {
        if (goldText == null)
        {
            goldText = GetComponent<TMP_Text>();
        }
    }

    private void Start()
    {
        if (goldManager == null)
        {
            goldManager = FindFirstObjectByType<GoldManager>();
        }

        if (goldManager == null)
        {
            Debug.LogWarning("[GoldUI] GoldManager reference is missing.", this);
            return;
        }

        goldManager.GoldChanged += HandleGoldChanged;
        HandleGoldChanged(goldManager.GetGold());
    }

    private void OnDestroy()
    {
        if (goldManager != null)
        {
            goldManager.GoldChanged -= HandleGoldChanged;
        }
    }

    private void HandleGoldChanged(int currentGold)
    {
        if (goldText == null)
        {
            return;
        }

        goldText.SetText(currentGold.ToString());
    }
}
