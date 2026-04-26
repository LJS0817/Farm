using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

[JsonConverter(typeof(StringEnumConverter))]
public enum ACTION_TYPE
{
    MoveTo,
    Plant,
    Harvest,
    Eat,
}

// 애니메이션 상태를 명확하게 관리하기 위한 Enum
public enum AgentState
{
    Idle = 0,
    Walk = 1,
    Work = 2
}

[System.Serializable]
public class AgentResponse
{
    public string answer;
    public List<AgentCommand> commands;

    public AgentResponse() { }
    public AgentResponse(string ans, List<AgentCommand> cmd)
    {
        answer = ans;
        commands = cmd;
    }

    public override string ToString()
    {
        string str = answer + "\n";
        foreach (AgentCommand cmd in commands) str = str + cmd.ToString() + "\n";
        return str;
    }
}

[System.Serializable]
[JsonConverter(typeof(AgentCommandConverter))]
public class AgentCommand
{
    [JsonConverter(typeof(StringEnumConverter))]
    public ACTION_TYPE Action;
    public Vector2Int TargetGridPos;
    [JsonConverter(typeof(StringEnumConverter))]
    public TileData.CropType Crop;

    public AgentCommand() { }
    public AgentCommand(ACTION_TYPE act, Vector2Int target, TileData.CropType cType)
    {
        Action = act;
        TargetGridPos = target;
        Crop = cType;
    }

    public override string ToString()
    {
        return $"{Action}({TargetGridPos.x}, {TargetGridPos.y}, {Crop})";
    }
}

public class AgentActionController : MonoBehaviour
{
    Transform _agent;
    AgentPathFinder _pathFinder;
    Transform _agentOffset;
    SpriteRenderer[] _agentRenderers;
    int[] _agentRendererRelativeOrders;

    [SerializeField]
    InventoryManager _inventoryMng;
    [SerializeField]
    TileManager _tileMng;
    [SerializeField]
    ProcessingUIController _processUI;
    [SerializeField]
    bool _isBusy = false;
    [SerializeField]
    float _moveSpeed = 2f;
    [SerializeField]
    string _sortingLayerName = "Default";
    [SerializeField]
    int _lineSortingBaseOrder = 11;

    Animator _ani;

    private readonly int _aniStateHash = Animator.StringToHash("AniState");

    //private Queue<AgentCommand> _commandQueue = new Queue<AgentCommand>();
    private Coroutine _actionCoroutine;

    Vector3 _agentScale;

    private void Start()
    {
        _agent = transform.parent;
        _ani = _agent.GetComponent<Animator>();
        _agentScale = _agent.localScale;
        _agentOffset = _agent.parent;
        _pathFinder = _agentOffset.GetComponent<AgentPathFinder>();
        AstarPath.active.Scan();
        CacheAgentRenderers();
        UpdateCharacterSorting();
    }

    // 테스트용
    private void Update()
    {
        UpdateCharacterSorting();

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _inventoryMng.AddItem(_inventoryMng.itemDatabase[2]);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _inventoryMng.AddItem(_inventoryMng.itemDatabase[3]);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            _inventoryMng.AddItem(_inventoryMng.itemDatabase[0]);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            _inventoryMng.AddItem(_inventoryMng.itemDatabase[1]);
        }
        if (Input.GetMouseButtonDown(1))
        {
            if(_tileMng.TryGetTileFromWorldPosition(Camera.main.ScreenToWorldPoint(Input.mousePosition), out TileData tile))
            {
                StartCoroutine(MoveToRoutine(tile.coord));
            }
        }
    }

    public bool IsBusy() { return _isBusy; }

    public void ReceiveCommands(List<AgentCommand> commands, System.Action<List<AgentCommand>> callback)
    {
        if (!_isBusy)
        {
            UnityEngine.Debug.Log($"[AI Timing] Action.ReceiveCommands: {commands.Count} command(s)");
            _actionCoroutine = StartCoroutine(ProcessCommandsCoroutine(commands, callback));
        }
    }

    AgentCommand GetValidCommand(AgentCommand cmd)
    {
        if (cmd.Action != ACTION_TYPE.Plant && cmd.Action != ACTION_TYPE.Eat && cmd.Crop != TileData.CropType.IsEmpty)
        {
            cmd.Crop = TileData.CropType.IsEmpty;
        }
        return cmd;
    }

    private IEnumerator ProcessCommandsCoroutine(List<AgentCommand> commands, System.Action<List<AgentCommand>> callback)
    {
        Stopwatch totalStopwatch = Stopwatch.StartNew();
        _isBusy = true;
        for(int i = 0; i < commands.Count; i++)
        { 
            AgentCommand currentCommand = GetValidCommand(commands[i]);
            Stopwatch commandStopwatch = Stopwatch.StartNew();

            switch (currentCommand.Action)
            {
                case ACTION_TYPE.MoveTo:
                    yield return StartCoroutine(MoveToRoutine(currentCommand.TargetGridPos));
                    break;
                case ACTION_TYPE.Plant:
                    yield return StartCoroutine(PlantRoutine(currentCommand.TargetGridPos, currentCommand.Crop));
                    break;
                case ACTION_TYPE.Harvest:
                    yield return StartCoroutine(HarvestRoutine(currentCommand.TargetGridPos));
                    break;
                case ACTION_TYPE.Eat:
                    yield return StartCoroutine(EatRoutine(currentCommand.Crop));
                    break;
            }

            commandStopwatch.Stop();
            UnityEngine.Debug.Log($"[AI Timing] Action.{currentCommand.Action}: {commandStopwatch.ElapsedMilliseconds}ms | command=\"{currentCommand}\"");
        }

        _isBusy = false;
        _actionCoroutine = null;

        ChangeState(AgentState.Idle);
        ResetDirection();
        totalStopwatch.Stop();
        UnityEngine.Debug.Log($"[AI Timing] Action.Total: {totalStopwatch.ElapsedMilliseconds}ms | commandCount={commands.Count}");
        callback?.Invoke(commands);
    }

    private IEnumerator MoveToRoutine(Vector2Int targetPos)
    {
        if (_tileMng.IsWalkable(targetPos) && _tileMng.TryGetTile(targetPos, out TileData tile))
        {
            ChangeState(AgentState.Walk);

            Vector2 targetWorldPos = tile.transform.position;
            SetFacingDirection(targetWorldPos.x);

            bool isMovementDone = false;

            _pathFinder.MoveToTarget(targetWorldPos, _moveSpeed, () =>
            {
                isMovementDone = true;
            });

            yield return new WaitUntil(() => isMovementDone);

            _agentOffset.position = targetWorldPos;
        }
        else
        {
            yield return null;
        }
    }

    private IEnumerator PlantRoutine(Vector2Int targetPos, TileData.CropType cType)
    {
        ChangeState(AgentState.Work);
        ResetDirection();

        if (CropManager.instance == null)
        {
            yield break;
        }

        CropsData cropsData = CropManager.instance.GetCropData(cType);
        if (cropsData == null)
        {
            yield break;
        }

        string seedName = $"{cType} seeds";
        if (!_inventoryMng.RemoveItem(seedName))
        {
            yield break;
        }

        if (_processUI != null)
            yield return StartCoroutine(_processUI.ProcessTaskRoutine("Planting", 2f));
        else
            yield return new WaitForSeconds(2f);

        bool success = _tileMng.PlantCrop(targetPos, cropsData);
        if (!success)
        {
            _inventoryMng.AddItem(_inventoryMng.GetItemSoWithName(seedName));
        }
    }

    private IEnumerator HarvestRoutine(Vector2Int targetPos)
    {
        ChangeState(AgentState.Work);
        ResetDirection();

        if (_processUI != null)
            yield return StartCoroutine(_processUI.ProcessTaskRoutine("Harvesting", 2f));
        else
            yield return new WaitForSeconds(2f);

        bool success = _tileMng.HarvestCrop(targetPos, _inventoryMng);
    }

    private IEnumerator EatRoutine(TileData.CropType cType)
    {
        ChangeState(AgentState.Work);
        ResetDirection();

        if (CropManager.instance == null)
        {
            yield break;
        }

        CropsData cropData = CropManager.instance.GetCropData(cType);
        if (cropData == null || cropData.harvestItem == null)
        {
            yield break;
        }

        if (_processUI != null)
            yield return StartCoroutine(_processUI.ProcessTaskRoutine("Eating", 1f));
        else
            yield return new WaitForSeconds(1f);

        _inventoryMng.RemoveItem(cropData.harvestItem);
    }


    private void ChangeState(AgentState newState)
    {
        if (_ani.GetInteger(_aniStateHash) != (int)newState)
        {
            _ani.SetInteger(_aniStateHash, (int)newState);
        }
    }

    private void SetFacingDirection(float targetX)
    {
        Vector3 scale = _agent.localScale;

        float desiredX = (targetX < _agent.position.x) ? -_agentScale.x : _agentScale.x;

        if (scale.x != desiredX)
        {
            scale.x = desiredX;
            _agent.localScale = scale;
        }
    }

    private void ResetDirection()
    {
        Vector3 scale = _agent.localScale;

        if (scale.x != _agentScale.x)
        {
            scale.x = _agentScale.x;
            _agent.localScale = scale;
        }
    }

    private void CacheAgentRenderers()
    {
        if (_agent == null)
        {
            return;
        }

        _agentRenderers = _agent.GetComponentsInChildren<SpriteRenderer>(true);
        if (_agentRenderers == null || _agentRenderers.Length == 0)
        {
            _agentRendererRelativeOrders = System.Array.Empty<int>();
            return;
        }

        int minOrder = _agentRenderers[0].sortingOrder;
        for (int i = 1; i < _agentRenderers.Length; i++)
        {
            if (_agentRenderers[i].sortingOrder < minOrder)
            {
                minOrder = _agentRenderers[i].sortingOrder;
            }
        }

        _agentRendererRelativeOrders = new int[_agentRenderers.Length];
        for (int i = 0; i < _agentRenderers.Length; i++)
        {
            _agentRendererRelativeOrders[i] = _agentRenderers[i].sortingOrder - minOrder;
        }
    }

    private void UpdateCharacterSorting()
    {
        if (_agent == null || _tileMng == null)
        {
            return;
        }

        if (_agentRenderers == null || _agentRenderers.Length == 0)
        {
            CacheAgentRenderers();
        }

        if (_agentRenderers == null || _agentRenderers.Length == 0)
        {
            return;
        }

        Vector3 sortingWorldPosition = _agentOffset.position;

        if (!_tileMng.TryGetTileFromWorldPosition(sortingWorldPosition, out TileData currentTile) || currentTile == null)
        {
            return;
        }

        int targetBaseOrder = _lineSortingBaseOrder + currentTile.coord.y;
        for (int i = 0; i < _agentRenderers.Length; i++)
        {
            _agentRenderers[i].sortingLayerName = _sortingLayerName;
            _agentRenderers[i].sortingOrder = targetBaseOrder + _agentRendererRelativeOrders[i];
        }
    }
}
