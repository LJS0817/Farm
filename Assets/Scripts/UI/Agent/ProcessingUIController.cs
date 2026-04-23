using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProcessingUIController : MonoBehaviour
{
    [SerializeField]
    RectTransform _processBar;
    [SerializeField]
    TMP_Text _actionText;
    [SerializeField]
    TMP_Text _processTimeText;
    [SerializeField]
    RectMask2D _processMask;

    float _width = -1f;
    
    /// <summary>
    /// 지정된 시간 동안 UI를 업데이트하고 대기하는 코루틴입니다.
    /// </summary>
    public IEnumerator ProcessTaskRoutine(string actionName, float processTime)
    {
        // UI 활성화
        gameObject.SetActive(true);

        if(_width < 0f) _width = _processBar.rect.width * _processBar.localScale.x;

        _actionText.SetText(actionName);

        float currentTime = processTime + Time.deltaTime;

        while (currentTime > 0)
        {
            currentTime -= Time.deltaTime;

            if (currentTime < 0) currentTime = 0;

            _processTimeText.SetText($"{currentTime:F1} s");

            float ratio = currentTime / processTime;
            float currentPadding = _width * (1f - ratio);
            _processMask.padding = new Vector4(0, 0, currentPadding, 0);

            yield return null;
        }

        gameObject.SetActive(false);
    }
}