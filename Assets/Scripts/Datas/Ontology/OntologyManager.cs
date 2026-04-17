using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class OntologyManager : MonoBehaviour
{
    public static OntologyManager Instance { get; private set; }

    [Header("Direct JSON Asset Reference")]
    [SerializeField] private TextAsset ontologyJsonFile;

    [Header("Resources/Data/Ontology/FarmOntology.json")]
    [SerializeField] private string ontologyResourcePath = "Data/Ontology/FarmOntology";

    private OntologyData _ontologyData;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[OntologyManager] Duplicate instance detected. Destroying...", this);
            Destroy(gameObject);
            return;
        }

        LoadOntology();
    }

    private void LoadOntology()
    {
        TextAsset jsonFile = ontologyJsonFile;

        if (jsonFile == null)
        {
            jsonFile = Resources.Load<TextAsset>(ontologyResourcePath);
        }

        if (jsonFile == null)
        {
            Debug.LogError(
                $"[OntologyManager] Ontology JSON not found. " +
                $"Assign a TextAsset in the inspector or place the file at Resources/{ontologyResourcePath}.json",
                this);
            return;
        }

        try
        {
            _ontologyData = JsonConvert.DeserializeObject<OntologyData>(jsonFile.text);

            if (_ontologyData == null)
            {
                Debug.LogError("[OntologyManager] Failed to deserialize ontology JSON.", this);
                return;
            }

            Debug.Log("[OntologyManager] Ontology loaded successfully.", this);
        }
        catch (JsonException exception)
        {
            Debug.LogError($"[OntologyManager] JSON parse failed.\n{exception.Message}", this);
        }
    }

    public bool IsLoaded()
    {
        return _ontologyData != null;
    }

    public string BuildEnhancedPrompt(string userQuestion)
    {
        if (string.IsNullOrWhiteSpace(userQuestion))
        {
            return string.Empty;
        }

        if (_ontologyData == null)
        {
            return userQuestion;
        }

        string mapInfo = GetMapInfo();
        string knowledge = SearchKnowledge(userQuestion);

        return
            $"### 맵 정보\n{mapInfo}\n\n" +
            $"### 정적 게임 지식\n{knowledge}\n\n" +
            $"### 사용자 질문\n{userQuestion}\n\n" +
            $"### 지시사항\n" +
            $"위 정보를 참고해서 답변하되, 기존 시스템 프롬프트의 JSON 응답 형식을 반드시 유지하세요.";
    }

    private string SearchKnowledge(string userQuestion)
    {
        HashSet<string> foundKnowledge = new HashSet<string>();

        IEnumerable<KnowledgeNode> cropNodes = _ontologyData.crops ?? Enumerable.Empty<KnowledgeNode>();
        IEnumerable<KnowledgeNode> ruleNodes = _ontologyData.system_rules ?? Enumerable.Empty<KnowledgeNode>();

        IEnumerable<KnowledgeNode> allNodes = cropNodes.Concat(ruleNodes);

        foreach (KnowledgeNode node in allNodes)
        {
            if (node == null || node.keywords == null || string.IsNullOrWhiteSpace(node.info))
            {
                continue;
            }

            foreach (string keyword in node.keywords)
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    continue;
                }

                if (userQuestion.Contains(keyword))
                {
                    foundKnowledge.Add(node.info);
                    break;
                }
            }
        }

        if (foundKnowledge.Count == 0)
        {
            return "관련 정적 지식을 찾지 못했습니다.";
        }

        return string.Join("\n", foundKnowledge);
    }

    private string GetMapInfo()
    {
        if (_ontologyData == null || _ontologyData.map == null)
        {
            return "맵 정보를 찾지 못했습니다.";
        }

        if (_ontologyData.map.coordinate_system != null &&
            !string.IsNullOrWhiteSpace(_ontologyData.map.coordinate_system.info))
        {
            return _ontologyData.map.coordinate_system.info;
        }

        if (_ontologyData.map.size != null)
        {
            return $"맵 크기는 가로 {_ontologyData.map.size.width}, 세로 {_ontologyData.map.size.height}입니다.";
        }

        return "맵 정보를 찾지 못했습니다.";
    }
}
