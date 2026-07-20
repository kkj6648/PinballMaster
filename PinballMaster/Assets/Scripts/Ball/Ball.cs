using System;
using UnityEngine;

public abstract class Ball : MonoBehaviour
{
    [Header("Component")]
    [SerializeField] protected SpriteRenderer Sr;
    [SerializeField] protected Collider2D Col;
    [SerializeField] protected Rigidbody2D Rb;

    [Header("Ball Stat")]
    protected int attackPoint = 8;
    [SerializeField] protected float moveSpeed = 10f;
    [SerializeField] protected int additionalAttackPoint = 0;

    [Header("Return")]
    [SerializeField] private float returnLockTime = 0.15f;
    [SerializeField] private float returnWallCooldown = 0.2f;

    [Header("Safety")]
    [SerializeField] private float minVelocitySqr = 0.01f;
    [SerializeField] private float wallIntoThreshold = -0.01f;

    [Header("Enemy Bounce")]
    [SerializeField] private float enemyPushOut = 0.04f;

    protected MyPlayer Owner;
    protected Object_Type type;
    protected Vector2 moveDir;

    protected virtual string SkillName => null;
    protected virtual bool IsNormalBall => false;

    private float magicMirrorBonusRate;
    private bool isSkillEventSubscribed;

    private float shootTime;
    private float ignoreReturnWallUntil = -1f;

    private Collider2D ownerCol;
    private bool isIgnoringOwnerCollision = false;

    private Vector2 lastValidDir = Vector2.up;
    private Vector2 previousPhysicsPos;

    private int lastEnemyHitFrame = -1;

    #region Get Set

    public Object_Type Get_ObjectType()
    {
        return type;
    }

    #endregion Get Set

    protected virtual void Awake()
    {
        if (Sr == null)
            Sr = GetComponent<SpriteRenderer>();

        if (Col == null)
            Col = GetComponent<Collider2D>();

        if (Rb == null)
            Rb = GetComponent<Rigidbody2D>();

        if (Rb != null)
        {
            Rb.gravityScale = 0f;
            Rb.linearDamping = 0f;
            Rb.angularDamping = 0f;
            Rb.freezeRotation = true;
            Rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    protected virtual void OnEnable()
    {
        SubscribeSkillLevelEvent();
    }

    protected virtual void OnDisable()
    {
        UnsubscribeSkillLevelEvent();

        if (isIgnoringOwnerCollision)
        {
            IgnoreOwnerCollision(false);
        }
    }

    public virtual void Init(MyPlayer owner, Object_Type objtype, int attack)
    {
        Owner = owner;
        type = objtype;
        additionalAttackPoint = attack;

        moveDir = Vector2.zero;
        lastValidDir = Vector2.up;
        magicMirrorBonusRate = 0f;

        transform.position = owner.Get_FirePos();
        previousPhysicsPos = transform.position;

        if (Rb != null)
        {
            Rb.linearVelocity = Vector2.zero;
            Rb.angularVelocity = 0f;
            previousPhysicsPos = Rb.position;
        }

        ownerCol = owner.Get_PlayerCollider();

        IgnoreOwnerCollision(true);
        SubscribeSkillLevelEvent();
    }

    private void FixedUpdate()
    {
        if (Rb == null)
            return;

        previousPhysicsPos = Rb.position;

        if (moveDir.sqrMagnitude <= 0.001f)
            return;

        Vector2 velocity = Rb.linearVelocity;

        if (velocity.sqrMagnitude <= minVelocitySqr)
        {
            velocity = lastValidDir * moveSpeed;
        }

        Vector2 dir = velocity.normalized;

        moveDir = dir;
        lastValidDir = dir;

        Rb.linearVelocity = dir * moveSpeed;
        Rb.angularVelocity = 0f;
    }

    public virtual void Shoot(Vector2 dir)
    {
        shootTime = Time.time;

        if (dir.sqrMagnitude <= 0.001f)
            dir = Vector2.up;

        moveDir = dir.normalized;
        lastValidDir = moveDir;
        previousPhysicsPos = Rb.position;

        Rb.linearVelocity = moveDir * moveSpeed;
        Rb.angularVelocity = 0f;
    }

    private void SubscribeSkillLevelEvent()
    {
        if (isSkillEventSubscribed)
            return;

        if (ChoiceManager.Instance == null)
            return;

        ChoiceManager.Instance.OnSkillSelected += HandleSkillSelected;
        isSkillEventSubscribed = true;
    }

    private void UnsubscribeSkillLevelEvent()
    {
        if (!isSkillEventSubscribed)
            return;

        if (ChoiceManager.Instance != null)
        {
            ChoiceManager.Instance.OnSkillSelected -= HandleSkillSelected;
        }

        isSkillEventSubscribed = false;
    }

    private void HandleSkillSelected(SkillData selectedSkill)
    {
        if (selectedSkill == null)
            return;

        if (string.IsNullOrWhiteSpace(SkillName))
            return;

        bool isSameSkill = string.Equals(selectedSkill.name?.Trim(), SkillName.Trim(), StringComparison.OrdinalIgnoreCase);

        if (!isSameSkill)
            return;

        RefreshSkillLevelData();
    }

    protected virtual void RefreshSkillLevelData()
    {
        // override
    }

    #region Collision

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.TryGetComponent(out MyPlayer player))
        {
            if (player == Owner && CanReturn())
            {
                Return_ToOwner();
                return;
            }
        }

        if (collision.collider.CompareTag("Wall"))
        {
            if (isIgnoringOwnerCollision)
            {
                IgnoreOwnerCollision(false);
            }

            AddMagicMirrorBonus();
            CorrectWallBounceIfNeeded(collision);
            return;
        }
    }

    protected virtual void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Wall"))
        {
            CorrectWallBounceIfNeeded(collision);
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("ReturnWall"))
        {
            if (Time.time >= ignoreReturnWallUntil)
            {
                Return_Bounce();
            }

            return;
        }

        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponent<Enemy>();

        if (enemy == null)
            return;

        if (!enemy.CanInteractWithBall())
            return;

        if (lastEnemyHitFrame == Time.frameCount)
            return;

        lastEnemyHitFrame = Time.frameCount;

        HitEnemy(other.gameObject);
        BounceFromEnemy(other);
    }

    protected virtual void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("ReturnWall"))
            return;

        if (Time.time < ignoreReturnWallUntil)
            return;

        Return_Bounce();
    }

    #endregion Collision

    private void AddMagicMirrorBonus()
    {
        if (PassiveManager.Instance == null)
            return;

        float bonusRate = PassiveManager.Instance.GetMagicMirrorDamageRate();

        if (bonusRate <= 0f)
            return;

        magicMirrorBonusRate += bonusRate;
    }

    protected int CalculateFinalDamage(int rawDamage)
    {
        float finalDamage = rawDamage;

        if (IsNormalBall && PassiveManager.Instance != null)
        {
            float normalBallMultiplier = PassiveManager.Instance.GetNormalBallDamageMultiplier();
            finalDamage *= normalBallMultiplier;
        }

        if (magicMirrorBonusRate > 0f)
        {
            finalDamage *= 1f + magicMirrorBonusRate;
            magicMirrorBonusRate = 0f;
        }

        return Mathf.RoundToInt(finalDamage);
    }

    private void CorrectWallBounceIfNeeded(Collision2D collision)
    {
        Vector2 normal = GetBestWallNormal(collision);

        if (normal.sqrMagnitude <= 0.001f)
            return;

        Vector2 velocity = Rb.linearVelocity;
        Vector2 currentDir;

        if (velocity.sqrMagnitude > minVelocitySqr)
        {
            currentDir = velocity.normalized;
        }
        else
        {
            currentDir = lastValidDir.normalized;
        }

        float dot = Vector2.Dot(currentDir, normal);

        if (dot > 0.01f && velocity.sqrMagnitude > minVelocitySqr)
        {
            moveDir = currentDir;
            lastValidDir = currentDir;

            Rb.linearVelocity = currentDir * moveSpeed;
            Rb.angularVelocity = 0f;
            return;
        }

        if (dot < wallIntoThreshold || velocity.sqrMagnitude <= minVelocitySqr)
        {
            Vector2 reflectedDir = Vector2.Reflect(currentDir, normal).normalized;

            if (reflectedDir.sqrMagnitude <= 0.001f)
                reflectedDir = normal;

            moveDir = reflectedDir;
            lastValidDir = reflectedDir;

            Rb.linearVelocity = reflectedDir * moveSpeed;
            Rb.angularVelocity = 0f;
        }
    }

    private Vector2 GetBestWallNormal(Collision2D collision)
    {
        if (collision.contactCount <= 0)
            return -lastValidDir;

        Vector2 incomingDir = lastValidDir.sqrMagnitude > 0.001f ? lastValidDir.normalized : moveDir.normalized;
        Vector2 bestNormal = collision.contacts[0].normal.normalized;
        float bestDot = Vector2.Dot(incomingDir, bestNormal);

        for (int i = 1; i < collision.contactCount; i++)
        {
            Vector2 normal = collision.contacts[i].normal.normalized;
            float dot = Vector2.Dot(incomingDir, normal);

            if (dot < bestDot)
            {
                bestDot = dot;
                bestNormal = normal;
            }
        }

        return bestNormal.normalized;
    }

    protected virtual void BounceFromEnemy(Collider2D enemyCol)
    {
        Vector2 normal = GetEnemyBoxSurfaceNormal(enemyCol);

        if (normal.sqrMagnitude <= 0.001f)
            normal = -moveDir;

        Vector2 incomingDir = moveDir.sqrMagnitude > 0.001f ? moveDir.normalized : lastValidDir.normalized;

        if (Vector2.Dot(incomingDir, normal) > 0f)
        {
            normal = -normal;
        }

        Vector2 reflectedDir = Vector2.Reflect(incomingDir, normal).normalized;

        if (reflectedDir.sqrMagnitude <= 0.001f)
            reflectedDir = -incomingDir;

        moveDir = reflectedDir;
        lastValidDir = reflectedDir;

        Rb.linearVelocity = moveDir * moveSpeed;
        Rb.angularVelocity = 0f;
        Rb.position += normal * enemyPushOut;
    }

    private Vector2 GetEnemyBoxSurfaceNormal(Collider2D enemyCol)
    {
        Bounds bounds = enemyCol.bounds;
        float radius = GetBallRadiusWorld();

        bounds.Expand(new Vector3(radius * 2f, radius * 2f, 0f));

        Vector2 start = previousPhysicsPos;
        Vector2 end = Rb.position;

        if (TryGetSweptAabbNormal(bounds, start, end, out Vector2 normal))
        {
            return normal;
        }

        Vector2 dir = moveDir.sqrMagnitude > 0.001f ? moveDir.normalized : lastValidDir.normalized;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            return dir.x > 0f ? Vector2.left : Vector2.right;
        }

        return dir.y > 0f ? Vector2.down : Vector2.up;
    }

    private bool TryGetSweptAabbNormal(Bounds bounds, Vector2 start, Vector2 end, out Vector2 normal)
    {
        normal = Vector2.zero;

        Vector2 delta = end - start;

        if (delta.sqrMagnitude <= 0.000001f)
            return false;

        Vector3 start3 = new Vector3(start.x, start.y, bounds.center.z);

        if (bounds.Contains(start3))
        {
            Vector2 dir = delta.normalized;

            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                normal = dir.x > 0f ? Vector2.left : Vector2.right;
            }
            else
            {
                normal = dir.y > 0f ? Vector2.down : Vector2.up;
            }

            return true;
        }

        float tEnter = 0f;
        float tExit = 1f;
        Vector2 enterNormal = Vector2.zero;

        if (Mathf.Abs(delta.x) < 0.000001f)
        {
            if (start.x < bounds.min.x || start.x > bounds.max.x)
            {
                return false;
            }
        }
        else
        {
            float invX = 1f / delta.x;
            float tx1 = (bounds.min.x - start.x) * invX;
            float tx2 = (bounds.max.x - start.x) * invX;

            Vector2 nx1 = Vector2.left;
            Vector2 nx2 = Vector2.right;

            float txNear;
            float txFar;
            Vector2 nxNear;

            if (tx1 < tx2)
            {
                txNear = tx1;
                txFar = tx2;
                nxNear = nx1;
            }
            else
            {
                txNear = tx2;
                txFar = tx1;
                nxNear = nx2;
            }

            if (txNear > tEnter)
            {
                tEnter = txNear;
                enterNormal = nxNear;
            }

            tExit = Mathf.Min(tExit, txFar);

            if (tEnter > tExit)
                return false;
        }

        if (Mathf.Abs(delta.y) < 0.000001f)
        {
            if (start.y < bounds.min.y || start.y > bounds.max.y)
            {
                return false;
            }
        }
        else
        {
            float invY = 1f / delta.y;
            float ty1 = (bounds.min.y - start.y) * invY;
            float ty2 = (bounds.max.y - start.y) * invY;

            Vector2 ny1 = Vector2.down;
            Vector2 ny2 = Vector2.up;

            float tyNear;
            float tyFar;
            Vector2 nyNear;

            if (ty1 < ty2)
            {
                tyNear = ty1;
                tyFar = ty2;
                nyNear = ny1;
            }
            else
            {
                tyNear = ty2;
                tyFar = ty1;
                nyNear = ny2;
            }

            if (tyNear > tEnter)
            {
                tEnter = tyNear;
                enterNormal = nyNear;
            }

            tExit = Mathf.Min(tExit, tyFar);

            if (tEnter > tExit)
                return false;
        }

        if (tEnter < -0.01f || tEnter > 1.01f)
            return false;

        normal = enterNormal;

        return normal.sqrMagnitude > 0.001f;
    }

    private float GetBallRadiusWorld()
    {
        CircleCollider2D circle = Col as CircleCollider2D;

        if (circle != null)
        {
            float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            return circle.radius * scale;
        }

        if (Col != null)
        {
            return Mathf.Max(Col.bounds.extents.x, Col.bounds.extents.y);
        }

        return 0.25f;
    }

    protected virtual void Return_Bounce()
    {
        if (isIgnoringOwnerCollision)
        {
            IgnoreOwnerCollision(false);
        }

        if (Owner == null || Rb == null)
            return;

        Collider2D playerCollider = Owner.Get_PlayerCollider();
        Vector2 targetPos = playerCollider != null ? playerCollider.bounds.center : Owner.transform.position;
        Vector2 currentPos = Rb.position;
        Vector2 returnDir = targetPos - currentPos;

        if (returnDir.sqrMagnitude <= 0.001f)
            return;

        moveDir = returnDir.normalized;
        lastValidDir = moveDir;
        ignoreReturnWallUntil = Time.time + returnWallCooldown;

        Rb.linearVelocity = moveDir * moveSpeed;
        Rb.angularVelocity = 0f;
    }

    protected virtual void HitEnemy(GameObject enemyObj)
    {
        Enemy enemy = enemyObj.GetComponent<Enemy>();

        if (enemy == null)
            return;

        int rawDamage = attackPoint + additionalAttackPoint;
        int finalDamage = CalculateFinalDamage(rawDamage);

        EffectManager.Instance.SpawnHitEffect(transform.position);
        enemy.DamagedByBall(finalDamage, transform.position);
    }

    protected virtual void Return_ToOwner()
    {
        if (isIgnoringOwnerCollision)
        {
            IgnoreOwnerCollision(false);
        }

        moveDir = Vector2.zero;

        Rb.linearVelocity = Vector2.zero;
        Rb.angularVelocity = 0f;

        Owner.Return_Ball(type, gameObject);
    }

    protected virtual bool CanReturn()
    {
        return Time.time >= shootTime + returnLockTime;
    }

    private void IgnoreOwnerCollision(bool ignore)
    {
        if (Col == null || ownerCol == null)
            return;

        Physics2D.IgnoreCollision(Col, ownerCol, ignore);
        isIgnoringOwnerCollision = ignore;
    }
}