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



        //작물이 자라는 중이면 땅 잔디 -> 흙으로 변경
        if(tileData.cropState == TileData.CropState.IsGrowing)
            cropLayer0.sprite = tileManager.GetTileSprite(TileData.TileType.Soil);
        
          //작물이 자라는 중이면 땅 잔디 -> 흙으로 변경
        if(tileData.cropState == TileData.CropState.IsHarvastable)
            cropLayer1.sprite = tileManager.GetCropSpirte(tileData.cropType);
    }
    


}
