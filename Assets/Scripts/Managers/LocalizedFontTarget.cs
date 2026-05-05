using UnityEngine;

public class LocalizedFontTarget : MonoBehaviour
{
    public enum FontRole
    {
        Default,
        Title,
        Desc,
        Shop
    }

    [SerializeField] private FontRole fontRole = FontRole.Default;

    public FontRole Role => fontRole;
}
