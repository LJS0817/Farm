using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class AgentActionValidator : MonoBehaviour
{
    // 엔진 쪽 최종 검증 레이어입니다. 여기 판단은 항상 현재 게임 상태를 기준으로 결정적이어야 합니다.
    [SerializeField] private TileManager _tileMng;
    [SerializeField] private InventoryManager _inventoryMng;
    [SerializeField] private AgentActionContextBuilder _contextBuilder;
    [SerializeField] private TokenManager _tokenManager;

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

        if (_contextBuilder == null)
        {
            _contextBuilder = GetComponent<AgentActionContextBuilder>();
        }

        if (_tokenManager == null)
        {
            _tokenManager = TokenManager.Instance != null
                ? TokenManager.Instance
                : FindFirstObjectByType<TokenManager>();
        }
    }

    public AgentValidationResult Validate(AgentIntentType intent, AgentFunctionArgumentsDto arguments)
    {
        AgentValidationResult result = new AgentValidationResult
        {
            intent = intent,
            currentPosition = GetCurrentPosition(),
            targetGridPos = arguments?.TargetGridPos != null ? arguments.TargetGridPos.ToVector2Int() : new Vector2Int(-1, -1),
            reasonCode = "UnsupportedIntent",
            status = AgentValidationStatus.Rejected,
        };

        if (!AgentLLMModelUtils.TryParseCropType(arguments?.Crop, out TileData.CropType cropType))
        {
            cropType = TileData.CropType.IsEmpty;
        }

        result.requestedCrop = cropType;

        // intent enum과 이 분기문은 같이 유지되어야 합니다. 각 분기는 실행 가능한 명령 목록이거나, 사용자에게 보여줄 거절 사유를 만들어야 합니다.
        switch (intent)
        {
            case AgentIntentType.GeneralChat:
                result.status = AgentValidationStatus.Informational;
                result.reasonCode = "GeneralChat";
                result.infoMessage = "일반 대화 응답입니다.";
                break;

            case AgentIntentType.QueryPosition:
                result.status = AgentValidationStatus.Informational;
                result.reasonCode = "PositionInfo";
                result.infoMessage = $"현재 위치는 ({result.currentPosition.x}, {result.currentPosition.y})입니다.";
                break;

            case AgentIntentType.QueryToken:
                ValidateTokenQuery(result);
                break;

            case AgentIntentType.QueryInventory:
                result.status = AgentValidationStatus.Informational;
                result.reasonCode = "InventoryInfo";
                result.infoMessage = "인벤토리 정보를 확인했습니다.";
                break;

            case AgentIntentType.QueryMap:
                result.status = AgentValidationStatus.Informational;
                result.reasonCode = "MapInfo";
                result.infoMessage = "맵 상태를 확인했습니다.";
                break;

            case AgentIntentType.QueryTile:
                ValidateTileQuery(result);
                break;

            case AgentIntentType.Move:
                ValidateMove(result);
                break;

            case AgentIntentType.Plant:
                ValidatePlant(result);
                break;

            case AgentIntentType.Harvest:
                ValidateHarvest(result);
                break;

            case AgentIntentType.Eat:
                ValidateEat(result);
                break;

            default:
                result.status = AgentValidationStatus.Rejected;
                result.reasonCode = "UnknownIntent";
                result.infoMessage = "무엇을 해주면 되는지 조금만 더 자세히 말해 주세요.";
                break;
        }

        return result;
    }

    public string BuildValidationJson(AgentValidationResult result)
    {
        // 답변 생성에는 planner의 초안이 아니라, 엔진이 검증을 끝낸 결과만 넘깁니다.
        List<AgentInventoryEntryDto> inventory = _contextBuilder != null
            ? _contextBuilder.BuildInventoryEntries()
            : AgentLLMModelUtils.BuildInventoryEntries(_inventoryMng);

        AgentTokenContextDto token = _contextBuilder != null
            ? _contextBuilder.BuildTokenContext()
            : BuildTokenContext();

        List<AgentTileSnapshotDto> visibleCrops = _contextBuilder != null
            ? _contextBuilder.BuildVisibleCropTiles()
            : new List<AgentTileSnapshotDto>();

        AgentValidationResultDto dto = result.ToDto(inventory, visibleCrops);
        dto.token = token;
        return JsonConvert.SerializeObject(dto, Formatting.Indented);
    }

    private void ValidateTileQuery(AgentValidationResult result)
    {
        if (!TryGetTargetTile(result.targetGridPos, out TileData tile))
        {
            result.status = AgentValidationStatus.Rejected;
            result.reasonCode = "InvalidTargetTile";
            result.infoMessage = "조회할 타일 좌표가 유효하지 않습니다.";
            return;
        }

        result.targetTile = tile;
        result.status = AgentValidationStatus.Informational;
        result.reasonCode = "TileInfo";
        result.infoMessage = "타일 정보를 확인했습니다.";
    }

    private void ValidateTokenQuery(AgentValidationResult result)
    {
        if (_tokenManager == null)
        {
            result.status = AgentValidationStatus.Rejected;
            result.reasonCode = "TokenManagerMissing";
            result.infoMessage = "지금은 토큰 정보를 확인할 수 없습니다.";
            return;
        }

        result.status = AgentValidationStatus.Informational;
        result.reasonCode = "TokenInfo";
        result.infoMessage = $"현재 남은 토큰은 {_tokenManager.CurrentToken}개입니다. 최대 {_tokenManager.MaxTokenCount}개까지 보유할 수 있어요.";
    }

    private AgentTokenContextDto BuildTokenContext()
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

    private void ValidateMove(AgentValidationResult result)
    {
        if (!IsInBounds(result.targetGridPos))
        {
            result.status = AgentValidationStatus.Rejected;
            result.reasonCode = "TargetOutOfBounds";
            result.infoMessage = "목표 좌표가 맵 범위를 벗어났습니다.";
            return;
        }

        if (!_tileMng.IsWalkable(result.targetGridPos))
        {
            result.status = AgentValidationStatus.Rejected;
            result.reasonCode = "TargetNotWalkable";
            result.infoMessage = "해당 위치로 이동할 수 없습니다.";
            return;
        }

        if (!_tileMng.TryGetTile(result.targetGridPos, out TileData tile) || tile == null)
        {
            result.status = AgentValidationStatus.Rejected;
            result.reasonCode = "TargetTileMissing";
            result.infoMessage = "목표 타일 정보를 찾을 수 없습니다.";
            return;
        }

        result.targetTile = tile;
        // 모든 검증을 통과한 뒤에만 명령을 확정해서, 실행부는 단순하게 유지합니다.
        result.commands.Add(new AgentCommand(ACTION_TYPE.MoveTo, result.targetGridPos, TileData.CropType.IsEmpty));
        result.status = AgentValidationStatus.Executable;
        result.reasonCode = "Executable";
        result.infoMessage = "이동 명령을 실행할 수 있습니다.";
    }

    private void ValidatePlant(AgentValidationResult result)
    {
        if (result.requestedCrop == TileData.CropType.IsEmpty)
        {
            result.status = AgentValidationStatus.Rejected;
            result.reasonCode = "UnknownCrop";
            result.infoMessage = "심을 작물을 확인할 수 없습니다.";
            return;
        }

        if (!TryGetTargetTile(result.targetGridPos, out TileData tile))
        {
            result.status = AgentValidationStatus.Rejected;
            result.reasonCode = "InvalidTargetTile";
            result.infoMessage = "심을 타일 좌표가 유효하지 않습니다.";
            return;
        }

        result.targetTile = tile;
        result.requiredItemName = $"{result.requestedCrop} seeds";
        result.currentItemCount = _inventoryMng != null ? _inventoryMng.GetItemCount(result.requiredItemName) : 0;

        if (result.currentItemCount <= 0)
        {
            result.status = AgentValidationStatus.Rejected;
            result.reasonCode = "MissingSeed";
            result.infoMessage = "필요한 씨앗이 부족합니다.";
            return;
        }

        if (!_tileMng.IsWalkable(result.targetGridPos))
        {
            result.status = AgentValidationStatus.Rejected;
            result.reasonCode = "TargetNotWalkable";
            result.infoMessage = "해당 위치로 이동할 수 없습니다.";
            return;
        }

        if (!tile.isFarmable)
        {
            result.status = AgentValidationStatus.Rejected;
            result.reasonCode = "TargetNotFarmable";
            result.infoMessage = "해당 타일에는 지금 작물을 심을 수 없습니다.";
            return;
        }

        result.commands.Add(new AgentCommand(ACTION_TYPE.MoveTo, result.targetGridPos, TileData.CropType.IsEmpty));
        result.commands.Add(new AgentCommand(ACTION_TYPE.Plant, result.targetGridPos, result.requestedCrop));
        result.status = AgentValidationStatus.Executable;
        result.reasonCode = "Executable";
        result.infoMessage = "심기 명령을 실행할 수 있습니다.";
    }

    private void ValidateHarvest(AgentValidationResult result)
    {
        if (!TryGetTargetTile(result.targetGridPos, out TileData tile))
        {
            result.status = AgentValidationStatus.Rejected;
            result.reasonCode = "InvalidTargetTile";
            result.infoMessage = "수확할 타일 좌표가 유효하지 않습니다.";
            return;
        }

        result.targetTile = tile;

        if (!_tileMng.IsWalkable(result.targetGridPos))
        {
            result.status = AgentValidationStatus.Rejected;
            result.reasonCode = "TargetNotWalkable";
            result.infoMessage = "해당 위치로 이동할 수 없습니다.";
            return;
        }

        if (tile.cropType == TileData.CropType.IsEmpty)
        {
            result.status = AgentValidationStatus.Rejected;
            result.reasonCode = "NothingToHarvest";
            result.infoMessage = "수확할 작물이 없습니다.";
            return;
        }

        if (tile.cropState != TileData.CropState.IsHarvastable)
        {
            result.status = AgentValidationStatus.Rejected;
            result.reasonCode = tile.cropState == TileData.CropState.IsGrowing ? "CropStillGrowing" : "NothingToHarvest";
            result.infoMessage = tile.cropState == TileData.CropState.IsGrowing
                ? "작물이 아직 자라고 있습니다."
                : "수확할 작물이 없습니다.";
            return;
        }

        result.commands.Add(new AgentCommand(ACTION_TYPE.MoveTo, result.targetGridPos, TileData.CropType.IsEmpty));
        result.commands.Add(new AgentCommand(ACTION_TYPE.Harvest, result.targetGridPos, TileData.CropType.IsEmpty));
        result.status = AgentValidationStatus.Executable;
        result.reasonCode = "Executable";
        result.infoMessage = "수확 명령을 실행할 수 있습니다.";
    }

    private void ValidateEat(AgentValidationResult result)
    {
        if (result.requestedCrop == TileData.CropType.IsEmpty)
        {
            result.status = AgentValidationStatus.Rejected;
            result.reasonCode = "UnknownCrop";
            result.infoMessage = "먹을 작물을 확인할 수 없습니다.";
            return;
        }

        result.requiredItemName = result.requestedCrop.ToString();
        result.currentItemCount = _inventoryMng != null ? _inventoryMng.GetItemCount(result.requiredItemName) : 0;

        if (result.currentItemCount <= 0)
        {
            result.status = AgentValidationStatus.Rejected;
            result.reasonCode = "MissingFood";
            result.infoMessage = "먹을 수 있는 작물이 없습니다.";
            return;
        }

        result.commands.Add(new AgentCommand(ACTION_TYPE.Eat, result.currentPosition, result.requestedCrop));
        result.status = AgentValidationStatus.Executable;
        result.reasonCode = "Executable";
        result.infoMessage = "먹기 명령을 실행할 수 있습니다.";
    }

    private bool TryGetTargetTile(Vector2Int coord, out TileData tile)
    {
        tile = null;
        return IsInBounds(coord) && _tileMng != null && _tileMng.TryGetTile(coord, out tile) && tile != null;
    }

    private bool IsInBounds(Vector2Int coord)
    {
        return _tileMng?.tiles != null
            && coord.x >= 0
            && coord.y >= 0
            && coord.x < _tileMng.tiles.GetLength(0)
            && coord.y < _tileMng.tiles.GetLength(1);
    }

    private Vector2Int GetCurrentPosition()
    {
        if (_contextBuilder != null && _contextBuilder.TryGetCurrentTile(out TileData tile) && tile != null)
        {
            return tile.coord;
        }

        return new Vector2Int(-1, -1);
    }
}
