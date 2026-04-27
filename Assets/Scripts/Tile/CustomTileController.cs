using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct TilePrefabMapping
{
    public string tileId;
    public GameObject prefab;
}

public class CustomTileController : MonoBehaviour
{
    [SerializeField]
    TilePrefabMapping[] _prefabMappings;
    Dictionary<string, GameObject> _customTiles;

    private void Awake()
    {
        InitDictionary();
    }

    private void InitDictionary()
    {
        int capacity = _prefabMappings != null ? _prefabMappings.Length : 0;
        _customTiles = new Dictionary<string, GameObject>(capacity);

        if (capacity == 0) return;

        for (int i = 0; i < _prefabMappings.Length; i++)
        {
            string key = _prefabMappings[i].tileId;
            GameObject prefab = _prefabMappings[i].prefab;

            if (string.IsNullOrWhiteSpace(key)) continue;

            if (!_customTiles.ContainsKey(key)) _customTiles.Add(key, prefab);
        }
    }

    public GameObject GetTilePrefab(string tileId)
    {
        if (_customTiles.TryGetValue(tileId, out GameObject prefab))
        {
            return prefab;
        }
        return null;
    }
}
