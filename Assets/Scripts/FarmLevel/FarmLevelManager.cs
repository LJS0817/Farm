using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FarmLevelManager : MonoBehaviour
{
   [Header("Default State")]
   [SerializeField] int _defaultFarmLevel = 1;
   [SerializeField] int _defaultFarmNowExp = 0;
   [SerializeField] int _expPerLevel = 100;

   [Header("Runtime State")]
   public int farmLevel;
   public int nowfarmExp;
   public int maxfarmExp;

   [Header("UI")]
   public GameObject root;
   public TMP_Text farmLevel_Text;
   public TMP_Text slider_Text; // nowfarmExp / maxFarmExp
   public Slider farmLevelSlider;
   public TMP_Text farmName;
    [SerializeField] private string loadingText = "Steam user loading...";
    [SerializeField] private string unavailableText = "Steam user unavailable";
    [SerializeField] private bool includePersonaName = true;
    [SerializeField] private float steamLabelRetryDelay = 0.5f;
    [SerializeField] private int steamLabelRetryCount = 10;

   bool _isInitialized;

    void Awake()
    {
        if (farmName == null)
        {
            farmName = GetComponent<TMP_Text>();
        }
    }

    void Start()
    {
        if (!_isInitialized)
        {
            InitializeFromBackend(null);
        }

        RefreshLabel();
        InvokeRepeating(nameof(TryRefreshLabelUntilReady), steamLabelRetryDelay, steamLabelRetryDelay);
    }

    public void InitializeFromBackend(FarmLevelStateDto state)
    {
        if (state == null || state.farmLevel <= 0 || state.farmNowExp < 0)
        {
            SetState(_defaultFarmLevel, _defaultFarmNowExp);
            return;
        }

        SetState(state.farmLevel, state.farmNowExp);
    }

    public void SetState(int level, int nowExp)
    {
        farmLevel = Mathf.Max(1, level);
        nowfarmExp = Mathf.Max(0, nowExp);
        maxfarmExp = GetMaxFarmEXP();

        ClampCurrentExpToMax();
        _isInitialized = true;
        RefreshUI();
    }

    public void GainFarmExp(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (!_isInitialized)
        {
            InitializeFromBackend(null);
        }

        nowfarmExp += amount;
        maxfarmExp = GetMaxFarmEXP();

        while (nowfarmExp >= maxfarmExp)
        {
            nowfarmExp -= maxfarmExp;
            farmLevel++;
            maxfarmExp = GetMaxFarmEXP();
        }

        RefreshUI();
    }

    public void SetFarmLevel(int value)
    {
        SetFarmLevel(value, keepCurrentExp: true);
    }

    public void SetFarmLevel(int value, bool keepCurrentExp)
    {
        if (!_isInitialized)
        {
            InitializeFromBackend(null);
        }

        int nextLevel = Mathf.Max(1, value);
        int nextExp = keepCurrentExp ? nowfarmExp : 0;
        SetState(nextLevel, nextExp);
    }

    public void IncreaseFarmLevel(int value = 1)
    {
        if (value <= 0)
        {
            return;
        }

        SetFarmLevel(farmLevel + value, keepCurrentExp: true);
    }

    public void RemoveFarmLevel(int value = 1)
    {
        if (value <= 0)
        {
            return;
        }

        SetFarmLevel(farmLevel - value, keepCurrentExp: true);
    }

    public int GetMaxFarmEXP()
    {
        int currentLevel = Mathf.Max(1, farmLevel);
        return currentLevel * Mathf.Max(1, _expPerLevel);
    }

    public FarmLevelStateDto CreateState()
    {
        if (!_isInitialized)
        {
            InitializeFromBackend(null);
        }

        return new FarmLevelStateDto
        {
            farmLevel = farmLevel,
            farmNowExp = nowfarmExp
        };
    }

    void ClampCurrentExpToMax()
    {
        maxfarmExp = Mathf.Max(1, maxfarmExp);
        nowfarmExp = Mathf.Clamp(nowfarmExp, 0, maxfarmExp);
    }

    void RefreshUI()
    {
        maxfarmExp = GetMaxFarmEXP();

        if (farmLevel_Text != null)
        {
            farmLevel_Text.SetText($"{farmLevel}");
        }

        if (slider_Text != null)
        {
            slider_Text.SetText($"{nowfarmExp} / {maxfarmExp}");
        }

        if (farmLevelSlider != null)
        {
            farmLevelSlider.maxValue = maxfarmExp;
            farmLevelSlider.value = nowfarmExp;
        }
    }

    public void OpenUI()
    {
        if (root == null)
        {
            Debug.LogWarning("[FarmLevelManager] root reference is missing.", this);
            return;
        }

        root.SetActive(true);
    }

    public void CloseUI()
    {
        if (root == null)
        {
            Debug.LogWarning("[FarmLevelManager] root reference is missing.", this);
            return;
        }

        root.SetActive(false);
    }

    public void RefreshLabel()
    {
        if (farmName == null)
        {
            Debug.LogWarning("SteamUserIdLabel requires a TMP_Text reference.", this);
            return;
        }

        SteamService steamService = SteamService.Instance;
        if (steamService == null)
        {
            farmName.text = loadingText;
            return;
        }

        steamService.RefreshUserData();

        if (!steamService.IsInitialized || string.IsNullOrEmpty(steamService.SteamId))
        {
            farmName.text = unavailableText;
            return;
        }

        if (includePersonaName && !string.IsNullOrEmpty(steamService.PersonaName))
        {
            farmName.text = $"{steamService.PersonaName}\n{steamService.SteamId}";
            return;
        }

        farmName.text = $"{steamService.SteamId}";
    }

    void TryRefreshLabelUntilReady()
    {
        if (farmName == null)
        {
            CancelInvoke(nameof(TryRefreshLabelUntilReady));
            return;
        }

        SteamService steamService = SteamService.Instance;
        if (steamService != null)
        {
            steamService.RefreshUserData();

            if (steamService.IsInitialized && !string.IsNullOrEmpty(steamService.SteamId))
            {
                RefreshLabel();
                CancelInvoke(nameof(TryRefreshLabelUntilReady));
                return;
            }
        }

        steamLabelRetryCount--;
        if (steamLabelRetryCount <= 0)
        {
            RefreshLabel();
            CancelInvoke(nameof(TryRefreshLabelUntilReady));
        }
    }
}

[System.Serializable]
public class FarmLevelStateDto
{
    public int farmLevel;
    public int farmNowExp;
}
