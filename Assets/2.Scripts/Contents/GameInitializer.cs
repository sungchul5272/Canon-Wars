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

    [Header("맵 스포너")]
    [SerializeField] private MapSpawner _mapSpawner;

    [Header("인게임 UI")]
    [SerializeField] IngameUIController _ingameUI;

    public CameraController _camController { get; private set; }

    private NetworkVariable<int> _netMapIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkList<Vector3> _spawnPosList = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private Dictionary<ulong, UserData> _clientUserData = new();
    private List<bool> _allDones = new();

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

        if (IsClient)
        {
            string nick = FirebaseManager._instance.userVO.NickName;
            string tankKey = NetworkPlayerData.SelectedTank.ToString();

            SetMyInfoUI(nick, tankKey);
            ReportPlayerInfoServerRpc(nick, tankKey);

            _loadingUI.SetActive(true);
        }

        StartCoroutine(InitRoutine());
    }

    private IEnumerator InitRoutine()
    {
        if (IsServer)
        {
            Debug.Log("[GameInitializer] InitRoutine 시작");

            yield return new WaitForSeconds(0.5f);

            // 생성할 맵 결정
            if (NetworkPlayerData.SelectedMapType == eMapType.Random)
            {
                _netMapIndex.Value = UnityEngine.Random.Range((int)eMapType.Valley, (int)eMapType.Max);
            }
            else
            {
                _netMapIndex.Value = (int)NetworkPlayerData.SelectedMapType;
            }

            // 맵 생성
            _mapSpawner.SpawnSelectMap(_netMapIndex.Value);
            UpdateLoadingMapBackground(_netMapIndex.Value);
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
        }

        // 플레이어 생성
        NetworkManager singleton = NetworkManager.Singleton;
        ulong clientId = singleton.LocalClientId;
        SpawnPlayerServerRpc(clientId, NetworkPlayerData.SelectedTank);
        Debug.Log("[GameInitializer] 탱크 생성 완료");

        if (IsServer)
        {
            yield return WaitAllClientsAsync();
            yield return new WaitForSeconds(1f);

            AssignTurnNumbers();
            yield return WaitAllClientsAsync();

            IngameManager.Instance.SetStartTurnIndex();
            IngameManager.Instance.StartGame();
        }
        else
        {
            while (_allDones.Count <= 0)
            {
                if (!singleton.IsListening)
                {
                    Debug.LogWarning("네트워크 연결이 끊겼습니다.");
                    break;
                }

                yield return null;
            }

            Debug.Log("준비 완료.");

            if (!singleton.IsListening)
            {
                Debug.LogWarning("네트워크 연결이 해제되어 로비로 돌아갑니다.");
                IngameManager.Instance.BackToLobby();
            }
        }
    }

    IEnumerator WaitAllClientsAsync()
    {
        while (_allDones.Count < NetworkPlayerData.GetMaxPlayer())
        {
            yield return null;
        }

        _allDones.Clear();
    }

    private void UpdateLoadingMapBackground(int mapIndex)
    {
        MapData mapData = SODataManager.instance.GetMapData((eMapType)mapIndex);

        if (mapData != null && mapData.backgroundPrefab != null)
        {
            SpriteRenderer sr = mapData.backgroundPrefab.GetComponentInChildren<SpriteRenderer>();

            if (sr != null)
            {
                _mapBackgroundImage.sprite = sr.sprite;
                _mapBackgroundImage.color = Color.white;
            }
            else
            {
                Debug.Log($"BackgroundPrefab 의 SpriteRenderer 누락");
            }
        }
        else
        {
            Debug.Log($"맵 인덱스 오류");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerServerRpc(ulong clientId, eTankType tankType)
    {
        int randIndex = UnityEngine.Random.Range(0, _spawnPosList.Count);
        Vector3 spawnPos = _spawnPosList[randIndex];
        _spawnPosList.RemoveAt(randIndex);
        Debug.Log($"남은 스폰 위치 수: {_spawnPosList.Count}.");

        // 탱크 데이터 불러와서 인스턴스
        TankDataSO tankData = GetSelectedTankData(tankType);
        GameObject tank = Instantiate(tankData._tankPrefab, spawnPos, Quaternion.identity);
        tank.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
        Debug.Log($"[GameInitializer] ID: {clientId}, 스폰 위치: {spawnPos}");

        PlayerController tankController = tank.GetComponent<PlayerController>();
        tankController._tankType.Value = tankData._tankType;

        // 완료
        _allDones.Add(true);
    }

    void AssignTurnNumbers()
    {
        // 턴 넘버 배정
        int turnNumber = 0;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            StartGameClientRpc(turnNumber, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new List<ulong> { clientId }
                }
            });

            turnNumber++;
        }
    }

    [ClientRpc]
    private void StartGameClientRpc(int turnNumber, ClientRpcParams clientRpcParams = default)
    {
        // 클라이언트가 자신의 턴 넘버를 저장
        IngameManager.Instance.playerTurnNumber = turnNumber;
        Debug.Log($"My turn number: {turnNumber}");

        _loadingUI.SetActive(false);
        _ingameUI.gameObject.SetActive(true);

        int mapIndex = _netMapIndex.Value;

        var mapData = SODataManager.instance.GetMapData((eMapType)mapIndex);
        if (mapData != null && mapData.sound != null)
        {
            SoundManager.Instance.PlayBGM(mapData.sound);
            Debug.Log($"BGM : {mapData}");
        }
        else
        {
            Debug.Log($"맵 데이터에 사운드가 없습니다");
        }

        if (!IsServer)
        {
            _allDones.Add(true);
        }

        SendAllDoneServerRpc();
    }


    [ServerRpc(RequireOwnership = false)]
    private void SendAllDoneServerRpc()
    {
        _allDones.Add(true);
    }

    private TankDataSO GetSelectedTankData(eTankType tankType)
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
        return tankData;
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
