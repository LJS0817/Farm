using System;
using TMPro;
using UnityEngine;

public class GoldManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text goldTextUI;

    [Header("Default State")]
    [SerializeField] private int defaultGold = 0;

    [Header("Runtime State")]
    [SerializeField] private int gold;

    private bool isInitialized;

    public event Action<int> GoldChanged;

    public int Gold => gold;

    private void Start()
    {
        if (!isInitialized)
        {
            InitializeFromBackend(null);
        }
    }

    public void InitializeFromBackend(GoldStateDto state)
    {
        if (state == null || state.gold < 0)
        {
            SetGold(defaultGold);
            return;
        }

        SetGold(state.gold);
    }

    public int GetGold()
    {
        return gold;
    }

    public void SetGold(int value)
    {
        gold = Mathf.Max(0, value);
        isInitialized = true;
        RefreshUI();
        GoldChanged?.Invoke(gold);
    }

    public void AddGold(int value)
    {
        if (value == 0)
        {
            return;
        }

        SetGold(gold + value);
    }

    public bool TrySpendGold(int value)
    {
        if (value <= 0)
        {
            return true;
        }

        if (gold < value)
        {
            return false;
        }

        SetGold(gold - value);
        return true;
    }

    public GoldStateDto CreateState()
    {
        if (!isInitialized)
        {
            InitializeFromBackend(null);
        }

        return new GoldStateDto
        {
            gold = gold
        };
    }

    public void RefreshUI()
    {
        if (goldTextUI == null)
        {
            return;
        }

        goldTextUI.SetText(gold.ToString());
    }
}

[Serializable]
public class GoldStateDto
{
    public int gold;
}
