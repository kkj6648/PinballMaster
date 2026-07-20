using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{

    Queue<GameObject> queue = new Queue<GameObject>();

    private int MaxCount;
    private GameObject Prefab;
    private Object_Type Type;


    public void Set_Prefab(GameObject obj)
    {
        Prefab = obj;
    }

    public void Set_Objecttype(Object_Type type)
    {
        Type = type;
    }

    public void Set_MaxCount(int count)
    {
        MaxCount = count;
    }

    public void Create_Pool()
    {
        for (int i = 0; i < MaxCount; i++)
        {
            GameObject Obj = Instantiate(Prefab, transform);
            Obj.SetActive(false);
            queue.Enqueue(Obj);
        }
    }

    public GameObject Get_Object()
    {
        if (queue.Count == 0)
        {
            GameObject NewObj = Instantiate(Prefab, transform);
            NewObj.SetActive(true);
            return NewObj;
        }

        GameObject Obj = queue.Dequeue();
        Obj.SetActive(true);

        return Obj;
    }


    public void Return_Object(GameObject Object)
    {
        Object.SetActive(false);

        Object.transform.SetParent(this.gameObject.transform);
        queue.Enqueue(Object);
    }



}
