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
    public GameObject backgroundPrefab = null;
    public GameObject foregroundPrefab = null;

    public eMapType mapType = eMapType.Max;
    public Vector2 mapSize = Vector2.zero;

    public Sprite _mapSprite = null;
}
