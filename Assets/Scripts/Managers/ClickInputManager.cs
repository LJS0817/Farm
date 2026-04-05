using UnityEngine;
using UnityEngine.EventSystems;

// 화면 입력을 월드 오브젝트 클릭으로 변환하는 입력 허브.
// UI 클릭은 무시하고, 캐릭터를 우선 처리한 뒤 타일 클릭을 처리한다.
public class ClickInputManager : MonoBehaviour
{
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!TryGetPointerScreenPosition(out Vector2 screenPosition))
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        if (TryHandleCharacterClick(screenPosition))
        {
            return;
        }

        TryHandleTileClick(screenPosition);
    }

    // 모바일 터치 종료 또는 마우스 버튼 업 시점의 포인터 좌표를 반환한다.
    private bool TryGetPointerScreenPosition(out Vector2 screenPosition)
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended)
            {
                screenPosition = touch.position;
                return true;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }

        screenPosition = default;
        return false;
    }

    // 같은 위치에 캐릭터와 타일이 함께 있을 수 있어 캐릭터 클릭을 먼저 우선 처리한다.
    private bool TryHandleCharacterClick(Vector2 screenPosition)
    {
        foreach (RaycastHit2D hit in Physics2D.GetRayIntersectionAll(mainCamera.ScreenPointToRay(screenPosition)))
        {
            CharacterTouchInteraction character = hit.collider.GetComponentInParent<CharacterTouchInteraction>();
            if (character != null)
            {
                character.HandleClick();
                return true;
            }
        }

        foreach (RaycastHit hit in Physics.RaycastAll(mainCamera.ScreenPointToRay(screenPosition)))
        {
            CharacterTouchInteraction character = hit.collider.GetComponentInParent<CharacterTouchInteraction>();
            if (character != null)
            {
                character.HandleClick();
                return true;
            }
        }

        return false;
    }

    // 타일에 부착된 TileInteraction을 찾아 실제 타일 상호작용으로 연결한다.
    private bool TryHandleTileClick(Vector2 screenPosition)
    {
        foreach (RaycastHit2D hit in Physics2D.GetRayIntersectionAll(mainCamera.ScreenPointToRay(screenPosition)))
        {
            TileInteraction tile = hit.collider.GetComponentInParent<TileInteraction>();
            if (tile != null)
            {
                tile.HandleClick();
                return true;
            }
        }

        foreach (RaycastHit hit in Physics.RaycastAll(mainCamera.ScreenPointToRay(screenPosition)))
        {
            TileInteraction tile = hit.collider.GetComponentInParent<TileInteraction>();
            if (tile != null)
            {
                tile.HandleClick();
                return true;
            }
        }

        return false;
    }
}
