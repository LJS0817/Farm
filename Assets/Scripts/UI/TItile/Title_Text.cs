using TMPro;
using UnityEngine;
using System.Collections;

public class Title_Text : MonoBehaviour
{
    [TextArea(2, 4)]
    private string[] textPresets =
    {
        "환영해요!",
        "작물마다 성장 시간이 다를 수 있어요!",
        "작물을 수확해서 인벤토리에 담아보세요!",
        "수확이 끝난 작물은 보상 아이템으로 인벤토리에 들어가요!",
        "타일을 터치하면 현재 상태를 확인할 수 있어요!",
        "인벤토리가 가득 차면 아이템을 모두 담지 못할 수도 있어요!",
        "나만의 농장을 완성해 보세요!",
        "무엇을 먼저 해야 할지 고민된다면, 빈 땅에 작물을 심는 것부터 시작해 보세요!"
    };
    public TMP_Text ai_Text;
    public GameObject loadingObject;

    [SerializeField] private float loadingDuration = 1f;

    private bool isShowingText;

    private void Start()
    {
        if (loadingObject != null)
        {
            loadingObject.SetActive(false);
        }

        ShowRandomTitleText();
    }

    public void ShowRandomTitleText()
    {
        if (isShowingText)
        {
            return;
        }

        StartCoroutine(ShowRandomTitleTextRoutine());
    }

    private IEnumerator ShowRandomTitleTextRoutine()
    {
        isShowingText = true;
        ai_Text.text = "";
        if (loadingObject != null)
        {
            loadingObject.SetActive(true);
        }

        yield return new WaitForSeconds(loadingDuration);

        if (loadingObject != null)
        {
            loadingObject.SetActive(false);
        }

        if (ai_Text != null && textPresets != null && textPresets.Length > 0)
        {
            int randomIndex = Random.Range(0, textPresets.Length);
            ai_Text.text = textPresets[randomIndex];
        }

        isShowingText = false;
    }
}
