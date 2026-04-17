using TMPro;
using UnityEngine;

public class SteamUserIdLabel : MonoBehaviour
{
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private string loadingText = "Steam user loading...";
    [SerializeField] private string unavailableText = "Steam user unavailable";
    [SerializeField] private string prefix = "SteamID: ";
    [SerializeField] private bool includePersonaName = true;

    private void Awake()
    {
        if (targetText == null)
        {
            targetText = GetComponent<TMP_Text>();
        }
    }

    private void Start()
    {
        RefreshLabel();
    }

    public void RefreshLabel()
    {
        if (targetText == null)
        {
            Debug.LogWarning("SteamUserIdLabel requires a TMP_Text reference.", this);
            return;
        }

        SteamService steamService = SteamService.Instance;
        if (steamService == null)
        {
            targetText.text = loadingText;
            return;
        }

        steamService.RefreshUserData();

        if (!steamService.IsInitialized || string.IsNullOrEmpty(steamService.SteamId))
        {
            targetText.text = unavailableText;
            return;
        }

        if (includePersonaName && !string.IsNullOrEmpty(steamService.PersonaName))
        {
            targetText.text = $"{steamService.PersonaName}\n{prefix}{steamService.SteamId}";
            return;
        }

        targetText.text = $"{prefix}{steamService.SteamId}";
    }
}
