using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkObjectPool : NetworkBehaviour
{
    public static NetworkObjectPool Instance;

    Queue<NetworkObject> _netObjPools = new();

    void Awake()
    {
        Instance = this;
    }

    public NetworkObject CreateNetObj(NetworkObject prefab)
    {
        NetworkObject netObj;
        if (_netObjPools.Count > 0)
        {
            netObj = _netObjPools.Dequeue();
        }
        else
        {
            netObj = Instantiate(prefab);
        }

        if (!netObj.IsSpawned)
        {
            netObj.Spawn();
        }
  
        ShowObjectServerRpc(netObj.NetworkObjectId);
        return netObj;
    }

    public void RemoveNetObj(NetworkObject netObj)
    {
        HideObjectServerRpc(netObj.NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ShowObjectServerRpc(ulong networkObjectId)
    {
        ShowObjectClientRpc(networkObjectId);
    }

    [ClientRpc]
    public void ShowObjectClientRpc(ulong networkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var netObj))
        {
            netObj.gameObject.SetActive(false);
            Debug.Log($"Prefab: {netObj.gameObject} set to {netObj.gameObject.activeSelf}");

            if (IsServer)
            {
                _netObjPools.Enqueue(netObj);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void HideObjectServerRpc(ulong networkObjectId)
    {
        HideObjectClientRpc(networkObjectId);
    }

    [ClientRpc]
    public void HideObjectClientRpc(ulong networkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var netObj))
        {
            netObj.gameObject.SetActive(false);
            Debug.Log($"Prefab: {netObj.gameObject} set to {netObj.gameObject.activeSelf}");
        }
    }
}
