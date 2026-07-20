using System.Collections;
using UnityEngine;

public class IceEffect : MonoBehaviour
{
    [Header("Component")]
    [SerializeField] private ParticleSystem particle;

    [Header("Pool")]
    [SerializeField] private Object_Type objectType = Object_Type.Effect_Ice;

    [Header("Position")]
    [SerializeField] private Vector3 localOffset = Vector3.zero;

    private Transform target;
    private Coroutine returnCoroutine;
    private bool isReturning;

    private void Awake()
    {
        if (particle == null)
            particle = GetComponent<ParticleSystem>();
    }

    public void Init(Transform newTarget)
    {
        if (newTarget == null)
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
        target = newTarget;

        transform.SetParent(target, false);
        transform.localPosition = localOffset;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (particle == null)
        {
            ReturnToPool();
            return;
        }

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play(true);
    }

    public void StopEffect()
    {
        if (isReturning)
            return;

        if (!gameObject.activeInHierarchy)
        {
            ReturnToPool();
            return;
        }

        if (returnCoroutine != null)
            return;

        returnCoroutine = StartCoroutine(StopEffectRoutine());
    }

    private IEnumerator StopEffectRoutine()
    {
        transform.SetParent(null, true);
        target = null;

        if (particle != null)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            while (particle.IsAlive(true))
                yield return null;
        }

        returnCoroutine = null;
        ReturnToPool();
    }

    public void StopImmediately()
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (isReturning)
            return;

        isReturning = true;
        target = null;

        transform.SetParent(null, true);

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

        target = null;
        isReturning = false;

        if (particle != null)
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}