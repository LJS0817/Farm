using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [SerializeField]
    InventoryManager _inventoryMng;
    [SerializeField]
    TileManager _tileMng;
    [SerializeField]
    bool _isBusy = false;
    [SerializeField]
    float _moveSpeed = 2f;

    Animator _ani;

    // "AniState" 문자열을 미리 해시값으로 변환하여 성능 최적화
    private readonly int _aniStateHash = Animator.StringToHash("AniState");

    //private Queue<AgentCommand> _commandQueue = new Queue<AgentCommand>();
    private Coroutine _actionCoroutine;

    Vector3 _agentScale;

    private void Start()
    {
        _agent = transform.parent;
        _ani = _agent.GetComponent<Animator>();
        _agentScale = _agent.localScale;
    }

    // 테스트용
    private void Update()
    {
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
    }

    public bool IsBusy() { return _isBusy; }

    public void ReceiveCommands(List<AgentCommand> commands, System.Action<List<AgentCommand>> callback)
    {
        if (!_isBusy)
        {
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
        _isBusy = true;
        for(int i = 0; i < commands.Count; i++)
        { 
            AgentCommand currentCommand = GetValidCommand(commands[i]);

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
        }

        _isBusy = false;
        _actionCoroutine = null;

        ChangeState(AgentState.Idle);
        ResetDirection();
        callback?.Invoke(commands);
    }

    private IEnumerator MoveToRoutine(Vector2Int targetPos)
    {
        if (_tileMng.TryGetTile(targetPos, out TileData tile))
        {
            ChangeState(AgentState.Walk);

            Vector2 targetWorldPos = tile.transform.position;
            SetFacingDirection(targetWorldPos.x);

            while (Vector2.Distance(_agent.position, targetWorldPos) > 0.01f)
            {
                _agent.position = Vector2.MoveTowards(_agent.position, targetWorldPos, _moveSpeed * Time.deltaTime);
                yield return null;
            }

            _agent.position = targetWorldPos;
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

        CropsData cropsData = CropManager.instance.GetCropData((int)cType - 1);
        _inventoryMng.RemoveItem($"{cType} seeds");

        yield return new WaitForSeconds(1f);
        bool success = _tileMng.PlantCrop(targetPos, cropsData);
    }

    private IEnumerator HarvestRoutine(Vector2Int targetPos)
    {
        ChangeState(AgentState.Work);
        ResetDirection();

        yield return new WaitForSeconds(1f);
        bool success = _tileMng.HarvestCrop(targetPos, _inventoryMng);
    }

    private IEnumerator EatRoutine(TileData.CropType cType)
    {
        ChangeState(AgentState.Work);
        ResetDirection();

        yield return new WaitForSeconds(1f);
        _inventoryMng.RemoveItem(CropManager.instance.GetCropData((int)cType - 1).harvestItem);
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
}