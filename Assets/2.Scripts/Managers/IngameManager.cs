using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public class IngameManager : NetworkBehaviour
{
    public static IngameManager Instance;

    [Header("턴 설정")]
    [SerializeField] float _turnTime = 40f;
    [SerializeField] Text _turnTimerText;

    [Header("Environment")]
    [Range(0f, 10f)]
    [SerializeField] private float _windForceMax = 0f;

    public Transform CurShellTrans { get; set; }

    public int playerNumber = -1;

    private const float TURN_END_TERM = 3f;

    NetworkVariable<float> _netWindForce = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    NetworkVariable<float> _netTurnTimer = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    NetworkVariable<int> _netTurnIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    bool _isGameStarted = false;
    bool _isTurnWait = false;
    bool _alreadySavedBattleInfo = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        _netTurnIndex.OnValueChanged += (prev, next) =>
        {
            Debug.Log($"[IngameManager] 턴 카운트 변경: {prev} → {next}");

            FindCurrentTurnPlayerClientRpc(next);

            if (IsServer)
            {
                _netWindForce.Value = Mathf.Round(Random.Range(-_windForceMax, _windForceMax) * 100f) / 100f;
            }
        };

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

    public void StartGame()
    {
        GetRandomWindForce();
        _netTurnTimer.Value = _turnTime;
        _isGameStarted = true;
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
            Debug.Log("게임 지속");
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
                bool isMyTurn = turnIndex == playerNumber;
                Debug.Log($"현재 턴: {turnIndex}, 나의 턴: {playerNumber}.");
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
    public bool IsCurPlayerTurnWait() => _isTurnWait;

    public void PlayerCameraFocusing(PlayerController playerController)
    {
        GameInitializer.Instance._camController.PlayerFocusing(playerController);
    }

    public bool IsMyTurn()
    {
        return _netTurnIndex.Value == playerNumber;
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
                playerObj.Despawn(true);
            }
        }

        CheckGameEndCondition();
    }
    bool CheckGameEndCondition()
    {
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

    private async Task HandleGameEndResultAsync(ulong winnerId)
    {
        string resultKey = "";

        if (winnerId == ulong.MaxValue)
        {
            FirebaseManager._instance.addBattleInnfo("무");
            resultKey = "무";
        }
        else if (NetworkManager.Singleton.LocalClientId == winnerId)
        {
            FirebaseManager._instance.addBattleInnfo("승");
            resultKey = "승";
        }
        else
        {
            FirebaseManager._instance.addBattleInnfo("패");
            resultKey = "패";
        }

        bool uploadSuccess = await FirebaseManager._instance.Update_UserBattleInfoAsync();

        if (uploadSuccess)
        {
            Debug.Log("Firebase 저장 완료");

            if (ResultUI.Instance != null)
            {
                ResultUI.Instance.ShowResult(resultKey);
            }
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
}

