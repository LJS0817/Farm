using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;

public class AgentActionContextBuilder : MonoBehaviour
{
    // 현재 엔진 상태를, planner LLM이 안정적으로 읽을 수 있는 JSON 스냅샷으로 변환합니다.
    [SerializeField] private TileManager _tileMng;
    [SerializeField] private InventoryManager _inventoryMng;
    [SerializeField] private TokenManager _tokenManager;

    private static readonly Regex CoordinateRegex =
        new Regex(@"\(?\s*(?<x>-?\d+)\s*[,，]\s*(?<y>-?\d+)\s*\)?", RegexOptions.Compiled);
    private static readonly Regex RelativeMoveRegex =
        new Regex(@"(?:(?<countBefore>\d+|한|두|세|네|다섯|여섯|일곱|여덟|아홉|열)\s*칸\s*)?(?<direction>오른쪽|왼쪽|위|위로|아래|아래로)(?:\s*으로?)?(?:\s*(?<countAfter>\d+|한|두|세|네|다섯|여섯|일곱|여덟|아홉|열)\s*칸)?\s*(?:가|이동|가자|가보자|이동하자|가줘)?",
            RegexOptions.Compiled);

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

        if (_tokenManager == null)
        {
            _tokenManager = TokenManager.Instance != null
                ? TokenManager.Instance
                : FindFirstObjectByType<TokenManager>();
        }
    }

    public string BuildPlanningContextJson(AgentIntentType intent, string userInput)
    {
        AgentPlanningContextDto context = new AgentPlanningContextDto
        {
            intent = intent.ToString(),
            userInput = userInput,
            currentPosition = null,
            token = BuildTokenContext(),
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
        if (TryExtractReferencedCoordinate(userInput, out Vector2Int referencedCoord))
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

    public AgentTokenContextDto BuildTokenContext()
    {
        if (_tokenManager == null)
        {
            return null;
        }

        return new AgentTokenContextDto
        {
            current = _tokenManager.CurrentToken,
            max = _tokenManager.MaxTokenCount,
            questionCost = _tokenManager.QuestionTokenCost,
        };
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

    public bool TryExtractReferencedCoordinate(string userInput, out Vector2Int coord)
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

    public bool TryResolveRelativeMoveTarget(string userInput, out Vector2Int target)
    {
        target = Vector2Int.zero;

        if (string.IsNullOrWhiteSpace(userInput) || !TryGetCurrentTile(out TileData currentTile))
        {
            return false;
        }

        Match match = RelativeMoveRegex.Match(userInput);
        if (!match.Success)
        {
            return false;
        }

        Vector2Int direction = ParseDirection(match.Groups["direction"].Value);
        if (direction == Vector2Int.zero)
        {
            return false;
        }

        string rawCount = match.Groups["countBefore"].Success && !string.IsNullOrWhiteSpace(match.Groups["countBefore"].Value)
            ? match.Groups["countBefore"].Value
            : match.Groups["countAfter"].Value;

        int stepCount = ParseStepCount(rawCount);
        target = currentTile.coord + direction * Mathf.Max(1, stepCount);
        return true;
    }

    public bool TryResolveCornerTarget(string userInput, out Vector2Int target)
    {
        target = Vector2Int.zero;

        if (string.IsNullOrWhiteSpace(userInput) || _tileMng?.tiles == null)
        {
            return false;
        }

        string normalized = userInput.Replace(" ", string.Empty);
        int width = _tileMng.tiles.GetLength(0);
        int height = _tileMng.tiles.GetLength(1);

        bool isTopLeft =
            normalized.Contains("좌상단")
            || normalized.Contains("왼쪽맨위")
            || normalized.Contains("왼쪽위")
            || normalized.Contains("맨왼쪽위")
            || normalized.Contains("좌측상단");

        bool isTopRight =
            normalized.Contains("우상단")
            || normalized.Contains("오른쪽맨위")
            || normalized.Contains("오른쪽위")
            || normalized.Contains("맨오른쪽위")
            || normalized.Contains("우측상단");

        bool isBottomLeft =
            normalized.Contains("좌하단")
            || normalized.Contains("왼쪽맨아래")
            || normalized.Contains("왼쪽아래")
            || normalized.Contains("맨왼쪽아래")
            || normalized.Contains("좌측하단");

        bool isBottomRight =
            normalized.Contains("우하단")
            || normalized.Contains("오른쪽맨아래")
            || normalized.Contains("오른쪽아래")
            || normalized.Contains("맨오른쪽아래")
            || normalized.Contains("우측하단");

        if (isTopLeft)
        {
            target = new Vector2Int(0, 0);
            return true;
        }

        if (isTopRight)
        {
            target = new Vector2Int(Mathf.Max(0, width - 1), 0);
            return true;
        }

        if (isBottomLeft)
        {
            target = new Vector2Int(0, Mathf.Max(0, height - 1));
            return true;
        }

        if (isBottomRight)
        {
            target = new Vector2Int(Mathf.Max(0, width - 1), Mathf.Max(0, height - 1));
            return true;
        }

        return false;
    }

    private static Vector2Int ParseDirection(string rawDirection)
    {
        return rawDirection switch
        {
            "오른쪽" => Vector2Int.right,
            "왼쪽" => Vector2Int.left,
            "위" => new Vector2Int(0, -1),
            "위로" => new Vector2Int(0, -1),
            "아래" => new Vector2Int(0, 1),
            "아래로" => new Vector2Int(0, 1),
            _ => Vector2Int.zero,
        };
    }

    private static int ParseStepCount(string rawCount)
    {
        if (string.IsNullOrWhiteSpace(rawCount))
        {
            return 1;
        }

        if (int.TryParse(rawCount, out int numericCount))
        {
            return numericCount;
        }

        return rawCount switch
        {
            "한" => 1,
            "두" => 2,
            "세" => 3,
            "네" => 4,
            "다섯" => 5,
            "여섯" => 6,
            "일곱" => 7,
            "여덟" => 8,
            "아홉" => 9,
            "열" => 10,
            _ => 1,
        };
    }
}
