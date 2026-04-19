using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

// 씬 곳곳에 흩어진 런타임 상태를 읽어
// 서버 전송용 스냅샷 하나로 조립하는 역할을 담당한다.
public class GameStateAssembler : MonoBehaviour
{
    [SerializeField] private MiddleDB middleDB;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private TokenManager tokenManager;

    // 버튼 테스트용 메서드.
    // 현재 스냅샷을 JSON으로 만들어 콘솔에 출력한다.
    public void DebugCreateSnapshot()
    {
        GameStateSnapshot snapshot = CreateSnapshot("test-user");

        Debug.Log(
            $"Snapshot created | userId: {snapshot.userId}, " +
            $"tiles: {snapshot.tiles.Length}, " +
            $"inventory: {snapshot.inventory.Length}, " +
            $"currentToken: {snapshot.currentToken}",
            this);
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
                    x = state.coord.x,
                    y = state.coord.y,
                    tileType = state.tileType.ToString(),
                    cropType = state.cropType.ToString(),
                    cropState = state.cropState.ToString(),
                    isFarmable = state.isFarmable,
                    growRemaining = state.growDuration,
                    growMax = state.maxTime
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
    public int x;
    public int y;
    public string tileType;
    public string cropType;
    public string cropState;
    public bool isFarmable;
    public float growRemaining;
    public float growMax;
}

[Serializable]
// 인벤토리 아이템 한 종류의 수량 정보.
public class InventoryItemDto
{
    public int itemId;
    public int count;
}
