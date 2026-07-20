using UnityEngine;

public class MagicCircleEffect : MonoBehaviour
{
    [Header("Component")]
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer sr;

    [Header("Animation")]
    [SerializeField] private string animationName = "MagicCircle";

    private void Awake()
    {
        if (anim == null)
            anim = GetComponent<Animator>();

        if (sr == null)
            sr = GetComponent<SpriteRenderer>();

        Hide();
    }

    public void PlayEffect()
    {
        gameObject.SetActive(true);

        if (sr != null)
            sr.enabled = true;

        if (anim == null)
            return;

        anim.enabled = true;
        anim.Play(animationName, 0, 0f);
        anim.Update(0f);
    }

    public void AnimationEnd()
    {
        Hide();
    }

    public void Hide()
    {
        if (sr != null)
            sr.enabled = false;

        gameObject.SetActive(false);
    }
}