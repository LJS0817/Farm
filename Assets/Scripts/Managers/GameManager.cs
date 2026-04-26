using UnityEngine;

public class GameManager : MonoBehaviour
{
    public RectTransform menu;
    public GameObject title_Tile;
    public GameObject title_Menu;
    public FarmLevelManager userInfo;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void GameStart()
    {
        userInfo.OpenUI();
        menu.anchoredPosition = new Vector2(0, 60);
        title_Tile.SetActive(false);
        title_Menu.SetActive(false);
    }
    public void ShowLogo()
    {
        userInfo.CloseUI();
        menu.anchoredPosition = new Vector2(0, -300);
        title_Tile.SetActive(true);
        title_Menu.SetActive(true);
    }

    public void GameOff()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
