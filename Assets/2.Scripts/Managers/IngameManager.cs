using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class IngameManager : NetworkBehaviour
{
    public static IngameManager Instance;

    //[Header("로딩 UI")]
    //[SerializeField] GameObject _loadingUI;

    [Header("턴 설정")]
    [SerializeField] float _turnTime = 40f;
    [SerializeField] float _postAttackDelay = 10f;
    [SerializeField] Text _turnTimerText;

    [Header("Environment")]
    [Range(0f, 10f)]
    [SerializeField] private float _windForceMax = 0f;
    [SerializeField] private float _curWindForce = 0f;

    private const float TURN_END_TERM = 3f;

    bool _isMapSpawned = false;
    bool _isTankSpawned = false;
    bool _isGameStarted = false;
    bool _isAttackResolving = false;
    bool _isTurnWait = false;

    //NetworkVariable<ulong> _currentTurnClientId = new();
    NetworkVariable<float> _turnTimer = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    NetworkVariable<int> _netTurnIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    Dictionary<ulong, bool> _clientReadyDict = new();

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
        };

        //if (IsClient && !IsServer)
        //{
        //    _loadingUI.SetActive(true);
        //}

        if (IsServer)
        {
            _isGameStarted = false;
        }
    }

    public void InitMapDone() { _isMapSpawned = true; InitAllDOne(); }
    public void InitTankDone() { _isTankSpawned = true; InitAllDOne(); }

    void InitAllDOne()
    {
        if (_isMapSpawned && _isTankSpawned)
        {
            ReportClientReadyServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void ReportClientReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        _clientReadyDict[senderId] = true;

        if (AllClientsReady())
        {
            StartCoroutine(StartGameRoutine());
        }
    }

    bool AllClientsReady()
    {
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!_clientReadyDict.ContainsKey(clientId) || !_clientReadyDict[clientId]) return false;
        }
        return true;
    }

    IEnumerator StartGameRoutine()
    {
        yield return new WaitForSeconds(1f);

        _turnTimer.Value = _turnTime;

        _isGameStarted = true;
    }

    void Update()
    {
        if (!IsServer || !_isGameStarted || _isAttackResolving || _isTurnWait) return;

        _turnTimer.Value -= Time.deltaTime;
        if (_turnTimerText != null)
            _turnTimerText.text = Mathf.CeilToInt(_turnTimer.Value).ToString();

        if (_turnTimer.Value <= 0f)
        {
            // 반복 호출 방지
            _turnTimer.Value = 99999;

            // 턴 종료
            PlayerTurnEnd();
        }
    }

    public void NotifyAttackCompleted()
    {
        if (!IsMyTurn() || _isAttackResolving) return;
        StartCoroutine(DelayedEndTurn());
    }

    IEnumerator DelayedEndTurn()
    {
        _isAttackResolving = true;
        yield return new WaitForSeconds(_postAttackDelay);
        PlayerTurnEnd();
        _isAttackResolving = false;
    }

    public void PlayerTurnEnd()
    {
        _isTurnWait = true;
        StartCoroutine(StartNextPlayerTurn());
    }

    private IEnumerator StartNextPlayerTurn()
    {
        // 포탄이 사라질 때까지 대기
        while (GameInitializer.Instance.CurShellTrans != null)
            yield return null;

        // 약간의 지연 시간
        yield return new WaitForSeconds(TURN_END_TERM);

        // 턴 이동
        _isTurnWait = false;
        MoveTurnServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void MoveTurnServerRpc()
    {
        _turnTimer.Value = _turnTime;
        _netTurnIndex.Value = (_netTurnIndex.Value + 1) % NetworkPlayerData.GetMaxPlayer();
    }

    public void SetStartTurnIndex()
    {
        int startTurn = UnityEngine.Random.Range(0, NetworkPlayerData.GetMaxPlayer());
        _netTurnIndex.Value = startTurn;
    }

    [ClientRpc]
    public void FindCurrentTurnPlayerClientRpc(int turnIndex)
    {
        ulong clientId = NetworkManager.LocalClientId;

        foreach (PlayerController player in FindObjectsOfType<PlayerController>())
        {
            bool isCurrentTurn = (int)player.OwnerClientId == turnIndex;
            player.SetTurnMarkVisible(isCurrentTurn);
        }
        if ((ulong)turnIndex == clientId)
        {
            Debug.Log($"나의 턴.");
            NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            if (playerObject == null)
            {
                Debug.LogError("Player Object is null.");
            }

            if (!playerObject.TryGetComponent<PlayerController>(out var player))
            {
                Debug.LogError("Player Controller is null.");
            }

            GameInitializer.Instance.CurTurnPlayer = player;
            player.IsMyTurn();
            player.FillFuel();
            RandomWindForce();
            PlayerCameraFocusing(player);
        }
        else
        {
            Debug.Log($"Player {turnIndex}의 턴.");
        }
    }

    private void RandomWindForce()
    {
        _curWindForce = Mathf.Round(Random.Range(-_windForceMax, _windForceMax) * 100f) / 100f;
        Debug.Log($"바람 세기: {_curWindForce}");
        IngameUIController.Instance.SetWind(_curWindForce);
    }

    public float GetWindForce() => _curWindForce;
    public bool IsCurPlayerTurnWait() => _isTurnWait;

    public void PlayerCameraFocusing(PlayerController playerController)
    {
        GameInitializer.Instance._camController.PlayerFocusing(playerController);
    }

    public bool IsMyTurn()
    {
        return _netTurnIndex.Value == (int)NetworkManager.Singleton.LocalClientId;
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyDeadServerRpc(ulong clientId)
    {
        Debug.Log($"게임 종료: 클라이언트 {clientId} 사망");
        _isGameStarted = false;
    }
}

public static class MatchData
{
    public static string EnemyUID;
}
