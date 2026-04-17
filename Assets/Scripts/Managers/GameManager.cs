using UnityEngine;

public class GameManager : MonoBehaviour
{
    public RectTransform menu;
    public GameObject title_Tile;
    public GameObject title_Menu;
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
        menu.anchoredPosition = new Vector2(0, 60);
        title_Tile.SetActive(false);
        title_Menu.SetActive(false);
    }

}
