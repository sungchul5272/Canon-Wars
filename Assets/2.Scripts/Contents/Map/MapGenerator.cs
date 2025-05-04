// 사용되지 않음

/*using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    private enum eMapType
    {
        None = -1,
        Bridge,
        //Jungle,
        //Desert,
        //Ice,
        Max
    }

    [SerializeField] private List<GameObject> _mapPrefabList = new List<GameObject>();
    [SerializeField] private List<Texture2D> _mapGroundTextureList = new List<Texture2D>();
    [SerializeField] private Ground _mapGroundPrefab = null;

    #region Random Map Generation
    [Header("Map Size")]
    [SerializeField] private float _mapSizeWidthRatio = 2.5f;         // 기본적으로 카메라 가로 크기에 맞춰서 증가

    [Header("Ground Scale")]
    [SerializeField] private float _randGroundScaleMin = 0.5f;        // 최소 땅 크기
    [SerializeField] private float _randGroundScaleMax = 1.5f;       // 최대 땅 크기

    [Header("Ground Interval")]
    [SerializeField] private float _randGroundIntervalMin = 1f;       // 최소 땅 간격
    [SerializeField] private float _randGroundIntervalMax = 2f;       // 최대 땅 간격

    private List<Rect> _existingGrounds = new List<Rect>();           // 생성된 땅들의 영역 저장 (검사용)
    private List<PolygonCollider2D> _colliderGroundList = new List<PolygonCollider2D>();       // 콜라이더 리스트 저장
    #endregion

    private eMapType _mapType = eMapType.None;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Init();
    }

    public void Init()
    {
        _colliderGroundList.Clear();
        _existingGrounds.Clear();

        RandomMapGeneration();
        //RandomGroundGeneration();
    }

    public void RandomMapGeneration()
    {
        // 랜덤 선택
        _mapType =(eMapType)Random.Range((int)eMapType.Bridge, (int)eMapType.Max);

        // 배경 생성
        GameObject map = Instantiate(_mapPrefabList[(int)_mapType]);
        map.transform.parent = this.transform;
    }

    #region Random Ground Generation
    private void RandomGroundGeneration()
    {
        float height = Camera.main.orthographicSize;
        float width = height * Camera.main.aspect * _mapSizeWidthRatio;

        int orderInLayerInc = 0;
        int maxAttempts = 3; // 최대 시도 횟수

        // 화면을 넘어가는 땅은 생성하지 않는다
        for (int i = (int)-width; i < (int)width;)
        {
            // 땅 생성
            Ground ground = Instantiate(_mapGroundPrefab);
            ground.transform.parent = this.transform;
            //ground.Init(_mapGroundTextureList[(int)_mapType]);

            // 레이어 순서 조정
            SpriteRenderer sr = ground.GetComponent<SpriteRenderer>();
            sr.sortingOrder += orderInLayerInc;
            orderInLayerInc++;

            // 랜덤 크기 설정
            float randScale = Random.Range(_randGroundScaleMin, _randGroundScaleMax);
            ground.transform.localScale = new Vector2(randScale, randScale);

            // 랜덤 위치 설정
            int minHeight = -(int)height;
            int maxHeight = minHeight - (minHeight / 2);

            bool isValidPosition = false;
            Vector3 newPosition = Vector3.zero;
            Rect newGroundBounds = new Rect();

            // 영역 겹칩 확인
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // 청
                float groundPosX = i == -(int)width ? i : i + Random.Range(_randGroundIntervalMin, _randGroundIntervalMax) * randScale;
                float groundPosY = Random.Range(minHeight, maxHeight);

                newGroundBounds = new Rect(
                    groundPosX - (ground.transform.localScale.x * 0.5f),
                    groundPosY - (ground.transform.localScale.y * 0.5f),
                    ground.transform.localScale.x,
                    ground.transform.localScale.y
                );

                bool isOverlapping = false;

                foreach (Rect existing in _existingGrounds)
                {
                    // 땅이 겹치는지 확인
                    if (existing.Overlaps(newGroundBounds))
                    {
                        // 작은 땅이 더 큰 땅 안에 완전히 포함되는지 확인
                        if (newGroundBounds.width <= existing.width && newGroundBounds.height <= existing.height &&
                            existing.Contains(new Vector2(newGroundBounds.xMin, newGroundBounds.yMin)) &&
                            existing.Contains(new Vector2(newGroundBounds.xMax, newGroundBounds.yMax)))
                        {
                            isOverlapping = true;
                            break; // 내부에 포함되면 다시 위치 찾기
                        }
                    }
                }

                if (!isOverlapping)
                {
                    newPosition = new Vector3(groundPosX, groundPosY, 0);
                    _existingGrounds.Add(newGroundBounds);
                    isValidPosition = true;
                    break;
                }
            }

            if (isValidPosition)
            {
                ground.transform.position = newPosition;
                _colliderGroundList.Add(ground.GetComponent<PolygonCollider2D>());
            }
            else
            {              
                Destroy(ground); // 적절한 위치를 찾지 못한 경우 제거
            }

            // 다음 위치 계산 (간격 랜덤)
            i += (int)(Random.Range(_randGroundIntervalMin, _randGroundIntervalMax) * randScale);
        }
    }

    public List<PolygonCollider2D> GetGroundCollider2DList()
    {
        return _colliderGroundList;
    }
    #endregion
}*/
