using System;
using System.Collections;
using UnityEngine;

public abstract class Enemy : MonoBehaviour
{

    [Header("Component")]
    [SerializeField] protected SpriteRenderer Sr;
    [SerializeField] protected Collider2D Col;
    [SerializeField] protected Rigidbody2D Rb;
    [SerializeField] protected Animator Anim;

    [Header("EnemyStat")]
    [SerializeField] protected int maxHP = 100;
    [SerializeField] protected int currentHP;
    [SerializeField] protected int attackPoint = 10;

    [Header("Move Setting")]
    [SerializeField] protected float moveSpeed = 0.05f;
    [SerializeField] protected Vector2 moveDir = Vector2.down;


    [Header("Spawn Protection")]
    [SerializeField] private LayerMask ballMask;
    [SerializeField] private float spawnIgnoreBallTime = 0.25f;

    [Header("Reward")]
    [SerializeField] protected int expReward = 0;


    [Header("Critical")]
    [SerializeField] private float criticalDamageMultiplier = 1.5f;

    [Header("Attack Move")]
    private float attackMoveSpeed = 20f;
    [SerializeField] private float attackArriveDistance = 0.05f;

    [Header("Death Explosion")]
    [SerializeField] private LayerMask enemyLayerMask;


    [Header("Explosion Multiplier")]
    [SerializeField] private float cellMultiplier = 2.5f;



    [Header("Hit Flash")]
    [SerializeField] private SpriteRenderer[] hitFlashRenderers;
    [SerializeField] private float hitFlashDuration = 0.08f;

    private static readonly int HitAmountId = Shader.PropertyToID("_HitAmount");
    private MaterialPropertyBlock hitPropertyBlock;



    // Effect
    private FireEffect activeFireEffect;
    private IceEffect activeIceEffect;

    // Animation
    private static readonly int IsDeadAnimHash = Animator.StringToHash("IsDead");
    private static readonly int DamageFBHash = Animator.StringToHash("IsDamage_FB");
    private static readonly int DamageLeftHash = Animator.StringToHash("IsDamage_L");
    private static readonly int DamageRightHash = Animator.StringToHash("IsDamage_R");
    private static readonly int IsAttackAnimHash = Animator.StringToHash("IsAttack");



#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool drawDeathExplosionRange = true;
    [SerializeField] private Color deathExplosionGizmoColor = new Color(1f, 0.4f, 0f, 0.25f);
#endif


    // Coroutine
    private Coroutine attackMoveCoroutine;
    private Coroutine spawnIgnoreCoroutine;
    private Coroutine hitFlashCoroutine;

    private readonly Collider2D[] ballHits = new Collider2D[16];


    protected WaveManager waveOwner;

    protected Object_Type type;
    protected Vector3 SpawnPos;



    protected bool IsDead = false;
    protected bool IsArrive = false;
    protected bool Attacking = false;
    protected bool ShowHPUI = false;


    // Status condition
    protected bool IsIced = false;
    protected bool IsFired = false;

    private int fireStack = 0;
    private float fireEndTime = 0f;

    private float originMoveSpeed;
    private Coroutine freezeCoroutine;
    private Coroutine firedCoroutine;


    private bool spawnProtected = false;

    // UI
    public Action OnHPChanged;

    #region Get Set

    public int Get_maxHP()
    {
        return maxHP;
    }

    public int Get_currentHP()
    {
        return currentHP;
    }

    public Object_Type Get_ObjectType()
    {
        return type;
    }

    public bool Get_ShowHPUI()
    {
        return ShowHPUI;
    }

    public int Get_ExpReward()
    {
        return expReward;
    }
    public void SetWaveOwner(WaveManager owner)
    {
        waveOwner = owner;
    }

    #endregion Get Set

    protected virtual void Awake()
    {
        if (Sr == null) Sr = GetComponent<SpriteRenderer>();
        if (Col == null) Col = GetComponent<Collider2D>();
        if (Rb == null) Rb = GetComponent<Rigidbody2D>();
        if (Anim == null) Anim = GetComponent<Animator>();


        if (Col != null)
            Col.isTrigger = true;

        hitPropertyBlock = new MaterialPropertyBlock();


    }

    protected virtual void OnEnable()
    {
        Init_Enemy();

        if (spawnIgnoreCoroutine != null)
        {
            StopCoroutine(spawnIgnoreCoroutine);
        }

        spawnIgnoreCoroutine = StartCoroutine(SpawnProtectionRoutine());
    }

    protected virtual void OnDisable()
    {
        if (spawnIgnoreCoroutine != null)
        {
            StopCoroutine(spawnIgnoreCoroutine);
            spawnIgnoreCoroutine = null;
        }

        if (attackMoveCoroutine != null)
        {
            StopCoroutine(attackMoveCoroutine);
            attackMoveCoroutine = null;
        }

        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = null;
        }

        SetHitAmount(0f);

        if (Col != null)
        {
            for (int i = 0; i < ballHits.Length; i++)
            {
                if (ballHits[i] != null)
                {
                    Physics2D.IgnoreCollision(Col, ballHits[i], false);
                    ballHits[i] = null;
                }
            }
        }

        if (freezeCoroutine != null)
        {
            StopCoroutine(freezeCoroutine);
            freezeCoroutine = null;
        }

        if (firedCoroutine != null)
        {
            StopCoroutine(firedCoroutine);
            firedCoroutine = null;
        }

        IsIced = false;
        IsFired = false;

        spawnProtected = false;

        moveSpeed = originMoveSpeed;

        activeIceEffect = null;
        activeFireEffect = null;

    }


    protected virtual void Init_Enemy()
    {
        currentHP = maxHP;

        IsDead = false;
        IsArrive = false;
        Attacking = false;
        ShowHPUI = false;

        SpawnPos = transform.position;

        originMoveSpeed = moveSpeed;

        if (Col != null)
        {
            Col.enabled = true;
        }

        ResetAnimator();

        OnHPChanged?.Invoke();
    }


    protected virtual void FixedUpdate()
    {
        if (IsDead)
            return;

        UpdateStatusColor();

        if (IsArrive)
        {
            if (!Attacking)
            {
                StartAttackAnimation();
            }

            return;
        }

        Move();
    }

    private void UpdateStatusColor()
    {
        if (IsFired)
        {
            Sr.color = Color.yellow;
        }
        else if (IsIced)
        {
            Sr.color = Color.cyan;
        }
        else
        {
            Sr.color = Color.white;
        }
    }


    public bool CanInteractWithBall()
    {
        return !spawnProtected && !IsDead;
    }


    protected virtual void Move()
    {
        Vector2 nextPos = Rb.position + moveDir.normalized * moveSpeed * Time.fixedDeltaTime;

        Rb.MovePosition(nextPos);
    }


    private void StartAttackAnimation()
    {
        if (IsDead)
            return;

        if (Attacking)
            return;

        Attacking = true;


        ResetDamageTriggers();

        if (Anim != null)
        {
            Anim.SetBool(IsAttackAnimHash, true);
        }
    }

    public void OnAttackAnimationEnd()
    {

        if (IsDead)
            return;

        if (!Attacking)
            return;

        if (attackMoveCoroutine != null)
        {
            StopCoroutine(attackMoveCoroutine);
        }

        attackMoveCoroutine =
        StartCoroutine(MoveToPlayerAndReturn());
    }

    private IEnumerator MoveToPlayerAndReturn()
    {
        MyPlayer player = Find_Player();
        Col.enabled = false;

        if (player == null)
        {
            Attacking = false;

            if (Anim != null)
            {
                Anim.SetBool(IsAttackAnimHash, false);
            }

            attackMoveCoroutine = null;
            Return_ToPool();
            yield break;
        }

        Vector3 targetPosition = player.transform.position;


        while (!IsDead)
        {
            float distance = Vector2.Distance(transform.position, targetPosition);

            if (distance <= attackArriveDistance)
                break;

            Vector3 nextPosition = Vector3.MoveTowards(transform.position, targetPosition, attackMoveSpeed * Time.deltaTime);


            if (Rb != null)
            {
                Rb.MovePosition(nextPosition);
            }
            else
            {
                transform.position = nextPosition;
            }

            yield return null;
        }

        attackMoveCoroutine = null;


        if (IsDead)
            yield break;

        transform.position = targetPosition;

        player.Damaged(attackPoint);

        Attacking = false;

        if (Anim != null)
        {
            Anim.SetBool(IsAttackAnimHash, false);
        }

        Return_ToPool();
    }

    public virtual void Damaged(int damage, bool isCritical = false)
    {
        if (IsDead)
            return;

        ShowHPUI = true;

        EffectManager.Instance.SpawnDamageEffect(transform.position, damage, isCritical);

        currentHP -= damage;

        if (currentHP <= 0)
        {
            currentHP = 0;
            Dead();
        }

        OnHPChanged?.Invoke();
    }

    private HitSide GetHitSide(Vector3 ballPosition)
    {
        Vector2 enemyForward = moveDir.sqrMagnitude > 0.001f ? moveDir.normalized : Vector2.down;

        Vector2 directionToBall = (Vector2)(ballPosition - transform.position);


        if (directionToBall.sqrMagnitude <= 0.001f)
            return HitSide.Side;

        directionToBall.Normalize();

        float dot = Vector2.Dot(enemyForward, directionToBall);


        const float threshold = 0.7071f; // cos 45¡Æ

        if (dot >= threshold)
            return HitSide.Front;

        if (dot <= -threshold)
            return HitSide.Back;

        return HitSide.Side;
    }

    public virtual void DamagedByBall(int damage, Vector3 ballPosition)
    {
        if (IsDead)
            return;

        int finalDamage = damage;

        HitSide hitSide = GetHitSide(ballPosition);

        float criticalRate = 0f;

        if (PassiveManager.Instance != null)
        {
            if (hitSide == HitSide.Front)
            {
                criticalRate = PassiveManager.Instance.GetFrontCriticalRate();

            }
            else if (hitSide == HitSide.Back)
            {
                criticalRate = PassiveManager.Instance.GetBackCriticalRate();
            }
        }

        bool isCritical = criticalRate > 0f && UnityEngine.Random.value < criticalRate;

        if (isCritical)
        {
            finalDamage = Mathf.RoundToInt(finalDamage * criticalDamageMultiplier);
        }

        bool willDie = currentHP - finalDamage <= 0;

        PlayHitFlash();

        if (!willDie && !Attacking)
            PlayDamageAnimation(ballPosition);

        Damaged(finalDamage, isCritical);

    }


    protected virtual void Dead()
    {
        if (IsDead)
            return;


        EffectManager.Instance.SpawnDustEffect(transform.position);
        EffectManager.Instance.SpawnStoneEffect(transform.position);


        IsDead = true;
        Attacking = false;

        if (attackMoveCoroutine != null)
        {
            StopCoroutine(attackMoveCoroutine);
            attackMoveCoroutine = null;
        }

        Debug.Log("Enemy Dead");

        if (Col != null)
        {
            Col.enabled = false;
        }

        if (Anim != null)
        {
            ResetDamageTriggers();

            Anim.SetBool(IsAttackAnimHash, false);
            Anim.SetBool(IsDeadAnimHash, true);
        }

        ApplyDeathExplosion();

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.AddExp(expReward);
        }

        StopFireEffect();
        StopIceEffect(true);
    }


    public void OnDeathAnimationEnd()
    {

        Debug.Log("Enemy_Death ->  Return Pool");

        if (!IsDead)
            return;

        Return_ToPool();
    }

    private void ApplyDeathExplosion()
    {
        if (PassiveManager.Instance == null)
            return;

        int explosionDamage = PassiveManager.Instance.GetDeathExplosionDamage();

        if (explosionDamage <= 0)
            return;

        if (WaveManager.Instance == null)
        {
            Debug.LogWarning(
            "WaveManager.Instance is null."
            );

            return;
        }

        Vector3 explosionPosition = transform.position;


        EffectManager.Instance.SpawnBoomEffect(explosionPosition);

        float cellSize = WaveManager.Instance.Get_CellSize();


        // Range -> 3x3 => limit range
        Vector2 explosionSize = new Vector2(cellSize * cellMultiplier, cellSize * cellMultiplier);

        Physics2D.SyncTransforms();

        Collider2D[] hits = Physics2D.OverlapBoxAll(explosionPosition, explosionSize, 0f, enemyLayerMask);



        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null)
                continue;

            Enemy target =
            hit.GetComponent<Enemy>();

            if (target == null)
                continue;

            if (target == this)
                continue;

            if (target.IsDead)
                continue;

            target.Damaged(explosionDamage);
        }

    }


    public void Setting_ReUse()
    {
        IsDead = false;
        IsArrive = false;
        Attacking = false;
        ShowHPUI = false;
        Col.enabled = true;

        currentHP = maxHP;
    }



    private IEnumerator SpawnProtectionRoutine()
    {
        spawnProtected = true;

        yield return new WaitForSeconds(spawnIgnoreBallTime);

        spawnProtected = false;
        spawnIgnoreCoroutine = null;
    }


    private IEnumerator SpawnIgnoreBallRoutine()
    {
        if (Col == null)
            yield break;

        Physics2D.SyncTransforms();

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(ballMask);
        filter.useTriggers = true;

        int count = Physics2D.OverlapBox(Col.bounds.center, Col.bounds.size, 0f, filter, ballHits);


        for (int i = 0; i < count; i++)
        {
            if (ballHits[i] != null)
            {
                Physics2D.IgnoreCollision(Col, ballHits[i], true);
            }
        }

        float startTime = Time.time;
        float maxIgnoreTime = 1.5f;

        while (Time.time < startTime + maxIgnoreTime)
        {
            bool stillOverlapping = false;

            for (int i = 0; i < count; i++)
            {
                if (ballHits[i] == null)
                    continue;

                if (Col.bounds.Intersects(ballHits[i].bounds))
                {
                    stillOverlapping = true;
                    break;
                }
            }

            if (!stillOverlapping && Time.time >= startTime + spawnIgnoreBallTime)
            {
                break;
            }

            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            if (ballHits[i] != null)
            {
                Physics2D.IgnoreCollision(Col, ballHits[i], false);
                ballHits[i] = null;
            }
        }

        spawnIgnoreCoroutine = null;
    }



    #region  Status  Condition

    public virtual void ApplyFreeze(float duration, float speedMultiplier)
    {
        if (IsDead)
            return;

        if (!gameObject.activeInHierarchy)
            return;


        if (freezeCoroutine != null)
        {
            StopCoroutine(freezeCoroutine);
            freezeCoroutine = null;
        }

        IsIced = true;


        if (activeIceEffect == null && EffectManager.Instance != null)
        {
            activeIceEffect = EffectManager.Instance.SpawnIceEffect(transform);
        }

        freezeCoroutine = StartCoroutine(FreezeRoutine(duration, speedMultiplier));
    }

    private IEnumerator FreezeRoutine(float duration, float speedMultiplier)
    {
        IsIced = true;

        moveSpeed = originMoveSpeed * speedMultiplier;

        yield return new WaitForSeconds(duration);

        IsIced = false;
        moveSpeed = originMoveSpeed;

        freezeCoroutine = null;

        StopIceEffect();
    }

    private void StopIceEffect(bool immediately = false)

    {
        if (activeIceEffect == null)
            return;

        if (immediately)
        {
            activeIceEffect.StopImmediately();
        }
        else
        {
            activeIceEffect.StopEffect();
        }

        activeIceEffect = null;
    }


    public virtual void ApplyFire(float duration, int damagePerSecond, int maxStack)
    {
        if (IsDead)
            return;

        if (!gameObject.activeInHierarchy)
            return;

        IsFired = true;

        fireStack = Mathf.Min(fireStack + 1, maxStack);


        fireEndTime = Time.time + duration;


        if (activeFireEffect == null && EffectManager.Instance != null)
        {
            activeFireEffect = EffectManager.Instance.SpawnFireEffect(transform);
        }

        if (firedCoroutine == null)
        {
            firedCoroutine = StartCoroutine(FireRoutine(damagePerSecond));
        }
    }

    private IEnumerator FireRoutine(int damagePerSecond)
    {
        while (Time.time < fireEndTime)
        {
            yield return new WaitForSeconds(1f);

            if (IsDead)
                break;

            if (!gameObject.activeInHierarchy)
                break;

            int tickDamage = fireStack * damagePerSecond;

            Damaged(tickDamage);

            Debug.Log($"Fire Tick Damage: {tickDamage}, Stack: {fireStack}");
        }

        fireStack = 0;
        fireEndTime = 0f;
        IsFired = false;
        firedCoroutine = null;

        StopFireEffect();

    }

    private void StopFireEffect()
    {
        if (activeFireEffect == null)
            return;

        activeFireEffect.StopEffect();
        activeFireEffect = null;
    }


    #endregion Status Condition



    protected virtual void Return_ToPool()
    {

        StopFireEffect();
        StopIceEffect(true);

        waveOwner?.OnEnemyDead(this);

        PoolManager.Instance.Return_Object(type, gameObject);
    }


    private void ResetAnimator()
    {
        if (Anim == null)
            return;

        Anim.Rebind();
        Anim.Update(0f);

        Anim.ResetTrigger(DamageFBHash);
        Anim.ResetTrigger(DamageLeftHash);
        Anim.ResetTrigger(DamageRightHash);

        Anim.SetBool(IsDeadAnimHash, false);
        Anim.SetBool(IsAttackAnimHash, false);

        Anim.Play("Spawn", 0, 0f);
        Anim.Update(0f);
    }

    private void PlayDamageAnimation(Vector3 hitPosition)
    {
        if (Anim == null || IsDead)
            return;

        Vector2 enemyForward = moveDir.sqrMagnitude > 0.001f ? moveDir.normalized : Vector2.down;


        Vector2 directionToHit = (Vector2)(hitPosition - transform.position);


        if (directionToHit.sqrMagnitude <= 0.001f)
        {
            Anim.SetTrigger(DamageFBHash);
            return;
        }

        directionToHit.Normalize();

        float forwardDot = Vector2.Dot(enemyForward, directionToHit);


        const float frontBackThreshold = 0.7071f;

        ResetDamageTriggers();

        if (Mathf.Abs(forwardDot) >= frontBackThreshold)
        {
            Anim.SetTrigger(DamageFBHash);
            return;
        }


        Vector2 enemyRight = new Vector2(-enemyForward.y, enemyForward.x);


        float rightDot = Vector2.Dot(enemyRight, directionToHit);


        if (rightDot >= 0f)
        {
            Anim.SetTrigger(DamageRightHash);
        }
        else
        {
            Anim.SetTrigger(DamageLeftHash);
        }
    }

    private void ResetDamageTriggers()
    {
        if (Anim == null)
            return;

        Anim.ResetTrigger(DamageFBHash);
        Anim.ResetTrigger(DamageLeftHash);
        Anim.ResetTrigger(DamageRightHash);
    }

    private void PlayHitFlash()
    {
        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
        }

        hitFlashCoroutine = StartCoroutine(HitFlashRoutine());

    }
    private IEnumerator HitFlashRoutine()
    {
        SetHitAmount(0.2f);

        yield return new WaitForSeconds(hitFlashDuration);

        SetHitAmount(0f);

        hitFlashCoroutine = null;
    }

    private void SetHitAmount(float amount)
    {
        if (hitFlashRenderers == null)
            return;

        for (int i = 0; i < hitFlashRenderers.Length; i++)
        {
            SpriteRenderer renderer = hitFlashRenderers[i];


            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(hitPropertyBlock);

            hitPropertyBlock.SetFloat(HitAmountId, amount);

            renderer.SetPropertyBlock(hitPropertyBlock);


        }
    }

    public MyPlayer Find_Player()
    {

        MyPlayer obj = FindFirstObjectByType<MyPlayer>();

        if (obj == null) return null;

        return obj;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("ArriveZone"))
        {
            IsArrive = true;
        }
    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawDeathExplosionRange)
            return;

        WaveManager manager = WaveManager.Instance;

        if (manager == null)
        {
            manager = FindFirstObjectByType<WaveManager>();
        }

        if (manager == null)
            return;

        float cellSize = manager.Get_CellSize();

        Vector3 explosionSize = new Vector3(cellSize * cellMultiplier, cellSize * cellMultiplier, 0.05f);


        Gizmos.color = deathExplosionGizmoColor;
        Gizmos.DrawCube(transform.position, explosionSize);

        Gizmos.color = new Color(deathExplosionGizmoColor.r, deathExplosionGizmoColor.g, deathExplosionGizmoColor.b, 1f);

        Gizmos.DrawWireCube(transform.position, explosionSize);
    }
#endif



}