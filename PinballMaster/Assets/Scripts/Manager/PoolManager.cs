using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{

    [System.Serializable]
    public class PoolData
    {
        public Object_Type type;
        public GameObject prefab;
        public int maxCount = 10;
    }


    [SerializeField] List<PoolData> poolDatas; // Insert PoolData

    Dictionary<Object_Type, ObjectPool> Pools = new Dictionary<Object_Type, ObjectPool>(); // Convet


    public static PoolManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Create_ObjectPools();
    }


    public void Create_ObjectPools()
    {
        foreach (var data in poolDatas)
        {
            GameObject poolObj = new GameObject(data.type.ToString() + "_Pool");

            poolObj.transform.parent = transform;

            ObjectPool pool = poolObj.AddComponent<ObjectPool>();

            pool.Set_Prefab(data.prefab);
            pool.Set_MaxCount(data.maxCount);
            pool.Set_Objecttype(data.type);
            pool.Create_Pool();

            Pools.Add(data.type, pool);
        }

    }

    public GameObject Get_Object(Object_Type type)
    {
        if (Pools.TryGetValue(type, out ObjectPool pool))
        {
            return pool.Get_Object();
        }

        return null;
    }

    public void Return_Object(Object_Type type, GameObject obj)
    {

        if (Pools.TryGetValue(type, out var pool))
        {
            pool.Return_Object(obj);
        }
        else
        {
            Destroy(obj);
        }
    }

}

