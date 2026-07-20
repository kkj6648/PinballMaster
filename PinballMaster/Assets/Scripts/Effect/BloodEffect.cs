using UnityEngine;

public class BloodEffect : MonoBehaviour
{
    [Header("Component")]
    [SerializeField] private SpriteRenderer Sr;
    [SerializeField] private Animator Anim;

    [Header("Pool")]
    [SerializeField] private Object_Type objectType = Object_Type.Effect_Blood;

    [Header("Animation")]
    [SerializeField] private string animationName = "Blood";

    private bool isReturning;

    private void Awake()
    {
        if (Sr == null)
            Sr = GetComponent<SpriteRenderer>();

        if (Anim == null)
            Anim = GetComponent<Animator>();
    }

    public void Init(Vector3 pos)
    {
        isReturning = false;

        transform.position = pos;
        transform.rotation = Quaternion.identity;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        PlayAnimation();
    }

    private void PlayAnimation()
    {
        if (Anim == null || Anim.runtimeAnimatorController == null)
        {
            ReturnToPool();
            return;
        }

        Anim.enabled = true;
        Anim.Rebind();
        Anim.Play(animationName, 0, 0f);

        if (gameObject.activeInHierarchy)
            Anim.Update(0f);
    }

    public void AnimationEnd()
    {
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (isReturning)
            return;

        isReturning = true;

        if (PoolManager.Instance != null)
            PoolManager.Instance.Return_Object(objectType, gameObject);
        else
            gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        isReturning = false;
    }
}