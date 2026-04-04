using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

[RequireComponent(typeof(TileData), typeof(SpriteRenderer))]
public class TileView : MonoBehaviour
{
    private TileData tileData;
    private SpriteRenderer spriteRenderer;
    private TileManager tileManager;
    public SpriteRenderer cropLayer0;//땅
    public SpriteRenderer cropLayer1;//작물 오브젝트

    private void Awake()
    {
        tileData = GetComponent<TileData>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        tileManager = FindFirstObjectByType<TileManager>();
    }

    public void Refresh()
    {
        if (tileData == null || spriteRenderer == null) return;

        if (tileManager == null)
        {
            tileManager = FindFirstObjectByType<TileManager>();
        }

        spriteRenderer.sprite = tileManager != null ? tileManager.GetTileSprite(tileData.tileType) : null;

        if (cropLayer0 != null)
        {
            cropLayer0.sprite = tileData.cropType == TileData.CropType.IsEmpty
                ? null
                : tileManager.GetTileSprite(TileData.TileType.Soil);
        }

        if (cropLayer1 != null)
        {
            cropLayer1.sprite = tileData.cropState == TileData.CropState.IsHarvastable
                ? tileManager.GetCropSpirte(tileData.cropType)
                : null;
        }
    }

}
