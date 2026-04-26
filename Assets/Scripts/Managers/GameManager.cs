using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public RectTransform menu;
    public GameObject title_Tile;
    public GameObject title_Menu;
    public FarmLevelManager userInfo;
    [SerializeField] private GameStateAssembler gameStateAssembler;
    [SerializeField] private NewStartUI newStartUI;
    [SerializeField] private TileManager tileManager;
    [SerializeField] private AgentActionController agentActionController;
    [SerializeField] private AgentChatManager agentChatManager;
    [SerializeField] private Vector2Int startTileCoord = new Vector2Int(7, 4);

    private bool _isStartingGame;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (gameStateAssembler == null)
        {
            gameStateAssembler = FindFirstObjectByType<GameStateAssembler>();
        }

        if (newStartUI == null)
        {
            newStartUI = FindFirstObjectByType<NewStartUI>(FindObjectsInactive.Include);
        }

        if (tileManager == null)
        {
            tileManager = FindFirstObjectByType<TileManager>();
        }

        if (agentActionController == null)
        {
            agentActionController = FindFirstObjectByType<AgentActionController>();
        }

        if (agentChatManager == null)
        {
            agentChatManager = FindFirstObjectByType<AgentChatManager>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void GameStart()
    {
        if (_isStartingGame)
        {
            return;
        }

        if (Application.isEditor)
        {
            NewStart();
            return;
        }

        _isStartingGame = true;
        NetworkManager.Instance.PushConnectLoading();

        if (gameStateAssembler == null)
        {
            Debug.LogWarning("[GameManager] GameStateAssembler reference is missing. Starting with current scene state.", this);
            GameInit();
            FinishGameStart();
            return;
        }

        gameStateAssembler.GetData(
            onLoaded: () =>
            {
                GameInit();
                FinishGameStart();
            },
            onNewStart: () =>
            {
                NewStart();
                FinishGameStart();
            },
            onFailed: error =>
            {
                Debug.LogError($"[GameManager] Failed to start game: {error}", this);
                FinishGameStart();
            });
    }

    public void GameInit()
    {
        if (newStartUI != null)
        {
            newStartUI.Close();
        }

        ResetPlayerToStartTile();

        userInfo.OpenUI();
        menu.anchoredPosition = new Vector2(0, 60);
        title_Tile.SetActive(false);
        title_Menu.SetActive(false);
    }

    public void NewStart()
    {
        Debug.Log("새로운 시작", this);

        if (newStartUI == null)
        {
            Debug.LogWarning("[GameManager] NewStartUI reference is missing.", this);
            return;
        }

        newStartUI.Open(this);
    }

    public void ShowLogo()
    {
        if (newStartUI != null)
        {
            newStartUI.Close();
        }

        userInfo.CloseUI();
        menu.anchoredPosition = new Vector2(0, -300);
        title_Tile.SetActive(true);
        title_Menu.SetActive(true);
        _isStartingGame = false;
    }

    public void StartNewGameWithSeed(int worldSeed)
    {
        if (_isStartingGame)
        {
            return;
        }

        StartCoroutine(StartNewGameRoutine(worldSeed));
    }

    public void GameOff()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void FinishGameStart()
    {
        NetworkManager.Instance.PopConnectLoading();
        _isStartingGame = false;
    }

    private IEnumerator StartNewGameRoutine(int worldSeed)
    {
        _isStartingGame = true;
        NetworkManager.Instance.PushConnectLoading();

        if (newStartUI != null)
        {
            newStartUI.Close();
        }

        // 로딩 UI가 먼저 그려진 뒤 월드 재초기화를 시작하도록 한 프레임 넘긴다.
        yield return null;

        if (gameStateAssembler == null)
        {
            Debug.LogWarning("[GameManager] GameStateAssembler reference is missing. Starting without new world initialization.", this);
        }
        else
        {
            gameStateAssembler.StartNewGame(worldSeed);
        }

        if (agentChatManager != null)
        {
            agentChatManager.ClearChatHistory();
        }

        GameInit();
        FinishGameStart();
    }

    private void ResetPlayerToStartTile()
    {
        if (tileManager == null)
        {
            Debug.LogWarning("[GameManager] TileManager reference is missing. Cannot reset player start position.", this);
            return;
        }

        if (agentActionController == null)
        {
            Debug.LogWarning("[GameManager] AgentActionController reference is missing. Cannot reset player start position.", this);
            return;
        }

        Vector2Int spawnCoord = ResolveSpawnTileCoord();

        if (!tileManager.TryGetTile(spawnCoord, out TileData startTile) || startTile == null)
        {
            Debug.LogWarning($"[GameManager] Start tile {spawnCoord} could not be found.", this);
            return;
        }

        agentActionController.ResetToWorldPosition(startTile.transform.position);
    }

    private Vector2Int ResolveSpawnTileCoord()
    {
        if (tileManager.IsWalkable(startTileCoord))
        {
            return startTileCoord;
        }

        int maxRadius = 20;
        for (int radius = 1; radius <= maxRadius; radius++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                    {
                        continue;
                    }

                    Vector2Int candidate = new Vector2Int(startTileCoord.x + x, startTileCoord.y + y);
                    if (tileManager.IsWalkable(candidate))
                    {
                        Debug.LogWarning(
                            $"[GameManager] Start tile {startTileCoord} is not walkable. Using fallback tile {candidate}.",
                            this);
                        return candidate;
                    }
                }
            }
        }

        Debug.LogWarning($"[GameManager] No walkable fallback found near {startTileCoord}. Using original start tile.", this);
        return startTileCoord;
    }
}
