using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameInitializer : NetworkBehaviour
{
    public static GameInitializer Instance;

    [Header("¸Ê ¹× ÅÊÅ© ÇÁ¸®ÆÕ")]
    [SerializeField] private MapSpawner _mapSpawner;
    [SerializeField] private List<GameObject> _tankPrefabList;
    [SerializeField] private eMapType _selectedMapType = eMapType.Random;

    public Transform CurShellTrans { get; set; }
    public CameraController _camController { get; private set; }
    public PlayerController CurTurnPlayer { get; set; }

    private NetworkVariable<int> _netMapIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private List<Vector3> _spawnPosList = new();

    void Awake()
    {
        if (Instance == null)
            Instance = this;

        _camController = Camera.main.GetComponent<CameraController>();
    }

    void Start()
    {
        Init();
    }

    public override void OnNetworkSpawn()
    {
        _netMapIndex.OnValueChanged += (prev, next) =>
        {
            Debug.Log($"[GameInitializer] ¸Ê ÀÎµ¦½º º¯°æ: {prev} ¡æ {next}");
        };
    }

    public void Init(Action callback = null)
    {
        SpawnMap();
        SpawnPlayers();

        _camController?.Init();

        callback?.Invoke();
    }

    private void SpawnMap()
    {
        if (IsServer)
        {
            _netMapIndex.Value = UnityEngine.Random.Range((int)eMapType.Valley, (int)eMapType.Max);
        }

        _mapSpawner.SpawnSelectMap(_netMapIndex.Value);
        IngameManager.Instance.InitMapDone();
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
            Debug.LogWarning("[GameInitializer] ½ºÆù ÁÂÇ¥°¡ ºÎÁ·ÇÕ´Ï´Ù.");
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

        IngameManager.Instance.InitTankDone();
        Debug.Log($"[GameInitializer] Player {clientId} ½ºÆù À§Ä¡: {spawnPos}");

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
}
