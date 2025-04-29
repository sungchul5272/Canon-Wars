using UnityEngine;

public enum eMapType
{
    Random = -1,
    Valley,
    Forest,
    City,
    DesertTemple,
    DeepForest,
    Max
}

[CreateAssetMenu(fileName = "MapData", menuName = "Scriptable Objects/MapData")]
public class MapData : ScriptableObject
{
    [Header("Prefab")]
    public GameObject backgroundPrefab = null;
    public GameObject foregroundPrefab = null;

    [Header("Info")]
    public eMapType mapType = eMapType.Max;
    public Vector2 mapSize = Vector2.zero;

    [Header("Image")]
    public Sprite _mapSprite = null;

    [Header("Sound")]
    public AudioClip sound = null;
}
