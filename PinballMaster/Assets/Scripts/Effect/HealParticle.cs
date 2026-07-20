using System.Collections;
using UnityEngine;

public class HealParticle : MonoBehaviour
{
    [Header("Component")]
    [SerializeField] private ParticleSystem particle;

    [Header("Pool")]
    [SerializeField] private Object_Type objectType = Object_Type.Effect_Heal;

    private Coroutine returnCoroutine;
    private bool isReturning;

    private void Awake()
    {
        if (particle == null)
            particle = GetComponent<ParticleSystem>();
    }

    public void Init(Vector3 position)
    {
        if (particle == null)
        {
            ReturnToPool();
            return;
        }

        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }

        isReturning = false;

        transform.position = position;
        transform.rotation = Quaternion.identity;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play(true);

        returnCoroutine = StartCoroutine(ReturnRoutine());
    }

    private IEnumerator ReturnRoutine()
    {
        while (particle != null && particle.IsAlive(true))
            yield return null;

        returnCoroutine = null;
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (isReturning)
            return;

        isReturning = true;

        if (particle != null)
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (PoolManager.Instance != null)
            PoolManager.Instance.Return_Object(objectType, gameObject);
        else
            gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }

        isReturning = false;
    }
}