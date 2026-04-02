using UnityEngine;
using UnityEngine.EventSystems;

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
