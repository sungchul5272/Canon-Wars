using System.Collections.Generic;
using UnityEngine;

public class SODataManager : MonoBehaviour
{
    public static SODataManager instance;

    [SerializeField] private List<MapData> _mapDataSOList = new List<MapData>();
    [SerializeField] private List<TankDataSO> _tankDataSOList = new List<TankDataSO>();

    private void Awake()
    {
        if(instance == null)
            instance = this;

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    public MapData GetMapData(eMapType mapType)
    {
        if (mapType == eMapType.Random || mapType == eMapType.Max)
            return null;

        return _mapDataSOList[(int)mapType];
    }

    public TankDataSO GetTankData(eTankType tankType)
    {
        if (tankType == eTankType.Random || tankType == eTankType.Max)
            return null;

        return _tankDataSOList[(int)tankType];
    }
}
