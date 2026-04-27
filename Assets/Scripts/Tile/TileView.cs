using System;
using UnityEngine;

[RequireComponent(typeof(TileData), typeof(SpriteRenderer))]
public class TileView : MonoBehaviour
{
    private TileData tileData;
    private SpriteRenderer spriteRenderer;
    private TileManager tileManager;

    public SpriteRenderer cropLayer_Soil; // 작물이 있을 때 켜지는 흙 오버레이
    public SpriteRenderer cropLayer1;      // 수확 가능 상태의 작물 오브젝트

    GameObject _customTile;

    private void Awake()
    {
        tileData = GetComponent<TileData>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        tileManager = FindFirstObjectByType<TileManager>();
        _customTile = null;
    }

    public void Refresh(Func<string, GameObject> getPrefabAction=null)
    {
        if (tileData == null || spriteRenderer == null)
        {
            return;
        }

        if (tileManager == null)
        {
            tileManager = FindFirstObjectByType<TileManager>();
        }

        if (tileManager == null)
        {
            return;
        }

        spriteRenderer.sprite = tileManager.GetTileSprite(tileData);
        
        bool hasCrop = tileData.cropType != TileData.CropType.IsEmpty;
        bool blocksCropLayers =
            tileData.tileType == TileData.TileType.Tree ||
            tileData.tileType == TileData.TileType.Rock ||
            tileData.tileType == TileData.TileType.Water;

        if (cropLayer_Soil != null)
        {
            cropLayer_Soil.gameObject.SetActive(hasCrop && !blocksCropLayers);
        }

        if (cropLayer1 != null)
        {
            cropLayer1.sprite = !blocksCropLayers && tileData.cropState == TileData.CropState.IsHarvastable
                ? tileManager.GetCropSpirte(tileData.cropType)
                : null;
        }

        if (getPrefabAction != null)
        {
            if (_customTile != null)
            {
                Destroy(_customTile);
                _customTile = null;
            }

            GameObject obj = getPrefabAction(spriteRenderer.sprite.name);
            if (obj != null)
            {
                _customTile = Instantiate(obj, transform);
                _customTile.transform.localPosition = Vector3.zero;
                _customTile.GetComponent<SpriteRenderer>().sortingOrder = spriteRenderer.sortingOrder;
                for(int i = 0; i < _customTile.transform.childCount; i++)
                    _customTile.transform.GetChild(i).GetComponent<SpriteRenderer>().sortingOrder = spriteRenderer.sortingOrder + 1;
                spriteRenderer.sprite = null;
            }
        }

        if (_customTile != null)
        {
            if (_customTile.activeInHierarchy && hasCrop) _customTile.SetActive(false);
            else if (!_customTile.activeInHierarchy && !hasCrop) _customTile.SetActive(true);
        }
    }
}
