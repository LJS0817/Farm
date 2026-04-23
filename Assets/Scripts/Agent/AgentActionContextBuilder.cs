using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;

public class AgentActionContextBuilder : MonoBehaviour
{
    // 현재 엔진 상태를, planner LLM이 안정적으로 읽을 수 있는 JSON 스냅샷으로 변환합니다.
    [SerializeField] private TileManager _tileMng;
    [SerializeField] private InventoryManager _inventoryMng;

    private static readonly Regex CoordinateRegex =
        new Regex(@"\(?\s*(?<x>-?\d+)\s*[,，]\s*(?<y>-?\d+)\s*\)?", RegexOptions.Compiled);

    private void Awake()
    {
        if (_tileMng == null)
        {
            _tileMng = FindFirstObjectByType<TileManager>();
        }

        if (_inventoryMng == null)
        {
            _inventoryMng = FindFirstObjectByType<InventoryManager>();
        }
    }

    public string BuildPlanningContextJson(AgentIntentType intent, string userInput)
    {
        AgentPlanningContextDto context = new AgentPlanningContextDto
        {
            intent = intent.ToString(),
            userInput = userInput,
            currentPosition = null,
            map = BuildMapContext(),
            inventory = AgentLLMModelUtils.BuildInventoryEntries(_inventoryMng),
            ontology = OntologyManager.Instance != null ? OntologyManager.Instance.GetOntologyData() : null,
        };

        if (TryGetCurrentTile(out TileData currentTile))
        {
            context.currentPosition = GridPositionDto.FromVector2Int(currentTile.coord);
            context.currentTile = AgentLLMModelUtils.CreateTileSnapshot(currentTile, _tileMng.IsWalkable(currentTile.coord));
            context.adjacentTiles = BuildAdjacentTiles(currentTile.coord);
        }

        // 명시 좌표는 별도 슬롯에 넣어서, planner가 자유 텍스트에서 다시 좌표를 추론하지 않도록 합니다.
        if (TryGetReferencedCoordinate(userInput, out Vector2Int referencedCoord))
        {
            context.referencedCoordinate = GridPositionDto.FromVector2Int(referencedCoord);

            if (_tileMng != null && _tileMng.TryGetTile(referencedCoord, out TileData referencedTile) && referencedTile != null)
            {
                context.referencedTile = AgentLLMModelUtils.CreateTileSnapshot(referencedTile, _tileMng.IsWalkable(referencedCoord));
            }
        }

        context.nonEmptyTiles = BuildVisibleCropTiles();
        return JsonConvert.SerializeObject(context, Formatting.Indented);
    }

    public List<AgentTileSnapshotDto> BuildVisibleCropTiles()
    {
        List<AgentTileSnapshotDto> tiles = new List<AgentTileSnapshotDto>();

        if (_tileMng == null)
        {
            return tiles;
        }

        List<TileData> nonEmptyTiles = _tileMng.GetNonEmptyTiles();
        foreach (TileData tile in nonEmptyTiles)
        {
            tiles.Add(AgentLLMModelUtils.CreateTileSnapshot(tile, _tileMng.IsWalkable(tile.coord)));
        }

        return tiles;
    }

    public List<AgentInventoryEntryDto> BuildInventoryEntries()
    {
        return AgentLLMModelUtils.BuildInventoryEntries(_inventoryMng);
    }

    public bool TryGetCurrentTile(out TileData tile)
    {
        tile = null;
        return _tileMng != null
            && _tileMng.TryGetTileFromWorldPosition(transform.position, out tile)
            && tile != null;
    }

    private AgentMapContextDto BuildMapContext()
    {
        AgentMapContextDto map = new AgentMapContextDto();

        if (_tileMng?.tiles == null)
        {
            return map;
        }

        int width = _tileMng.tiles.GetLength(0);
        int height = _tileMng.tiles.GetLength(1);

        map.width = width;
        map.height = height;
        map.topLeft = new GridPositionDto { x = 0, y = 0 };
        map.topRight = new GridPositionDto { x = Mathf.Max(0, width - 1), y = 0 };
        map.bottomLeft = new GridPositionDto { x = 0, y = Mathf.Max(0, height - 1) };
        map.bottomRight = new GridPositionDto { x = Mathf.Max(0, width - 1), y = Mathf.Max(0, height - 1) };
        return map;
    }

    private List<AgentTileSnapshotDto> BuildAdjacentTiles(Vector2Int center)
    {
        // 상하좌우만 넣어도 대부분의 상대 위치 명령을 커버할 수 있어서, 컨텍스트 비용을 과하게 키우지 않습니다.
        List<AgentTileSnapshotDto> tiles = new List<AgentTileSnapshotDto>();
        if (_tileMng == null)
        {
            return tiles;
        }

        Vector2Int[] offsets = new[]
        {
            new Vector2Int(0, -1),
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(-1, 0),
        };

        foreach (Vector2Int offset in offsets)
        {
            Vector2Int coord = center + offset;
            if (_tileMng.TryGetTile(coord, out TileData tile) && tile != null)
            {
                tiles.Add(AgentLLMModelUtils.CreateTileSnapshot(tile, _tileMng.IsWalkable(coord)));
            }
        }

        return tiles;
    }

    private bool TryGetReferencedCoordinate(string userInput, out Vector2Int coord)
    {
        coord = Vector2Int.zero;

        if (string.IsNullOrWhiteSpace(userInput))
        {
            return false;
        }

        Match match = CoordinateRegex.Match(userInput);
        if (!match.Success
            || !int.TryParse(match.Groups["x"].Value, out int x)
            || !int.TryParse(match.Groups["y"].Value, out int y))
        {
            return false;
        }

        coord = new Vector2Int(x, y);
        return true;
    }
}
