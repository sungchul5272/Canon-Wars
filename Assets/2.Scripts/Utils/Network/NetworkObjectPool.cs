using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkObjectPool : NetworkBehaviour
{
    public static NetworkObjectPool Instance;

    private Dictionary<string, Queue<NetworkObject>> _netObjPools = new();

    void Awake()
    {
        Instance = this;
    }

    public NetworkObject CreateNetObj(NetworkObject prefab)
    {
        if (!IsServer)
        {
            return null;
        }

        string key = prefab.name;

        if (!_netObjPools.ContainsKey(key))
            _netObjPools[key] = new Queue<NetworkObject>();

        NetworkObject netObj;
        var pool = _netObjPools[key];

        if (pool.Count > 0)
        {
            netObj = pool.Dequeue();
        }
        else
        {
            netObj = Instantiate(prefab);
            netObj.name = key;
        }

        if (!netObj.IsSpawned)
        {
            netObj.Spawn();
        }
  
        ShowObjectClientRpc(netObj.NetworkObjectId);
        return netObj;
    }

    public void RemoveNetObj(NetworkObject netObj)
    {
        if (IsServer)
        {
            HideObjectClientRpc(netObj.NetworkObjectId);
        }
    }

    //[ServerRpc(RequireOwnership = false)]
    //public void ShowObjectServerRpc(ulong networkObjectId)
    //{
    //    ShowObjectClientRpc(networkObjectId);
    //}

    [ClientRpc]
    public void ShowObjectClientRpc(ulong networkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var netObj))
        {
            netObj.gameObject.SetActive(true);
            Debug.Log($"Prefab: {netObj.gameObject} set to {netObj.gameObject.activeSelf}");
        }
    }

    //[ServerRpc(RequireOwnership = false)]
    //public void HideObjectServerRpc(ulong networkObjectId)
    //{
    //    HideObjectClientRpc(networkObjectId);
    //}

    [ClientRpc]
    public void HideObjectClientRpc(ulong networkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var netObj))
        {
            netObj.gameObject.SetActive(false);
            //Debug.Log($"Prefab: {netObj.gameObject} set to {netObj.gameObject.activeSelf}");

            if (IsServer)
            {
                string key = netObj.name;

                if (!_netObjPools.ContainsKey(key))
                    _netObjPools[key] = new Queue<NetworkObject>();

                _netObjPools[key].Enqueue(netObj);
            }
        }
    }
}
