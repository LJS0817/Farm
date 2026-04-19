using TMPro;
using UnityEngine;

public class TileInfo_Plus : MonoBehaviour
{
    public enum PlusInfo
    {
        None,
        Moist
    }
    public PlusInfo plusInfo;
    public TMP_Text text;

    public void PlusInfoInit(PlusInfo plusInfo)
    {
        this.plusInfo = plusInfo;

        if (text == null)
        {
            text = GetComponentInChildren<TMP_Text>();
        }

        if (text == null)
        {
            return;
        }

        text.text = GetDisplayText(plusInfo);
    }

    private string GetDisplayText(PlusInfo plusInfo)
    {
        return plusInfo switch
        {
            PlusInfo.Moist => "<b>촉촉함</b>: 인접한 물 타일 효과로 수확량이 2배가 됩니다.",
            _ => string.Empty
        };
    }
}
