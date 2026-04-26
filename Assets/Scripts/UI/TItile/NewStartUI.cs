using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NewStartUI : MonoBehaviour
{
    public int worldseed;
    public TMP_Text worldseedText;
    [SerializeField] private TMP_InputField worldseedInput;

    private GameManager _gameManager;

    private void Awake()
    {
        if (worldseedInput == null)
        {
            worldseedInput = GetComponentInChildren<TMP_InputField>(true);
        }

        ConfigureSeedInput();
    }

    public void WorldResetRandombuton()
    {
        worldseed = Random.Range(1, int.MaxValue);
        ApplySeedToUI(worldseed);
    }

    public void FirstInit()
    {
        if (!TryReadSeedFromUI(out int parsedSeed))
        {
            WorldResetRandombuton();
            return;
        }

        worldseed = parsedSeed;
        ApplySeedToUI(worldseed);
    }

    public void CallGameInit()
    {
        if (!TryReadSeedFromUI(out int parsedSeed))
        {
            WorldResetRandombuton();
            parsedSeed = worldseed;
        }

        worldseed = parsedSeed;
        ApplySeedToUI(worldseed);

        if (_gameManager == null)
        {
            _gameManager = FindFirstObjectByType<GameManager>();
        }

        if (_gameManager == null)
        {
            Debug.LogWarning("[NewStartUI] GameManager reference is missing.", this);
            return;
        }

        _gameManager.StartNewGameWithSeed(worldseed);
    }

    public void Open(GameManager gameManager)
    {
        _gameManager = gameManager;
        gameObject.SetActive(true);
        FirstInit();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void ApplySeedToUI(int seed)
    {
        string seedText = seed.ToString();

        if (worldseedText != null)
        {
            worldseedText.text = seedText;
        }

        if (worldseedInput != null && worldseedInput.text != seedText)
        {
            worldseedInput.SetTextWithoutNotify(seedText);
        }
    }

    private bool TryReadSeedFromUI(out int parsedSeed)
    {
        string seedText = string.Empty;

        if (worldseedInput != null)
        {
            seedText = worldseedInput.text;
        }
        else if (worldseedText != null)
        {
            seedText = worldseedText.text;
        }

        return int.TryParse(seedText, out parsedSeed);
    }

    private void ConfigureSeedInput()
    {
        if (worldseedInput == null)
        {
            return;
        }

        worldseedInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        worldseedInput.characterValidation = TMP_InputField.CharacterValidation.Integer;
        worldseedInput.characterLimit = 10;
        worldseedInput.onValidateInput = ValidateSeedCharacter;
        worldseedInput.onValueChanged.RemoveListener(SanitizeSeedInput);
        worldseedInput.onValueChanged.AddListener(SanitizeSeedInput);
        SanitizeSeedInput(worldseedInput.text);
    }

    private void SanitizeSeedInput(string value)
    {
        if (worldseedInput == null || string.IsNullOrEmpty(value))
        {
            return;
        }

        char[] buffer = new char[value.Length];
        int length = 0;

        for (int i = 0; i < value.Length; i++)
        {
            char current = value[i];
            if (char.IsDigit(current))
            {
                buffer[length++] = current;

                if (length >= 10)
                {
                    break;
                }
            }
        }

        string sanitized = new string(buffer, 0, length);
        if (sanitized != value)
        {
            worldseedInput.SetTextWithoutNotify(sanitized);
        }
    }

    private char ValidateSeedCharacter(string text, int charIndex, char addedChar)
    {
        if (char.IsDigit(addedChar) && charIndex < 10)
        {
            return addedChar;
        }

        return '\0';
    }
}
