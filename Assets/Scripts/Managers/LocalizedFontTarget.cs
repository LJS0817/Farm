using UnityEngine;

public class LocalizedFontTarget : MonoBehaviour
{
    public enum FontRole
    {
        Default,
        Title,
        Desc
    }

    [SerializeField] private FontRole fontRole = FontRole.Default;

    public FontRole Role => fontRole;
}
