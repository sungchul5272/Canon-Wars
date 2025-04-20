using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;

public class GameInitializer : NetworkBehaviour
{
    public static GameInitializer Instance;

    [Header("로딩 UI")]
    [SerializeField] GameObject _loadingUI;
    [SerializeField] Text _myNickText;
    [SerializeField] Image _myTankImage;
    [SerializeField] Text _enemyNickText;
    [SerializeField] Image _enemyTankImage;

    [Header("맵 및 탱크 프리팹")]
    [SerializeField] private MapSpawner _mapSpawner;
    [SerializeField] private List<GameObject> _tankPrefabList;
    [SerializeField] private eMapType _selectedMapType = eMapType.Random;

    [Header("인게임 UI")]
    [SerializeField] GameObject _ingameUI;

    public Transform CurShellTrans { get; set; }
    public CameraController _camController { get; private set; }
    public PlayerController CurTurnPlayer { get; set; }

    private NetworkVariable<int> _netMapIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private List<Vector3> _spawnPosList = new();
    private Dictionary<ulong, UserData> _clientUserData = new();

    void Awake()
    {
        if (Instance == null)
            Instance = this;

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
            SetMyInfoUI(FirebaseManager._instance.userVO.NickName, FirebaseManager._instance.userVO.NowTank);
            ReportPlayerInfoServerRpc(
                FirebaseManager._instance.userVO.NickName,
                FirebaseManager._instance.userVO.NowTank
            );

            _loadingUI.SetActive(true);
        }

        StartCoroutine(InitRoutine());
    }

    private IEnumerator InitRoutine()
    {
        // 호스트일 경우
        if (IsServer)
        {
            Debug.Log("[GameInitializer] InitRoutine 시작");

            yield return new WaitForSeconds(0.5f);

            // 생성할 맵 결정
            SetMapToSpawn();
            Debug.Log("[GameInitializer] 맵 생성 완료");

            // 플레이어 생성
            SpawnPlayers();
            Debug.Log("[GameInitializer] 탱크 생성 완료");
        }

        // 클라이언트도 맵을 생성해야 함
        _mapSpawner.SpawnSelectMap(_netMapIndex.Value);

        // 각자의 카메라 초기화
        _camController = Camera.main.GetComponent<CameraController>();
        _camController?.Init();
        Debug.Log("[GameInitializer] 카메라 초기화 완료");

        if (IsServer)
        {
            yield return new WaitForSeconds(5f);
            CloseLoadingUIClientRpc();
        }
    }

    private void SetMapToSpawn()
    {
        // 생성할 맵 결정
        _netMapIndex.Value = UnityEngine.Random.Range((int)eMapType.Valley, (int)eMapType.Max);
        _mapSpawner.SpawnSelectMap(_netMapIndex.Value);
    }

    private void SpawnPlayers()
    {
        if (!IsServer) return;

        _spawnPosList = _mapSpawner.GetSpawnPosPList();

        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            SpawnPlayer(clientId);
        }

        NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayer;
        //NetworkManager.Singleton.ConnectedClients[0].name
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (_spawnPosList.Count == 0)
        {
            Debug.LogWarning("[GameInitializer] 스폰 좌표가 부족합니다.");
            return;
        }

        int randIndex = UnityEngine.Random.Range(0, _spawnPosList.Count);
        Vector3 spawnPos = _spawnPosList[randIndex];
        _spawnPosList.RemoveAt(randIndex);

        GameObject tank = Instantiate(_tankPrefabList[0], spawnPos, Quaternion.identity);
        tank.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
        tank.name = $"Player {clientId}";

        var player = tank.GetComponent<PlayerController>();
        if (player.transform.position.x > 0) player.Flip(-1);

        Debug.Log($"[GameInitializer] Player {clientId} 스폰 위치: {spawnPos}");

        if (NetworkManager.Singleton.ConnectedClients.Count == NetworkPlayerData.GetMaxPlayer())
        {
            IngameManager.Instance.SetStartTurnIndex();
        }
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
        _enemyTankImage.sprite = TankUtil.GetTankSprite(enemyTankKey);

        Debug.Log($"[클라이언트] 상대 정보 수신 - 닉네임: {enemyNick}, 탱크: {enemyTankKey}");
    }

    public void SetMyInfoUI(string nick, string tankKey)
    {
        _myNickText.text = nick;
        _myTankImage.sprite = TankUtil.GetTankSprite(tankKey);
        Debug.Log($"[클라이언트] 내 정보 UI 세팅 - 닉네임: {nick}, 탱크: {tankKey}");
    }



    [ClientRpc]
    void CloseLoadingUIClientRpc()
    {
        _loadingUI.SetActive(false);
        Debug.Log("[GameInitializer] 초기화 완료! 로딩 UI 닫기");

        _ingameUI.SetActive(true);
    }
}
