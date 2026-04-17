using LLMUnity;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class AgentIntentClassifier : MonoBehaviour
{
    [SerializeField]
    LLMCharacter classifierLLM;
    [SerializeField]
    TileManager _tileMng;
    [SerializeField]
    InventoryManager _inventoryMng;

    /// <summary>
    /// 유저 입력을 분석하고 필요한 데이터를 붙여 완성된 프롬프트 문자열을 반환합니다.
    /// </summary>
    public void GetFinalPrompt(string userInput, Action<(string input, string finalInput)> onPromptReady)
    {
        classifierLLM.ClearHistory();

        string currentReply = "";
        classifierLLM.Chat(
            userInput,
            (reply) => { currentReply = reply; },
            () =>
            {
                // 판별이 끝나면 문자열 조립 후 콜백으로 바로 전달
                string finalPrompt = BuildPrompt(userInput, currentReply);

                classifierLLM.ClearHistory();
                onPromptReady?.Invoke((userInput, finalPrompt));
            }
        );
    }

    private string BuildPrompt(string userInput, string rawResult)
    {
        string clean = rawResult.Trim().ToUpper();
        //Debug.Log($"[1차 분류 라우터 결과] : {rawResult}");

        string additionalData = "";

        // C(단순 대화)가 아니거나, 다른 기호가 섞여 있다면 데이터 조립 시작
        if (!clean.Contains("C") || clean.Contains("M") || clean.Contains("I") || clean.Contains("P"))
        {
            if (clean.Contains("M")) additionalData += GetCompressedMapState() + "\n";
            if (clean.Contains("I")) additionalData += GetInventoryState() + "\n";
            if (clean.Contains("P")) additionalData += GetAgentPositionState() + "\n";
        }

        // 최후의 안전장치: 아무 기호도 인식 못 했을 때 (오류 방지)
        if (string.IsNullOrEmpty(additionalData) && !clean.Contains("C"))
        {
            Debug.Log("[IntentClassifier] 인식 실패. 기본 데이터(M, P)를 강제로 추가합니다.");
            additionalData += GetCompressedMapState() + "\n" + GetAgentPositionState() + "\n";
        }

        // 조립된 데이터가 있으면 유저 입력 뒤에 붙여서 리턴, 없으면 유저 입력만 리턴
        if (!string.IsNullOrEmpty(additionalData))
        {
            return $"{userInput}\n[현재 게임 상태]\n{additionalData}";
        }

        return userInput;
    }

    // --- 데이터 추출 함수들 ---
    private string GetCompressedMapState()
    {
        if (_tileMng == null) return "[맵 정보 없음]";

        // 1. TileManager로부터 데이터 리스트를 가져옴
        List<TileData> activeTiles = _tileMng.GetNonEmptyTiles();

        // 2. LLM이 읽기 좋은 문자열로 조립
        StringBuilder sb = new StringBuilder();
        sb.Append("[맵 상태] ");

        if (activeTiles.Count == 0)
        {
            sb.Append("심어진 작물 없음");
        }
        else
        {
            for (int i = 0; i < activeTiles.Count; i++)
            {
                TileData tile = activeTiles[i];
                string stateKr = GetCropStateKorean(tile.cropState);
                sb.Append($"({tile.coord.x}, {tile.coord.y}): {tile.cropType} ({stateKr})");
                if(i < activeTiles.Count - 1) sb.Append(", ");
            }
        }

        return sb.ToString();
    }

    private string GetCropStateKorean(TileData.CropState state)
    {
        return state switch
        {
            TileData.CropState.IsGrowing => "성장 중",
            TileData.CropState.IsHarvastable => "수확 가능",
            _ => "상태 알 수 없음"
        };
    }

    private string GetInventoryState()
    {
        if (_inventoryMng == null) return "[인벤토리 정보 없음]";

        // 1. 아이템별로 수량을 합산하기 위한 딕셔너리
        Dictionary<string, int> itemTotals = new Dictionary<string, int>();

        // 2. 모든 슬롯을 순회하며 데이터 수집
        foreach (var slot in _inventoryMng.slots)
        {
            if (slot != null && !slot.IsEmpty)
            {
                string itemName = slot.item.itemName;
                if (itemTotals.ContainsKey(itemName))
                {
                    itemTotals[itemName] += slot.count;
                }
                else
                {
                    itemTotals[itemName] = slot.count;
                }
            }
        }

        // 3. 문자열 조립
        if (itemTotals.Count == 0)
        {
            return "[인벤토리] 비어 있음";
        }

        StringBuilder sb = new StringBuilder();
        sb.Append("[인벤토리] ");

        List<string> entries = new List<string>();
        foreach (var pair in itemTotals)
        {
            entries.Add($"{pair.Key}: {pair.Value}개");
        }

        sb.Append(string.Join(", ", entries));
        return sb.ToString();
    }

    private string GetAgentPositionState()
    {
        Vector2Int pos = Vector2Int.zero;
        if(_tileMng.TryGetTileFromWorldPosition(transform.position, out TileData tile))
        {
            pos = tile.coord;
        }
        return $"[에이전트 정보] 현재 위치: ({pos.x}, {pos.y})";
    }
}