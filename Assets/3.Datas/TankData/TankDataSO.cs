using System.Collections.Generic;
using UnityEngine;

public enum eTankType
{
    Random = -1,
    Green,
    Yellow,
    Max
}

[CreateAssetMenu(fileName = "TankData", menuName = "Tank/Tank Data", order = 1)]
public class TankDataSO : ScriptableObject
{
    [Header("기본 탱크 정보")]
    public eTankType _tankType = eTankType.Random;
    public string _tankName;         // 탱크 이름
    public int _hp;              // 체력
    public int _atk;              // 공격력
    public float _speed;             // 이동 속도

    [Header("탱크 이미지")]
    public Sprite _tankSprite;       // 탱크 본체 이미지

    [Header("탱크 프리팹")]
    public GameObject _tankPrefab; // 탱크 프리팹

    [Header("탱크 소유 포탄 프리팹")]
    public List<GameObject> _shellList = new List<GameObject>();
}
