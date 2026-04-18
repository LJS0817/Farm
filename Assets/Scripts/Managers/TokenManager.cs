using System.Collections;
using TMPro;
using UnityEngine;

public class TokenManager : MonoBehaviour
{
    public static TokenManager Instance { get; private set; }
    const int MaxToken = 10;
    const int QuestionCost = 1;

    [SerializeField] TMP_Text _tokenText;
    [SerializeField] bool _dontDestroyOnLoad = true;
    [SerializeField] Color _defaultTextColor = Color.white;
    [SerializeField] Color _warningTextColor = Color.red;
    [SerializeField] float _flashInterval = 0.12f;
    [SerializeField] int _flashCount = 3;

    public int token = MaxToken;
    Coroutine _flashCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        if (_tokenText != null)
        {
            _defaultTextColor = _tokenText.color;
        }

        RefreshTokenText();
    }

    public void SetTokenText(TMP_Text tokenText)
    {
        _tokenText = tokenText;

        if (_tokenText != null)
        {
            _defaultTextColor = _tokenText.color;
        }

        RefreshTokenText();
    }

    public void SetToken(int value)
    {
        token = Mathf.Clamp(value, 0, MaxToken);
        RefreshTokenText();
    }

    public bool HasEnoughToken(int amount)
    {
        return token >= amount;
    }

    public bool AddToken(int amount)
    {
        if (token + amount < 0)
        {
            Debug.LogWarning($"토큰이 부족합니다. 현재 토큰: {token}, 요청 변화량: {amount}");
            PlayInsufficientTokenFeedback();
            return false;
        }

        token = Mathf.Clamp(token + amount, 0, MaxToken);
        RefreshTokenText();
        return true;
    }

    public bool UseToken(int amount)
    {
        if (amount < 0)
        {
            amount = -amount;
        }

        return AddToken(-amount);
    }

    public bool TrySpendQuestionToken()
    {
        return UseToken(QuestionCost);
    }

    private void RefreshTokenText()
    {
        if (_tokenText == null)
        {
            return;
        }

        _tokenText.SetText(token.ToString() + "/ " + MaxToken);
    }

    private void PlayInsufficientTokenFeedback()
    {
        if (_tokenText == null)
        {
            return;
        }

        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
        }

        _flashCoroutine = StartCoroutine(FlashTokenText());
    }

    private IEnumerator FlashTokenText()
    {
        for (int i = 0; i < _flashCount; i++)
        {
            _tokenText.color = _warningTextColor;
            yield return new WaitForSeconds(_flashInterval);
            _tokenText.color = _defaultTextColor;
            yield return new WaitForSeconds(_flashInterval);
        }

        _tokenText.color = _defaultTextColor;
        _flashCoroutine = null;
    }
}
