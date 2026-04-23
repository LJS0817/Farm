using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemFloatingUI : MonoBehaviour
{
    [SerializeField]
    float _floatingTime = 1f;

    [SerializeField, Header("위로 떠오를 거리")]
    float _floatDistance = 50f;

    [SerializeField, Header("투명도 곡선 (0 -> 1 -> 0)")]
    AnimationCurve _alphaCurve = new AnimationCurve(
        new Keyframe(0f, 0f),    // 시작 시점 (0초): 투명도 0
        new Keyframe(0.15f, 1f), // 15% 시점: 투명도 1 (급격히 나타남)
        new Keyframe(1f, 0f)     // 끝 시점 (1초): 투명도 0 (서서히 사라짐)
    );

    [SerializeField, Header("이동 애니메이션 곡선 (0 -> 1)")]
    AnimationCurve _moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    Image _icon;
    TMP_Text _amount;
    CanvasGroup _group;
    RectTransform _rect;

    Coroutine _fadeCoroutine;
    Vector2 _startPosition;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _rect = GetComponent<RectTransform>();
        _icon = transform.GetChild(0).GetComponent<Image>();
        _amount = transform.GetChild(1).GetComponent<TMP_Text>();
        HideUI();
    }

    public void ShowUI(Vector2 pos, Sprite sp, int amt)
    {
        _startPosition = pos;

        // 초기 알파값을 0으로 설정하여 시작할 때 안 보이게 처리
        _group.alpha = 0f;
        _icon.sprite = sp;
        _amount.SetText((amt > 0 ? "+ " : "- ") + amt);
        gameObject.SetActive(true);

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        _fadeCoroutine = StartCoroutine(FloatAndFadeRoutine());
    }

    IEnumerator FloatAndFadeRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < _floatingTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / _floatingTime;

            // 1. 투명도 적용: Lerp 없이 알파 전용 곡선의 값을 그대로 사용
            _group.alpha = _alphaCurve.Evaluate(t);

            // 2. 위치 적용: 이동 전용 곡선의 값으로 Lerp 계산
            float moveT = _moveCurve.Evaluate(t);
            _rect.anchoredPosition = new Vector2(
                _startPosition.x,
                _startPosition.y + Mathf.Lerp(0f, _floatDistance, moveT)
            );

            yield return null;
        }

        // 루프 종료 후 확실하게 초기화
        _group.alpha = 0f;
        HideUI();
    }

    void HideUI()
    {
        gameObject.SetActive(false);
    }
}