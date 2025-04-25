using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSpawner : MonoBehaviour
{
    private MapData _curMap = null;
    private Ground _selectedGround = null;
    private int _seletedMapIndex = 0;

    public void SpawnSelectMap(int mapIndex)
    {
        _curMap = null;

        // 맵 인덱스를 통해서 생성
        _seletedMapIndex = mapIndex;
        _curMap = SODataManager.instance.GetMapData((eMapType)_seletedMapIndex);

        // 후경 생성
        Instantiate(_curMap.backgroundPrefab);

        // 전경 생성
        GameObject goFore = Instantiate(_curMap.foregroundPrefab);

        _selectedGround = goFore.GetComponent<Ground>();
        _selectedGround.Init();
    }

    public List<Vector3> GetSpawnPosPList()
    {
        List<Vector3> posList = new List<Vector3>();

        foreach (Transform trans in _selectedGround.SpawnTransList)
        {
            posList.Add(trans.position);
        }

        return posList;
    }

    public Vector2 GetMapSize()
    {
        if (_curMap== null)
        {
            Debug.LogWarning("Cur map is null!");
            return Vector2.zero;
        }

        return _curMap.mapSize;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        // 중심 좌표 (현재 오브젝트 위치 기준)
        Vector3 center = transform.position;

        if (_curMap != null)
        {
            // Z축은 0으로 고정, X와 Y만 사용
            Vector3 size = new Vector3(_curMap.mapSize.x, _curMap.mapSize.y, 0f);

            Gizmos.DrawWireCube(center, size);
        }
    }
}
