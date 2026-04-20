using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

// 씬 곳곳에 흩어진 런타임 상태를 읽어
// 서버 전송용 스냅샷 하나로 조립하는 역할을 담당한다.
public class GameStateAssembler : MonoBehaviour
{
    private const string DefaultTestUserId = "test-user";

    [SerializeField] private MiddleDB middleDB;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private TokenManager tokenManager;

    // 버튼 테스트용 메서드.
    // 현재 스냅샷을 JSON 파일로 저장하고 요약 정보를 콘솔에 출력한다.
    public void DebugCreateSnapshot()
    {
        GameStateSnapshot snapshot = CreateSnapshot(GetDefaultUserId());
        string savedPath = SaveSnapshotJsonToDesktop(snapshot);

        Debug.Log(
            $"Snapshot created | userId: {snapshot.userId}, " +
            $"tiles: {snapshot.tiles.Length}, " +
            $"inventory: {snapshot.inventory.Length}, " +
            $"currentToken: {snapshot.currentToken} | savedPath: {savedPath}",
            this);
    }

    // 버튼 테스트용 메서드.
    // 서버 헬스체크를 호출해 백엔드 연결 상태를 바로 확인한다.
    public void DebugCheckHealth()
    {
        APIController.Health.Check(
            onSuccess: response =>
            {
                Debug.Log(
                    $"Health check success | status: {response.status}, database: {response.database}, time: {response.time}",
                    this);
            },
            onError: error =>
            {
                Debug.LogError($"[GameStateAssembler] Health check failed: {error}", this);
            });
    }

    // 버튼 테스트용 메서드.
    // 현재 게임 상태 스냅샷을 백엔드에 업로드한다.
    public void DebugSendSnapshot()
    {
        GameStateSnapshot snapshot = CreateSnapshot(GetDefaultUserId());

        if (!TryValidateSnapshot(snapshot, out string validationError))
        {
            Debug.LogError($"[GameStateAssembler] Snapshot validation failed: {validationError}", this);
            return;
        }

        Debug.Log(
            $"Sending snapshot | userId: {snapshot.userId}, " +
            $"tiles: {snapshot.tiles.Length}, " +
            $"inventory: {snapshot.inventory.Length}, " +
            $"currentToken: {snapshot.currentToken}",
            this);

        APIController.Game.SendSnapshot(
            snapshot,
            onSuccess: response =>
            {
                Debug.Log(
                    $"Snapshot upload success | id: {response.id}, userId: {response.userId}, createdAt: {response.createdAt}, tileCount: {response.tileCount}",
                    this);
            },
            onError: error =>
            {
                Debug.LogError($"[GameStateAssembler] Snapshot upload failed: {error}", this);
            });
    }

    private void Awake()
    {
        // 직접 연결되지 않았을 때를 대비해 필요한 매니저를 자동으로 찾는다.
        if (middleDB == null)
        {
            middleDB = FindFirstObjectByType<MiddleDB>();
        }

        if (inventoryManager == null)
        {
            inventoryManager = FindFirstObjectByType<InventoryManager>();
        }

        if (tokenManager == null)
        {
            tokenManager = TokenManager.Instance != null
                ? TokenManager.Instance
                : FindFirstObjectByType<TokenManager>();
        }
    }

    // 현재 게임 상태를 서버에 보내기 쉬운 형태의 스냅샷으로 묶는다.
    public GameStateSnapshot CreateSnapshot(string userId)
    {
        return new GameStateSnapshot
        {
            userId = userId,
            tiles = BuildTileDtos(),
            inventory = BuildInventoryDtos(),
            currentToken = BuildCurrentToken()
        };
    }

    // MiddleDB에 들어 있는 전체 타일 상태를 전송용 DTO 배열로 변환한다.
    private TileStateDto[] BuildTileDtos()
    {
        if (middleDB == null)
        {
            Debug.LogWarning("[GameStateAssembler] MiddleDB reference is missing.", this);
            return Array.Empty<TileStateDto>();
        }

        middleDB.EnsureInitialized();

        List<TileStateDto> result = new List<TileStateDto>(middleDB.TileCount);

        for (int y = 0; y < middleDB.Height; y++)
        {
            for (int x = 0; x < middleDB.Width; x++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                MiddleDB.TileState state = middleDB.GetTileState(coord);

                if (state == null)
                {
                    continue;
                }

                result.Add(new TileStateDto
                {
                    id = state.id,
                    tileType = state.tileType.ToString(),
                    cropType = state.cropType.ToString(),
                    cropState = state.cropState.ToString(),
                    variantIndex = state.variantIndex
                });
            }
        }

        return result.ToArray();
    }

    // 인벤토리 슬롯 중 실제 아이템이 들어 있는 슬롯만 추려서 DTO로 변환한다.
    private InventoryItemDto[] BuildInventoryDtos()
    {
        if (inventoryManager == null)
        {
            Debug.LogWarning("[GameStateAssembler] InventoryManager reference is missing.", this);
            return Array.Empty<InventoryItemDto>();
        }

        List<InventoryItemDto> result = new List<InventoryItemDto>();

        foreach (InventorySlot slot in inventoryManager.slots)
        {
            if (slot == null || slot.IsEmpty || slot.item == null)
            {
                continue;
            }

            result.Add(new InventoryItemDto
            {
                itemId = slot.item.itemId,
                count = slot.count
            });
        }

        return result.ToArray();
    }

    // 현재 토큰 수치를 읽어 스냅샷에 포함한다.
    private int BuildCurrentToken()
    {
        if (tokenManager == null)
        {
            Debug.LogWarning("[GameStateAssembler] TokenManager reference is missing.", this);
            return 0;
        }

        return tokenManager.token;
    }

    private string GetDefaultUserId()
    {
        PlayerId playerId = NetworkManager.Instance.GetPlayerId();
        if (playerId != null && !string.IsNullOrWhiteSpace(playerId.userId))
        {
            return playerId.userId;
        }

        return DefaultTestUserId;
    }

    private bool TryValidateSnapshot(GameStateSnapshot snapshot, out string error)
    {
        error = null;

        if (snapshot == null)
        {
            error = "snapshot is null.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(snapshot.userId))
        {
            error = "userId is required.";
            return false;
        }

        if (snapshot.currentToken < 0)
        {
            error = "currentToken must be 0 or greater.";
            return false;
        }

        if (snapshot.inventory == null)
        {
            error = "inventory is required.";
            return false;
        }

        for (int i = 0; i < snapshot.inventory.Length; i++)
        {
            InventoryItemDto item = snapshot.inventory[i];
            if (item == null)
            {
                error = $"inventory[{i}] is null.";
                return false;
            }

            if (item.itemId < 0 || item.count < 0)
            {
                error = $"inventory[{i}] has invalid values.";
                return false;
            }
        }

        if (snapshot.tiles == null)
        {
            error = "tiles is required.";
            return false;
        }

        if (snapshot.tiles.Length != 135)
        {
            error = $"tiles length must be 135, but was {snapshot.tiles.Length}.";
            return false;
        }

        bool[] seenIds = new bool[135];

        for (int i = 0; i < snapshot.tiles.Length; i++)
        {
            TileStateDto tile = snapshot.tiles[i];
            if (tile == null)
            {
                error = $"tiles[{i}] is null.";
                return false;
            }

            if (tile.id < 0 || tile.id >= 135)
            {
                error = $"tiles[{i}].id must be between 0 and 134, but was {tile.id}.";
                return false;
            }

            if (seenIds[tile.id])
            {
                error = $"tiles[{i}].id {tile.id} is duplicated.";
                return false;
            }

            seenIds[tile.id] = true;

            if (string.IsNullOrWhiteSpace(tile.tileType)
                || string.IsNullOrWhiteSpace(tile.cropType)
                || string.IsNullOrWhiteSpace(tile.cropState))
            {
                error = $"tiles[{i}] has empty string fields.";
                return false;
            }
        }

        for (int id = 0; id < seenIds.Length; id++)
        {
            if (!seenIds[id])
            {
                error = $"tiles is missing id {id}.";
                return false;
            }
        }

        return true;
    }

    private string SaveSnapshotJsonToDesktop(GameStateSnapshot snapshot)
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string fileName = $"game_snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        string filePath = Path.Combine(desktopPath, fileName);
        string json = JsonConvert.SerializeObject(snapshot, Formatting.Indented);

        File.WriteAllText(filePath, json);
        return filePath;
    }
}

[Serializable]
// 서버로 전송할 게임 상태의 최상위 묶음.
public class GameStateSnapshot
{
    public string userId;
    public TileStateDto[] tiles;
    public InventoryItemDto[] inventory;
    public int currentToken;
}

[Serializable]
// 타일 하나를 서버에 전달하기 위한 최소 상태 정보.
public class TileStateDto
{
    public int id;
    public string tileType;
    public string cropType;
    public string cropState;
    public int variantIndex;
}

[Serializable]
// 인벤토리 아이템 한 종류의 수량 정보.
public class InventoryItemDto
{
    public int itemId;
    public int count;
}

[Serializable]
public class HealthCheckResponse
{
    public string status;
    public string database;
    public string time;
}

[Serializable]
public class SnapshotUploadResponse
{
    public string id;
    public string userId;
    public int currentToken;
    public int tileCount;
    public int inventoryCount;
    public string createdAt;
}
