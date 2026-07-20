using System.Collections;
using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private Object_Type type = Object_Type.BossProjectile;

    [SerializeField] private int damage = 20;
    [SerializeField] private float speed = 10f;

    private bool hasHit;
    private Coroutine lifeCoroutine;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    private void OnDisable()
    {
        if (lifeCoroutine != null)
        {
            StopCoroutine(lifeCoroutine);
            lifeCoroutine = null;
        }

        rb.linearVelocity = Vector2.zero;
        hasHit = false;
    }

    public void Init( Vector3 pos, Vector2 direction)
    {
     
        hasHit = false;

        rb.linearVelocity = direction.normalized * speed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.position = pos;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (lifeCoroutine != null)
            StopCoroutine(lifeCoroutine);

        lifeCoroutine = StartCoroutine(LifeRoutine());
    }

    private IEnumerator LifeRoutine()
    {
        yield return new WaitForSeconds(lifeTime);

        lifeCoroutine = null;
        ReturnToPool();
    }




    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHit)
            return;

        MyPlayer player = collision.gameObject.GetComponent<MyPlayer>();

        if (player == null)
            return;

        hasHit = true;
        player.Damaged(damage);

        ReturnToPool();
    }




    private void ReturnToPool()
    {
        rb.linearVelocity = Vector2.zero;
        PoolManager.Instance.Return_Object(type, gameObject);
    }
}