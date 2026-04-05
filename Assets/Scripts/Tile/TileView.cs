using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

[RequireComponent(typeof(TileData), typeof(SpriteRenderer))]
// TileData 상태를 실제 스프라이트 표시로 바꿔 주는 뷰 컴포넌트.
// 바닥, 흙 레이어, 최종 작물 레이어를 각각 갱신한다.
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

    // 현재 TileData 상태를 읽어 타일과 작물 스프라이트를 다시 그린다.
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
