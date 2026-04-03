using Newtonsoft.Json;
using UnityEngine;

public class AgentInstructionManager : MonoBehaviour
{

    AgentActionController _actionController;

    private void Start()
    {
        _actionController = GetComponent<AgentActionController>();
    }

    public string ParseLLMResponse(string jsonString)
    {
        jsonString = jsonString.Replace("```json", "").Replace("```", "").Trim();

        AgentResponse response = JsonConvert.DeserializeObject<AgentResponse>(jsonString);
        if (response != null)
        {
            Debug.Log($"[LLM 답변]: {response.answer}");

            if (response.commands != null && response.commands.Count > 0)
            {
                _actionController.ReceiveCommands(response.commands);
            }

            return response.answer;
        }
        else
        {
            return "JSON 파싱 실패";
        }
    }
}
