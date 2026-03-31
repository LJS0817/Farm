using UnityEngine;

[RequireComponent(typeof(TileData), typeof(SpriteRenderer))]
public class TileView : MonoBehaviour
{
    [SerializeField] private Sprite soil;
    private TileData tileData;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        tileData = GetComponent<TileData>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Refresh()
    {
        if (tileData == null || spriteRenderer == null) return;

        switch (tileData.tileType)
        {
            case TileData.TileType.Empty:
                spriteRenderer.sprite = null;
                break;

            case TileData.TileType.Soil:
                spriteRenderer.sprite = soil;
                break;
        }
    }
}
