using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DemoSpawnManager : MonoBehaviour
{
    public NetworkObject playerPrefab;

    public static DemoSpawnManager Instance { get; private set; }

    NetworkObject _player;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 메인 카메라 비활성화
        Camera.main.gameObject.SetActive(false);

        // 접속한 클라이언트 플레이어 소환
        NetworkManager singleton = NetworkManager.Singleton;
        singleton.OnClientConnectedCallback += (clientId) =>
        {
            SpawnPlayerRpc(clientId);
        };

        //호스트 플레이어 소환
        if (NetworkManager.Singleton.IsServer)
        {
            SpawnPlayerRpc(singleton.ConnectedClientsIds[0]);
        }
    }

    [Rpc(SendTo.Server)]
    void SpawnPlayerRpc(ulong clientId)
    {
        // 플레이어 소환
        _player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        _player.SpawnAsPlayerObject(clientId);
        Debug.Log($"Player spawned: {clientId}");
    }
}
