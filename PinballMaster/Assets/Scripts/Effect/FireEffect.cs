using UnityEngine;

public class FireEffect : MonoBehaviour
{
    [Header("Component")]
    [SerializeField] private SpriteRenderer Sr;
    [SerializeField] private Animator Anim;

    [Header("Pool")]
    [SerializeField]
    private Object_Type objectType =
        Object_Type.Effect_Fire;

    [Header("Animation")]
    [SerializeField] private string animationName = "Fire";

    [Header("Position")]
    [SerializeField] private Vector3 localOffset = Vector3.zero;
   

    private Transform target;
    private bool isReturning;


    private void Awake()
    {
        if (Sr == null)
        {
            Sr = GetComponent<SpriteRenderer>();
        }

        if (Anim == null)
        {
            Anim = GetComponent<Animator>();
        }
    }


    public void Init(Transform newTarget)
    {
        if (newTarget == null)
        {
            ReturnToPool();
            return;
        }

        isReturning = false;
        target = newTarget;

        transform.SetParent(target);
        transform.localPosition = localOffset;
        transform.localRotation = Quaternion.identity;

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        PlayAnimation();
    }


    private void PlayAnimation()
    {
        if (Anim == null ||
            Anim.runtimeAnimatorController == null)
        {
            ReturnToPool();
            return;
        }

        Anim.enabled = true;
        Anim.Rebind();
        Anim.Update(0f);

        Anim.Play(
            animationName,
            0,
            0f
        );

        Anim.Update(0f);
    }


    public void StopEffect()
    {
        ReturnToPool();
    }


    private void ReturnToPool()
    {
        if (isReturning)
            return;

        isReturning = true;
        target = null;

        transform.SetParent(null, true);

        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Return_Object(
                objectType,
                gameObject
            );
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        target = null;
        isReturning = false;

    }

}