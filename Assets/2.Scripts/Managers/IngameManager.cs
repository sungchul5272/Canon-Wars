using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class IngameManager : NetworkBehaviour
{
    public static IngameManager Instance;

    [Header("턴 설정")]
    [SerializeField] float _turnTime = 40f;

    [Header("Environment")]
    [Range(0f, 10f)]
    [SerializeField] float _windForceMax = 0f;

    [Header("Result")]
    [SerializeField] ResultUI _resultUI;

    public Transform CurShellTrans { get; set; }

    public int PlayerTurnNumber { get; set; } = -1;
    public bool HostBackToLobby { get; private set; } = false;

    const float TURN_END_TERM = 3f;

    NetworkVariable<float> _netWindForce = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    NetworkVariable<float> _netTurnTimer = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    NetworkVariable<int> _netTurnIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    NetworkVariable<int> _netSelectedShellIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    string _lobbySceneName = "2.LobbyScene";
    bool _isGameStarted = false;
    bool _isTurnWait = false;
    bool _alreadySavedBattleInfo = false;
    bool _isGameEnded = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (!IsServer || !_isGameStarted || _isTurnWait)
        {
            return;
        }

        _netTurnTimer.Value -= Time.deltaTime;

        if (_netTurnTimer.Value <= 0f)
        {
            PlayerTurnEndServerRpc();
        }
    }

    public override void OnNetworkSpawn()
    {
        // 연결이 끊어졌을 때 이벤트
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisConnect;

        // 턴이 바뀔 때 이벤트
        _netTurnIndex.OnValueChanged += (prev, next) =>
        {
            Debug.Log($"[IngameManager] 턴 카운트 변경: {prev} → {next}");

            FindCurrentTurnPlayerClientRpc(next);

            if (IsServer)
            {
                _netWindForce.Value = Mathf.Round(Random.Range(-_windForceMax, _windForceMax) * 100f) / 100f;
            }
        };

        // 바람이 바뀔 때 이벤트
        _netWindForce.OnValueChanged += (prev, next) =>
        {
            Debug.Log($"[IngameManager] 바람 세기 변경: {prev} → {next}");
            SetWindUIClientRpc(next);
        };

        if (IsServer)
        {
            _isGameStarted = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        // 씬이 바뀔 때 이벤트 해제
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisConnect;
    }

    void OnClientDisConnect(ulong clientId)
    {
        // 게임이 끝났을 땐 나가는 버튼이 있으므로 게임 도중만 처리
        if (!_isGameEnded)
        {
            // 네트워크가 끊기면 로비 씬으로 복귀
            NetworkPlayerData.GameAborted();
            StartCoroutine(LeaveGameAsync());
        }
    }

    public void StartGame()
    {
        GetRandomWindForce();
        _netTurnTimer.Value = _turnTime;
        _isGameStarted = true;
    }

    public IEnumerator LeaveGameAsync()
    {
        Debug.Log("Leave game and back to lobby.");

        // 연결 해제
        NetworkManager singleton = NetworkManager.Singleton;
        singleton.Shutdown();

        // 네트워크 종료까지 대기
        while (singleton.ShutdownInProgress || singleton.IsListening)
        {
            yield return null;
        }

        // 본인만 로비 씬 로드
        SceneManager.LoadSceneAsync(_lobbySceneName, LoadSceneMode.Single);
    }

    public IEnumerator SetHostToLobby()
    {
        bool isListening = NetworkManager.Singleton.IsListening;
        if (isListening)
        {
            SetHostToLobbyClientRpc();
        }

        // 호스트 복귀 알림이 보내질 때까지 대기
        while (!HostBackToLobby && isListening)
        {
            yield return null;
        }

        StartCoroutine(LeaveGameAsync());
    }

    [ClientRpc]
    void SetHostToLobbyClientRpc()
    {
        // 호스트 로비 복귀 알림
        HostBackToLobby = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayerTurnEndServerRpc()
    {
        Debug.Log("턴 종료.");
        _isTurnWait = true;
        
        StartCoroutine(StartNextPlayerTurn());
    }

    private IEnumerator StartNextPlayerTurn()
    {
        // 포탄이 사라질 때까지 대기
        while (CurShellTrans != null)
            yield return null;

        // 약간의 지연 시간
        yield return new WaitForSeconds(TURN_END_TERM);

        if (!_isGameStarted)
        {
            Debug.Log("게임 종료됨 턴이동 금지");
            yield break;
        }

        GetRandomWindForce();

        // 턴 이동
        _isTurnWait = false;
        MoveTurn();
    }

    private void MoveTurn()
    {
        Debug.Log("턴 이동.");
        _netTurnTimer.Value = _turnTime;
        _netTurnIndex.Value = (_netTurnIndex.Value + 1) % NetworkPlayerData.GetMaxPlayer();
    }

    public void SetStartTurnIndex()
    {
        Debug.Log("시작 턴 결정.");
        int startTurn = UnityEngine.Random.Range(0, NetworkPlayerData.GetMaxPlayer());
        _netTurnIndex.Value = startTurn;

        _netTurnTimer.Value = _turnTime;
        _isGameStarted = true;
    }

    public void GetRandomWindForce()
    {
        if (IsServer)
        {
            float randomWind = Mathf.Round(Random.Range(-_windForceMax, _windForceMax) * 100f) / 100f;
            _netWindForce.Value = randomWind;
            Debug.Log($"[IngameManager] 새로운 바람 설정: {randomWind}");

            SetWindUIClientRpc(randomWind);
        }
    }

    [ClientRpc]
    void SetWindUIClientRpc(float windForce)
    {
        IngameUIController.Instance.SetWind(windForce);
    }

    [ClientRpc]
    public void FindCurrentTurnPlayerClientRpc(int turnIndex)
    {
        Debug.Log("현재 턴의 플레이어 탐색.");

        if (CheckGameEndCondition())
        {
            Debug.Log("게임 종료");
            return;
        }

        var connectedClients = NetworkManager.Singleton.ConnectedClients;
        int counter = 0;
        Debug.Log($"현재 접속된 클라이언트: {connectedClients.Count}.");
        foreach(var client in connectedClients)
        {
            NetworkObject playerObject = client.Value.PlayerObject;
            PlayerController player = playerObject.GetComponent<PlayerController>();

            bool isTurn = turnIndex == counter;
            player.SetTurnMarkVisible(isTurn);
            if (isTurn)
            {
                PlayerCameraFocusing(player);
                bool isMyTurn = turnIndex == PlayerTurnNumber;
                Debug.Log($"현재 턴: {turnIndex}, 나의 턴: {PlayerTurnNumber}.");
                if (isMyTurn)
                {
                    Debug.Log($"나의 턴.");
                    player.SetMyTurn();
                    player.FillFuel();
                }
            }

            counter++;
        }
    }

    public float GetWindForce() => _netWindForce.Value;

    public void PlayerCameraFocusing(PlayerController playerController)
    {
        GameInitializer.Instance._camController.PlayerFocusing(playerController);
    }

    public bool IsMyTurn()
    {
        return _netTurnIndex.Value == PlayerTurnNumber;
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyDeathServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(senderId, out var client))
        {
            var playerObj = client.PlayerObject;
            if (playerObj != null)
            {
                Debug.Log($"서버: 플레이어 {senderId} 처치됨, 오브젝트 제거");
                StopTankMoveLoopClientRpc();
                playerObj.Despawn(true);
            }
        }

        CheckGameEndCondition();
    }


    [ClientRpc]
    public void StopTankMoveLoopClientRpc()
    {
        SoundManager.Instance.StopTankMoveLoop();
    }

    bool CheckGameEndCondition()
    {
        // 최소한 게임이 시작됐다는것을 확인하고 나서 종료조건 확인
        if (_isGameStarted == false)
            return false;

        var alivePlayers = new List<NetworkObject>();
        foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
        {
            var obj = kvp.Value.PlayerObject;
            if (obj != null && obj.IsSpawned)
            {
                alivePlayers.Add(obj);
            }
        }
       
        Debug.Log($"[게임 상태] 생존한 플레이어 수: {alivePlayers.Count}");

        if (alivePlayers.Count <= 1)
        {
            Debug.Log("[CheckGameEndCondition] 게임 종료 조건 만족, EndGame 호출");
            EndGame(alivePlayers.Count == 1 ? alivePlayers[0].OwnerClientId : (ulong?)null);
            return true;
        }

        return false;
    }

    void EndGame(ulong? winnerClientId)
    {
        NotifyGameEndClientRpc(winnerClientId.HasValue ? winnerClientId.Value : ulong.MaxValue);

        _isGameStarted = false;
    }

    [ClientRpc]
    void NotifyGameEndClientRpc(ulong winnerId)
    {
        if (_alreadySavedBattleInfo)
            return;

        _alreadySavedBattleInfo = true;

        _ = HandleGameEndResultAsync(winnerId);
    }

    async Task HandleGameEndResultAsync(ulong winnerId)
    {
        string resultKey;
        if (winnerId == ulong.MaxValue)
        {
            FirebaseManager._instance.addBattleInnfo("무");
            SoundManager.Instance.PlayDraw();
            resultKey = "무";
        }
        else if (NetworkManager.Singleton.LocalClientId == winnerId)
        {
            FirebaseManager._instance.addBattleInnfo("승");
            SoundManager.Instance.PlayWin();
            resultKey = "승";
        }
        else
        {
            FirebaseManager._instance.addBattleInnfo("패");
            SoundManager.Instance.PlayLose();
            resultKey = "패";
        }

        bool uploadSuccess = await FirebaseManager._instance.Update_UserBattleInfoAsync();
        if (uploadSuccess)
        {
            Debug.Log("Firebase 저장 완료");

            _resultUI.ShowResult(resultKey);
            _isGameEnded = true;
        }
        else
        {
            Debug.LogError("Firebase 저장 실패");
        }
    }

    public float GetTurnTime()
    {
        return Mathf.Max(0, _netTurnTimer.Value);
    }

    // 클라이언트가 선택한 포탄 인덱스를 서버에 전송하는 메소드
    public void SetSelectedShellIndex(int index)
    {
        if (IsServer)
        {
            _netSelectedShellIndex.Value = index; // 서버에서 직접 수정
            Debug.Log($"[서버] 선택된 포탄 인덱스 값 : {index}");
        }
        else
        {
            // 클라이언트는 ServerRpc를 통해 서버에 값을 전송
            SetSelectedShellIndexServerRpc(index);
            Debug.Log($"[클라이언트] 선택된 포탄 인덱스 값 : {index}");
        }
    }

    // 서버에서 클라이언트의 요청을 받아 포탄 인덱스를 설정
    [ServerRpc(RequireOwnership = false)]
    void SetSelectedShellIndexServerRpc(int index)
    {
        _netSelectedShellIndex.Value = index;
    }

    public int GetSelectShellIndex()
    {
        return _netSelectedShellIndex.Value;
    }
}

