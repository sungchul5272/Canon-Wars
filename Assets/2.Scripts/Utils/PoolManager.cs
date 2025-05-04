using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class Pool
{
    private GameObject _prefab;
    private IObjectPool<GameObject> _pool;

    private Transform _root;

    //public Transform Root
    //{
    //    get
    //    {
    //        if (_root == null)
    //        {
    //            GameObject go = new GameObject();
    //            go.name = $"{_prefab.name} Pooling Root";

    //            _root = go.transform;
    //        }

    //        return _root;
    //    }
    //}

    public Pool(GameObject prefab)
    {
        _prefab = prefab;
        _pool = new ObjectPool<GameObject>(OnCreate, OnGet, OnRelease, OnDestroy);
    }

    public GameObject Pop()
    {
        if (_pool.Get() == null)
        {
            GameObject go = GameObject.Instantiate(_prefab);
            PoolManager.Instance.Push(go);
        }

        return _pool.Get();
    }

    public void Push(GameObject obj)
    {
        if (obj == null || obj.Equals(null))
            return;

        _pool.Release(obj);
    }

    private GameObject OnCreate()
    {
        GameObject go = GameObject.Instantiate(_prefab);

        //go.transform.parent = Root;
        go.name = _prefab.name;

        return go;
    }

    private void OnGet(GameObject obj)
    {
        if (obj != null && !obj.Equals(null))
            obj.SetActive(true);
    }

    private void OnRelease(GameObject obj)
    {
        if (obj != null && !obj.Equals(null))
            obj.SetActive(false);
    }

    private void OnDestroy(GameObject obj)
    {
        if (obj != null && !obj.Equals(null))
            GameObject.Destroy(obj);
    }
}

public class PoolManager
{
    private static PoolManager _instance;
    public static PoolManager Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = new PoolManager();
            }

            return _instance;
        }
    }

    Dictionary<string, Pool> _pools = new Dictionary<string, Pool>();

    public GameObject Pop(GameObject prefab)
    {
        if (_pools.ContainsKey(prefab.name) == false)
        {
            CreatePool(prefab);
        }

        return _pools[prefab.name].Pop();
    }

    public bool Push(GameObject prefab)
    {
        if (_pools.ContainsKey(prefab.name) == false)
            return false;

        _pools[prefab.name].Push(prefab);

        return true;
    }

    private void CreatePool(GameObject prefab)
    {
        Pool pool = new Pool(prefab);
        _pools.Add(prefab.name, pool);
    }

    public bool IsContainPooling(GameObject prefab)
    {
        if (_pools.ContainsKey(prefab.name))
        {
            return true;
        }

        return false;
    }

    public void Clear()
    {
        _pools.Clear();
    }
}
