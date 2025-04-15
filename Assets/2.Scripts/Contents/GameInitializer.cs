using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameInitializer : NetworkBehaviour
{
    #region 임시 싱글턴
    public static GameInitializer Instance;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }
    #endregion

    private const float TURN_END_TERM = 3f;         // 턴 사이 시간

    [Header("Info")]
    [SerializeField] private MapSpawner _mapSpawner;
    [SerializeField] private List<GameObject> _tankPrefabList;
    [SerializeField] private eMapType _selectedMapType = eMapType.Random;
    //[SerializeField] private int _playerCount = 0;

    [Header("Environment")]
    [Range(0f,10f)]
    [SerializeField] private float _windForceMax = 0f;
    [SerializeField] private float _curWindForce = 0f;

    private CameraController _camController = null;
    private List<PlayerController> _playerList = new List<PlayerController>();

    //private int _curTurnPlayerIndex = 0;
    //public PlayerController CurTurnPlayer { get => _playerList[_curTurnPlayerIndex]; }
    public Transform CurShellTrans { get; set; }

    private bool _isTurnWait = false;

    public PlayerController CurTurnPlayer { get; set; }
    private List<Vector3> _spawnPosList = new();
    private NetworkVariable<int> _netMapIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> _netTurnIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Init();
    }

    public override void OnNetworkSpawn()
    {
        _netMapIndex.OnValueChanged += (previousValue, newValue) =>
        {
            Debug.Log($"Map index is {newValue}.");
        };

        _netTurnIndex.OnValueChanged += (previousValue, newValue) =>
        {
            FindCurrentTurnPlayer(newValue);
        };
    }

    public void Init(Action pCallback = null)
    {
        // 선택한 맵 생성
        SpawnMap();

        // 플레이어 소환
        SpawnPlayers();

        // 호스트일 경우
        if (IsServer)
        {
            // 무작위 턴 순서 지정
            _netTurnIndex.Value = UnityEngine.Random.Range(0, NetworkPlayerData.GetMaxPlayer());
        }

        //// 선택한 맵 생성
        //_mapSpawner.SpawnSelectMap(_selectedMapType);

        //if (_playerCount == 0)
        //{
        //    Debug.Log("No Player");
        //    return;
        //}

        //// 캐릭터 랜덤 스폰 좌표 배치
        //PlayerRandomPosSpawn();

        // 카메라 초기화
        _camController = Camera.main.GetComponent<CameraController>();

        if (_camController != null)
            _camController.Init();

        //// 랜덤 순서로 턴 시작
        //_curTurnPlayerIndex = UnityEngine.Random.Range(0, _playerCount);

        //Debug.Log($"First Turn Player : {CurTurnPlayer.name}");

        //// 플레이어 카메라 포커싱
        //PlayerCameraFocusing(CurTurnPlayer);
        //CurTurnPlayer.IsMyTurn();

        // 바람 설정
        RandomWindForce();

        if (pCallback != null)
            pCallback.Invoke();
    }

    private void FindCurrentTurnPlayer(int turnIndex)
    {
        ulong clientId = NetworkManager.LocalClientId;
        Debug.Log($"Turn index is {turnIndex}. Your turn is {clientId}.");
        if ((ulong)turnIndex == clientId)
        {
            PlayerController player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerController>();
            player.IsMyTurn();
            CurTurnPlayer = player;

            // 플레이어 카메라 포커싱
            PlayerCameraFocusing(CurTurnPlayer);
            Debug.Log($"Your turn.");
        }
        else
        {
            Debug.Log($"Player {turnIndex}'s turn.");
        }
    }

    // 맵 생성
    private void SpawnMap()
    {
        // 호스트일 경우
        if (IsServer)
        {
            // 무작위 맵 지정
            _netMapIndex.Value = UnityEngine.Random.Range((int)eMapType.Valley, (int)eMapType.Max);
        }

        _mapSpawner.SpawnSelectMap(_netMapIndex.Value);
    }

    // 플레이어 생성
    private void SpawnPlayers()
    {
        // 스폰 위치 초기화
        if (IsServer)
        {
            _spawnPosList = _mapSpawner.GetSpawnPosPList();
        }

        NetworkManager singleton = NetworkManager.Singleton;
        if (IsServer)
        {
            //호스트 플레이어 소환
            SpawnPlayer(singleton.ConnectedClientsIds[0]);

            // 접속한 클라이언트 플레이어 소환
            NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
            {
                SpawnPlayer(clientId);
                CurrentTurnPlayerClientRpc(_netTurnIndex.Value);
            };
        }
    }

    // 플레이어 소환
    private void SpawnPlayer(ulong clientIndex)
    {
        // 랜덤 좌표 저장
        int randomIndex = UnityEngine.Random.Range(0, _spawnPosList.Count);
        Vector3 spawnPos = _spawnPosList[randomIndex];
        Debug.Log($"Spawn position ({_spawnPosList[randomIndex]}) removed.");
        _spawnPosList.RemoveAt(randomIndex);

        // 임시로 0번째 프리팹 생성
        GameObject prefab = Instantiate(_tankPrefabList[0], spawnPos, Quaternion.identity);
        prefab.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientIndex);
        prefab.name = $"Player {clientIndex}";

        // 플레이어 초기화
        PlayerController player = prefab.GetComponent<PlayerController>();

        // x좌표가 0보다 큰 경우 우측을 바라보도록
        if (player.transform.position.x > 0)
        {
            player.Flip(-1);
        }

        Debug.Log($"{player.gameObject.name} spawned at {spawnPos}.");
    }

    [ClientRpc]
    private void CurrentTurnPlayerClientRpc(int turnIndex)
    {
        FindCurrentTurnPlayer(turnIndex);
    }

    //// 스폰 좌표 목록중에 랜덤으로 선택해서 플레이어 캐릭터 배치하는 메소드
    //private void PlayerRandomPosSpawn()
    //{
    //    List<Vector3> posList = _mapSpawner.GetSpawnPosPList();
    //    int randPosIndex = 0;
    //    int addSortOrder = 2;
    //    int sortOrderValue = 0;

    //    // 플레이어의 수만큼 랜덤 좌표에 생성
    //    for (int i = 0; i < _playerCount; i++)
    //    {
    //        randPosIndex = UnityEngine.Random.Range(0, posList.Count);

    //        // 임시로 0번째 프리팹 생성
    //        Vector2 randPos = (Vector2)posList[randPosIndex];
    //        GameObject go = Instantiate(_tankPrefabList[0], randPos, Quaternion.identity);
    //        go.name = $"Player {i}";

    //        // 플레이어 초기화
    //        PlayerController player = go.GetComponent<PlayerController>();
    //        player.Init();
    //        player.ChangeSpriteOrder(sortOrderValue);

    //        // x좌표가 0보다 큰 경우 우측을 바라보도록
    //        if (player.transform.position.x > 0)
    //        {
    //            player.Flip(-1);
    //        }

    //        _playerList.Add(player);

    //        // 뽑힌 자리는 랜덤 좌표 목록에서 제거
    //        posList.RemoveAt(randPosIndex);

    //        // Sort Order값 증가
    //        sortOrderValue += addSortOrder;
    //    }
    //}

    public void PlayerTurnEnd()
    {
        _isTurnWait = true;

        StartCoroutine(StartNextPlayerTurn());
    }

    private IEnumerator StartNextPlayerTurn()
    {
        //// 턴 인덱스 증가
        //int nextIndex = (_curTurnPlayerIndex + 1) % _playerCount;
        //_curTurnPlayerIndex = nextIndex;

        Debug.Log($"Now Turn Player : {CurTurnPlayer.name}");

        // 현재 탄이 안터졌다면 대기
        while (CurShellTrans != null)
            yield return null;

        // 터지고나서 대기시간
        yield return new WaitForSeconds(TURN_END_TERM);

        // 바람 설정
        RandomWindForce();

        // 다음 플레이어 턴 시작
        MoveTurnServerRpc();
        //_camController.PlayerFocusing(CurTurnPlayer);
        //CurTurnPlayer.IsMyTurn();

        _isTurnWait = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void MoveTurnServerRpc()
    {
        _netTurnIndex.Value = (_netTurnIndex.Value + 1) % NetworkPlayerData.GetMaxPlayer();
    }

    private void RandomWindForce()
    {
        // 반올림 후 소수점 둘째자리까지만
        _curWindForce = Mathf.Round(UnityEngine.Random.Range(-_windForceMax, _windForceMax) * 100f) / 100f;
    }

    public float GetWindForce()
    {
        return _curWindForce;
    }


    public bool IsCurPlayerTurnWait()
    {
        return _isTurnWait;
    }

    public Vector2 GetMapSize()
    {
        if (_mapSpawner == null)
            return Vector2.zero;

        return _mapSpawner.GetMapSize();
    }

    public void PlayerCameraFocusing(PlayerController playerController)
    {
        _camController.PlayerFocusing(playerController);
    }

    //#region Ground Top Pos Find Method
    //private List<Vector2Int> FindGroundTopPos()
    //{
    //    List<Vector2Int> topPosListAll = new List<Vector2Int>(); // 모든 그라운드 top pos 리스트

    //    foreach (var colider in _polygonCollider2DList)
    //    {
    //        if (colider == null)
    //            continue;

    //        int maxY = int.MinValue;
    //        HashSet<Vector2Int> coliderTopPosSet = new HashSet<Vector2Int>(); // 중복 제거를 위한 HashSet

    //        // 모든 경로 순회하며 최상단 좌표 탐색
    //        for (int pathIndex = 0; pathIndex < colider.pathCount; pathIndex++)
    //        {
    //            Vector2[] points = colider.GetPath(pathIndex);

    //            foreach (Vector2 point in points)
    //            {
    //                Vector2 worldPoint = colider.transform.TransformPoint(point);

    //                // x 좌표: SPAWN_OFFSET_X 보정 후 변환
    //                int posIntX = Mathf.RoundToInt(worldPoint.x - Mathf.Sign(worldPoint.x) * SPAWN_OFFEST_X);
    //                // y 좌표: SPAWN_OFFSET_Y 보정 후 변환
    //                int posIntY = Mathf.CeilToInt(worldPoint.y + SPAWN_OFFSET_Y);

    //                Vector2Int worldPointInt = new Vector2Int(posIntX, posIntY);

    //                // 최상단 y값 갱신
    //                if (posIntY > maxY)
    //                {
    //                    maxY = posIntY;
    //                    coliderTopPosSet.Clear(); // 새로운 최상단 발견 시 초기화
    //                    coliderTopPosSet.Add(worldPointInt);
    //                }
    //                else if (posIntY == maxY)
    //                {
    //                    coliderTopPosSet.Add(worldPointInt); // 같은 높이의 좌표 추가
    //                }
    //            }
    //        }

    //        // 최상단 좌표 리스트 정렬 (x 값 기준으로 정렬)
    //        List<Vector2Int> coliderTopPosList = coliderTopPosSet.ToList();

    //        // 최대 ~ 최소까지 1 단위 간격으로 좌표 생성
    //        List<Vector2Int> topLinePos = GenerateTopLine(coliderTopPosList);

    //        // 결과 리스트에 추가
    //        topPosListAll.AddRange(topLinePos);
    //    }

    //    return topPosListAll;
    //}

    //// 최상단 좌표를 기반으로 연속적인 x 좌표 리스트 생성
    //private List<Vector2Int> GenerateTopLine(List<Vector2Int> topPositions)
    //{
    //    List<Vector2Int> topNewPos = new List<Vector2Int>();

    //    if (topPositions.Count == 0) return topNewPos;

    //    int startX = topPositions[0].x;
    //    int endX = topPositions[topPositions.Count - 1].x;
    //    int y = topPositions[0].y; // 모든 y 값이 같으므로 하나만 가져옴

    //    for (int x = startX; x >= endX; x--)
    //    {
    //        topNewPos.Add(new Vector2Int(x, y));
    //    }

    //    return topNewPos;
    //}

    //// x좌표 중복되는곳 제외
    //List<Vector2Int> FilterMultiplyPositionX(List<Vector2Int> positions)
    //{
    //    // x좌표가 key값 Vector2Int (x,y) 좌표가 Value
    //    Dictionary<float, Vector2Int> uniquePositions = new Dictionary<float, Vector2Int>();

    //    foreach (Vector2Int pos in positions)
    //    {
    //        // x좌표가 중복인지 확인
    //        if (uniquePositions.ContainsKey(pos.x))
    //        {
    //            // 동일한 x좌표의 경우 y값이 더 큰 좌표만 저장
    //            if (pos.y > uniquePositions[pos.x].y)
    //            {
    //                uniquePositions[pos.x] = pos;
    //            }
    //        }
    //        else
    //        {
    //            // 새로운 x좌표 추가
    //            uniquePositions[pos.x] = pos;
    //        }
    //    }

    //    return uniquePositions.Values.ToList(); // 결과를 리스트로 반환
    //}
    //#endregion

    //#region Player Random Pos Spawn Method
    //private void PlayerRandomSpawn()
    //{
    //    // 랜덤 좌표 생성
    //    int randPosIndex = UnityEngine.Random.Range(0, _groundTopPos.Count);
    //    float posX = _groundTopPos[randPosIndex].x;
    //    float posY = _groundTopPos[randPosIndex].y;
    //    Vector3 randSpawnPos = new Vector3(posX, posY);

    //    // TODO : Player가 선택한 탱크에 맞는 키값을 이용해서 알맞는 탱크 생성하기
    //    //          : 현재는 첫번째 탱크만 소환
    //    GameObject go = Instantiate(_tankPrefabList[0], randSpawnPos, Quaternion.identity);

    //    // 중앙에서 오른쪽에 있다면 좌측을 바라보도록 뒤집기
    //    if(_groundTopPos[randPosIndex].x > 0)
    //    {
    //        go.transform.localScale = new Vector3(-1,1,1);
    //    }

    //    // 중복 좌표에 생성 안되도록 제거
    //    int minRemoveIndex = Mathf.Max(0,randPosIndex - SPAWN_POS_REMOVE_RANGE);
    //    int maxRemoveIndex = Mathf.Min(randPosIndex + SPAWN_POS_REMOVE_RANGE, _groundTopPos.Count-1);

    //    for (int i = maxRemoveIndex; i >= minRemoveIndex; i--)
    //    {
    //        _groundTopPos.RemoveAt(i);
    //    }
    //}
    //#endregion
}
