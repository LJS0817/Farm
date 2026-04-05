using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

[JsonConverter(typeof(StringEnumConverter))]
public enum ACTION_TYPE
{
    [EnumMember(Value = "MoveTo")]
    E_MOVETO,

    [EnumMember(Value = "Plant")]
    E_PLANT,

    [EnumMember(Value = "Harvest")]
    E_HARVEST,

}

[System.Serializable]
public class AgentResponse
{
    public string answer;                 // LLM의 텍스트 답변
    public List<AgentCommand> commands;   // 실행할 행동 리스트
}

// JSON을 파싱하여 생성할 데이터 클래스
[System.Serializable]
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

    private Queue<AgentCommand> _commandQueue = new Queue<AgentCommand>();
    private Coroutine _actionCoroutine;

    private void Start()
    {
        _agent = transform.parent;
    }

    public bool IsBusy() { return _isBusy; }

    public void ReceiveCommands(List<AgentCommand> commands)
    {
        foreach (var cmd in commands)
        {
            _commandQueue.Enqueue(cmd);
        }

        if (!_isBusy)
        {
            _actionCoroutine = StartCoroutine(ProcessCommandsCoroutine());
        }
    }

    private IEnumerator ProcessCommandsCoroutine()
    {
        _isBusy = true;

        while (_commandQueue.Count > 0)
        {
            AgentCommand currentCommand = _commandQueue.Dequeue();

            switch (currentCommand.Action)
            {
                case ACTION_TYPE.E_MOVETO:
                    yield return StartCoroutine(MoveToRoutine(currentCommand.TargetGridPos));
                    break;
                case ACTION_TYPE.E_PLANT:
                    yield return StartCoroutine(PlantRoutine(currentCommand.TargetGridPos, currentCommand.Crop));
                    break;
                case ACTION_TYPE.E_HARVEST:
                    yield return StartCoroutine(HarvestRoutine(currentCommand.TargetGridPos));
                    break;
            }
        }

        _isBusy = false;
        _actionCoroutine = null;
    }

    private IEnumerator MoveToRoutine(Vector2Int targetPos)
    {
        if(_tileMng.TryGetTile(targetPos, out TileData tile))
        {
            Vector2 targetWorldPos = tile.transform.position;
            while (Vector2.Distance(_agent.position, targetWorldPos) > 0.01f)
            {
                _agent.position = Vector2.MoveTowards(_agent.position, targetWorldPos, _moveSpeed * Time.deltaTime);
                yield return null;
            }

            _agent.position = targetWorldPos;
        } else
        {
            yield return null;
        }
    }

    private IEnumerator PlantRoutine(Vector2Int targetPos, TileData.CropType cType)
    {
        CropsData cropsData = CropManager.instance.GetCropData((int)cType - 1);

        yield return new WaitForSeconds(1f);
        bool success = _tileMng.PlantCrop(targetPos, cropsData);
    }

    private IEnumerator HarvestRoutine(Vector2Int targetPos)
    {
        yield return new WaitForSeconds(1f);
        bool success = _tileMng.HarvestCrop(targetPos, _inventoryMng);
    }
}