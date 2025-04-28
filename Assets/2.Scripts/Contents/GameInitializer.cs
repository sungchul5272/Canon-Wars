using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;
using UnityEditor.PackageManager;

public class GameInitializer : NetworkBehaviour
{
    public static GameInitializer Instance;

    [Header("로딩 UI")]
    [SerializeField] Sprite _textureRandom;
    [SerializeField] GameObject _loadingUI;
    [SerializeField] Text _myNickText;
    [SerializeField] Image _myTankImage;
    [SerializeField] Text _enemyNickText;
    [SerializeField] Image _enemyTankImage;
    [SerializeField]  Text _myWinRateText;
    [SerializeField] Text _enemyWinRateText;
    [SerializeField]  Image _mapBackgroundImage;
    [SerializeField]  List<Sprite> _mapBackgroundSprites;

    [Header("맵 스포너")]
    [SerializeField] private MapSpawner _mapSpawner;
    [SerializeField] private eMapType _selectedMapType = eMapType.Random;

    [Header("인게임 UI")]
    [SerializeField] IngameUIController _ingameUI;

    public CameraController _camController { get; private set; }

    private NetworkVariable<int> _netMapIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkList<Vector3> _spawnPosList = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private Dictionary<ulong, UserData> _clientUserData = new();

    private bool _allReady = false;

    //// 맵 생성 여부 확인
    //private NetworkList<bool> _mapLoadComplete = new(new List<bool>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    //// 카메라 초기화 여부 확인
    //private NetworkList<bool> _camerInitComplete = new(new List<bool>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    //private bool _bWaitAllComplete = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;

        _ingameUI.Init();
        _camController = Camera.main.GetComponent<CameraController>();
    }

    public override void OnNetworkSpawn()
    {
        _netMapIndex.OnValueChanged += (prev, next) =>
        {
            Debug.Log($"[GameInitializer] 맵 인덱스 변경: {prev} → {next}");
        };

        //// 맵 로드 확인
        //_mapLoadComplete.OnListChanged += (NetworkListEvent<bool> changeEvent) =>
        //{
        //    _bWaitAllComplete = true;
        //    int playerCount = NetworkManager.Singleton.ConnectedClients.Count;

        //    Debug.Log($"[GameInitializer] PlayerCount : {playerCount} mapLoadCompleteCount : {_mapLoadComplete.Count}");
        //    if (playerCount != _mapLoadComplete.Count)
        //    {
        //        _bWaitAllComplete = false;
        //    }
        //};

        //// 카메라 초기화 확인
        //_camerInitComplete.OnListChanged += (NetworkListEvent<bool> changeEvent) =>
        //{
        //    _bWaitAllComplete = true;

        //    int playerCount = NetworkManager.Singleton.ConnectedClients.Count;
        //    Debug.Log($"[GameInitializer] PlayerCount : {playerCount} camerInitComplete : {_camerInitComplete.Count}");
        //    if (playerCount != _camerInitComplete.Count)
        //    {
        //        _bWaitAllComplete = false;
        //    }
        //};

        if (IsClient)
        {
            string nick = FirebaseManager._instance.userVO.NickName;
            string tankKey = NetworkPlayerData.selectedTank.ToString();

            SetMyInfoUI(nick, tankKey);
            ReportPlayerInfoServerRpc(nick, tankKey);

            _loadingUI.SetActive(true);
        }

        //NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        StartCoroutine(InitRoutine());
    }

    private IEnumerator InitRoutine()
    {
        if (IsServer)
        {
            Debug.Log("[GameInitializer] InitRoutine 시작");

            yield return new WaitForSeconds(0.5f);

            // 생성할 맵 결정
            if (NetworkPlayerData.selectedMapType == eMapType.Random)
            {
                _netMapIndex.Value = UnityEngine.Random.Range((int)eMapType.Valley, (int)eMapType.Max);
            }
            else
            {
                _netMapIndex.Value = (int)NetworkPlayerData.selectedMapType;
            }

            // 맵 생성
            _mapSpawner.SpawnSelectMap(_netMapIndex.Value);
            UpdateLoadingMapBackground(_netMapIndex.Value);
            //_mapLoadComplete.Add(true);
            Debug.Log("[GameInitializer] 맵 생성 완료");
            var spawnList = _mapSpawner.GetSpawnPosPList();
            for (int i = 0; i < spawnList.Count; i++)
            {
                _spawnPosList.Add(spawnList[i]);
            }
        }
        else
        {
            // 맵이 정해질 때까지 대기
            while (_netMapIndex.Value < 0)
            {
                yield return null;
            }

            // 맵 생성
            _mapSpawner.SpawnSelectMap(_netMapIndex.Value);
            UpdateLoadingMapBackground(_netMapIndex.Value);

            //// 맵 로드 완료
            //ReportMapLoadedServerRpc();
        }

        //// 모든 플레이어 맵 로드 대기
        //while (!_bWaitAllComplete)
        //{
        //    yield return null;
        //}

        //Debug.Log("모든 유저 맵 로딩 완료");

        //// 각자의 카메라 초기화
        //_camController = Camera.main.GetComponent<CameraController>();
        //_camController.Init(() =>
        //{
        //    if(IsServer)
        //    {
        //        Debug.Log("Server Camera Init Complete");
        //        Debug.Log($"Camera Size : {Camera.main.orthographicSize}");
        //        _camerInitComplete.Add(true);
        //    }
        //    else
        //    {
        //        Debug.Log("Client Camera Init Complete");
        //        Debug.Log($"Camera Size : {Camera.main.orthographicSize}");
        //        ReportCameraInitServerRpc();
        //    }
        //});

        //// 모든 플레이어 카메라 초기화 대기
        //while (!_bWaitAllComplete)
        //{
        //    yield return null;
        //}
        //Debug.Log("[GameInitializer] 카메라 초기화 완료");

        // 플레이어 생성
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        SpawnPlayerServerRpc(clientId, NetworkPlayerData.selectedTank);
        Debug.Log("[GameInitializer] 탱크 생성 완료");

        if (IsServer)
        {
            while (!_allReady)
            {
                yield return null;
            }

            yield return new WaitForSeconds(1f);
            IngameManager.Instance.SetStartTurnIndex();
            IngameManager.Instance.StartGame();
            StartGameClientRpc();
        }

        //if (IsServer)
        //{
        //    yield return new WaitForSeconds(5f);
        //    CloseLoadingUIClientRpc();
        //}
    }

    //[ServerRpc(RequireOwnership = false)]
    //public void ReportMapLoadedServerRpc()
    //{
    //    _mapLoadComplete.Add(true); // 요청
    //}

    //[ServerRpc(RequireOwnership = false)]
    //public void ReportCameraInitServerRpc()
    //{
    //    _camerInitComplete.Add(true); // 요청
    //}
    private void UpdateLoadingMapBackground(int mapIndex)
    {
        if (_mapBackgroundSprites == null || _mapBackgroundSprites.Count == 0)
            return;

        if (mapIndex >= 0 && mapIndex < _mapBackgroundSprites.Count)
        {
            _mapBackgroundImage.sprite = _mapBackgroundSprites[mapIndex];
            _mapBackgroundImage.color = Color.white;
            Debug.Log($"[GameInitializer] 로딩창 배경 변경 완료: {((eMapType)mapIndex).ToString()}");
        }
        else
        {
            Debug.LogWarning("[GameInitializer] 잘못된 맵 인덱스입니다.");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerServerRpc(ulong clientId, eTankType tankType)
    {
        Debug.Log($"플레이어 확인: {clientId}");

        int randIndex = UnityEngine.Random.Range(0, _spawnPosList.Count);
        Vector3 spawnPos = _spawnPosList[randIndex];
        _spawnPosList.RemoveAt(randIndex);
        Debug.Log($"남은 스폰 위치 수: {_spawnPosList.Count}.");

        // 탱크 데이터 불러와서 인스턴스
        GameObject tankPrefab = GetTankPrefab(tankType);
        GameObject tank = Instantiate(tankPrefab, spawnPos, Quaternion.identity);
        tank.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
        Debug.Log($"[GameInitializer] Player {clientId} 스폰 위치: {spawnPos}");

        // 모든 플레이어가 들어왔을 경우
        if (NetworkManager.Singleton.ConnectedClients.Count == NetworkPlayerData.GetMaxPlayer())
        {
            _allReady = true;
        }
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        _loadingUI.SetActive(false);
        _ingameUI.gameObject.SetActive(true);
        Debug.Log("[GameInitializer] 초기화 완료! UI 전환");
    }

    private GameObject GetTankPrefab(eTankType tankType)
    {
        eTankType typeToSpawn;
        if (tankType == eTankType.Random) // 탱크가 랜덤일 경우
        {
            Debug.Log("무작위 탱크 선택됨.");
            typeToSpawn = (eTankType)UnityEngine.Random.Range(0, (int)eTankType.Max);
        }
        else
        {
            Debug.Log($"선택된 탱크: {tankType}.");
            typeToSpawn = tankType;
        }

        TankDataSO tankData = SODataManager.instance.GetTankData(typeToSpawn);
        return tankData._tankPrefab;
    }

    public Vector2 GetMapSize()
    {
        if (_mapSpawner == null)
        {
            return Vector2.zero;
        }

        return _mapSpawner.GetMapSize();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReportPlayerInfoServerRpc(string nick, string tankKey, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        _clientUserData[senderId] = new UserData
        {
            NickName = nick,
            NowTank = tankKey
        };

        Debug.Log($"[서버] 유저 정보 등록 완료 - ID: {senderId}, 닉네임: {nick}, 탱크: {tankKey}");

        if (_clientUserData.Count >= 2)
        {
            foreach (var pair in _clientUserData)
            {
                foreach (var target in _clientUserData)
                {
                    if (pair.Key != target.Key)
                    {
                        SendPlayerInfoClientRpc(pair.Value.NickName, pair.Value.NowTank, new ClientRpcParams
                        {
                            Send = new ClientRpcSendParams { TargetClientIds = new[] { target.Key } }
                        });
                    }
                }
            }
        }
    }

    [ClientRpc]
    void SendPlayerInfoClientRpc(string enemyNick, string enemyTankKey, ClientRpcParams clientRpcParams = default)
    {
        _enemyNickText.text = enemyNick;

        if (System.Enum.TryParse(enemyTankKey, out eTankType tankType))
        {
            var tankData = SODataManager.instance.GetTankData(tankType);
            if (tankData != null)
            {
                _enemyTankImage.sprite = tankData._tankSprite;
            }
            else
            {
                _enemyTankImage.sprite = _textureRandom;
            }
        }

        Debug.Log($"[클라이언트] 상대 정보 수신 - 닉네임: {enemyNick}, 탱크: {enemyTankKey}");
    }

    public void SetMyInfoUI(string nick, string tankKey)
    {
        _myNickText.text = nick;

        if (System.Enum.TryParse(tankKey, out eTankType tankType))
        {
            var tankData = SODataManager.instance.GetTankData(tankType);
            if (tankData != null)
            {
                _myTankImage.sprite = tankData._tankSprite;
            }
            else
            {
                _myTankImage.sprite = _textureRandom;
            }
        }

        // 닉네임 표시 끝나고 승률 따로 표시
        float myWinRate = CalculateMyWinRate();
        _myWinRateText.text = $"승률 {myWinRate:F1}%";

        SendMyWinRateServerRpc(nick, myWinRate);
    }
    private float CalculateMyWinRate()
    {
        int win = 0, lose = 0, draw = 0;

        if (FirebaseManager._instance.userVO.BattleInfos != null)
        {
            foreach (var info in FirebaseManager._instance.userVO.BattleInfos)
            {
                switch (info.result)
                {
                    case "승":
                        win++;
                        break;
                    case "패":
                        lose++;
                        break;
                    case "무":
                        draw++;
                        break;
                }
            }
        }

        int totalGames = win + lose + draw;
        if (totalGames == 0) return 0f;

        return (float)win / totalGames * 100f;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendMyWinRateServerRpc(string nick, float winRate, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        // 받은 승률을 다른 클라이언트에게 보내주기
        foreach (var target in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (target != senderId)
            {
                SendEnemyWinRateClientRpc(nick, winRate, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { target } }
                });
            }
        }
        Debug.Log($"[서버] 나의 승률 전송 완료");
    }

    [ClientRpc]
    private void SendEnemyWinRateClientRpc(string enemyNick, float enemyWinRate, ClientRpcParams clientRpcParams = default)
    {
        _enemyNickText.text = enemyNick;
        _enemyWinRateText.text = $"승률 {enemyWinRate:F1}%";
        Debug.Log($"[클라이언트] 상대 승률 수신 완료"); 
    }
}
