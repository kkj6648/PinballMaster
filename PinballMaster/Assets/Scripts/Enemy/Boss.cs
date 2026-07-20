using System.Collections;
using UnityEngine;

public class Boss : Enemy
{
    [Header("Projectile Attack")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private float attackInterval = 5f;


    private Coroutine projectileAttackCoroutine;

    protected override void Awake()
    {
        base.Awake();

        type = Object_Type.Enemy_Boss;
        maxHP = 500;
        currentHP = maxHP;
        attackPoint = 30;
        expReward = 50;
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (projectileAttackCoroutine != null)
            StopCoroutine(projectileAttackCoroutine);

        projectileAttackCoroutine = StartCoroutine(ProjectileAttackRoutine());
    }

    protected override void OnDisable()
    {
        if (projectileAttackCoroutine != null)
        {
            StopCoroutine(projectileAttackCoroutine);
            projectileAttackCoroutine = null;
        }

        base.OnDisable();
    }

    private IEnumerator ProjectileAttackRoutine()
    {
        while (!IsDead)
        {
            yield return new WaitForSeconds(attackInterval);

            if (IsDead)
                break;

            FireProjectile();
        }

        projectileAttackCoroutine = null;
    }

    private void FireProjectile()
    {
        MyPlayer player = Find_Player();

        if (player == null || PoolManager.Instance == null)
            return;

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        Vector2 direction = ((Vector2)player.transform.position - (Vector2)spawnPosition).normalized;

        GameObject projectileObject = PoolManager.Instance.Get_Object(Object_Type.BossProjectile);

        if (projectileObject == null)
            return;

        projectileObject.transform.position = spawnPosition;
        projectileObject.transform.rotation = Quaternion.identity;
        projectileObject.SetActive(true);

        BossProjectile projectile = projectileObject.GetComponent<BossProjectile>();

        if (projectile == null)
        {
            PoolManager.Instance.Return_Object(Object_Type.BossProjectile, projectileObject);
            return;
        }

        EffectManager.Instance.SpawnBossMagicCircleEffect(transform.position);

        projectile.Init( transform.position , direction);
    }
}