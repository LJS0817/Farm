using UnityEngine;
using UnityEngine.EventSystems;

public class HoverActivateUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject targetObject;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetObject != null)
            targetObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (targetObject != null)
            targetObject.SetActive(false);
    }
}
