using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Title_Text : MonoBehaviour
{
    [SerializeField] private string textPresetTableName = "Title";
    [SerializeField] private string[] textPresetKeys =
    {
        "tip_welcome",
        "tip_crop_growth_times",
        "tip_harvest_to_inventory",
        "tip_harvest_rewards",
        "tip_touch_tiles",
        "tip_inventory_full",
        "tip_complete_farm",
        "tip_first_step"
    };
    public TMP_Text ai_Text;
    public GameObject loadingObject;

    [SerializeField] private float loadingDuration = 1f;

    private bool isShowingText;

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += HandleSelectedLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= HandleSelectedLocaleChanged;
    }

    private void Start()
    {
        if (loadingObject != null)
        {
            loadingObject.SetActive(false);
        }

        ShowRandomTitleText();
    }

    public void ShowRandomTitleText()
    {
        if (isShowingText)
        {
            return;
        }

        StartCoroutine(ShowRandomTitleTextRoutine());
    }

    private void HandleSelectedLocaleChanged(Locale locale)
    {
        if (!isShowingText && ai_Text != null && !string.IsNullOrEmpty(ai_Text.text))
        {
            ShowRandomTitleText();
        }
    }

    private IEnumerator ShowRandomTitleTextRoutine()
    {
        isShowingText = true;
        if (ai_Text != null)
        {
            ai_Text.text = "";
        }

        if (loadingObject != null)
        {
            loadingObject.SetActive(true);
        }

        yield return new WaitForSeconds(loadingDuration);

        if (loadingObject != null)
        {
            loadingObject.SetActive(false);
        }

        if (ai_Text != null && textPresetKeys != null && textPresetKeys.Length > 0)
        {
            int randomIndex = Random.Range(0, textPresetKeys.Length);
            yield return SetLocalizedPresetText(textPresetKeys[randomIndex]);
        }

        isShowingText = false;
    }

    private IEnumerator SetLocalizedPresetText(string entryKey)
    {
        if (string.IsNullOrEmpty(textPresetTableName) || string.IsNullOrEmpty(entryKey))
        {
            yield break;
        }

        AsyncOperationHandle<string> handle = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(textPresetTableName, entryKey);
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded && !string.IsNullOrEmpty(handle.Result))
        {
            ai_Text.text = handle.Result;
        }
        else
        {
            ai_Text.text = entryKey;
        }
    }
}
