using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[Serializable]
public class ActiveSkillBallData
{
    public string skillName;
    public Object_Type ballType;
}

public class MyPlayer : MonoBehaviour
{
    [Header("Component")]
    [SerializeField] private Animator Anim;
    [SerializeField] private Collider2D Col;

    [Header("Visual")]
    [SerializeField] private Transform characterVisual;
    [SerializeField] private Transform wandPivot;

    [Header("Player Stat")]
    [SerializeField] private int maxHP = 300;
    [SerializeField] private int currentHP;
    [SerializeField] private int additionalAttackPoint = 0;

    [Header("Auto Heal")]
    [SerializeField] private bool useAutoHeal = true;
    [SerializeField] private int autoHealPer5Second = 5;

    [Header("Ball Shoot")]
    [SerializeField] private List<Object_Type> startBalls = new List<Object_Type>();
    [SerializeField] private Transform firePos;
    [SerializeField] private float shootInterval = 0.15f;

    [Header("Aim")]
    [SerializeField] private Aim Aim;
    [SerializeField] private float maxAngle = 80f;

    [Header("Wand Rotation")]
    [SerializeField] private bool smoothWandRotation = true;
    [SerializeField] private float wandRotateSpeed = 1000f;

    [Header("Head Aim")]
    [SerializeField] private Transform headAimPivot;
    [SerializeField] private float maxHeadAimAngle = 8f;
    [SerializeField] private float headRotateSpeed = 180f;
    [SerializeField] private float headAngleMultiplier = 0.15f;

    [Header("Player Facing")]
    [SerializeField] private bool originalVisualFacesLeft = true;
    [SerializeField] private float facingDeadZone = 0.1f;

    [Header("Active Skill Ball")]
    [SerializeField] private List<ActiveSkillBallData> activeSkillBalls = new List<ActiveSkillBallData>();

    [Header("Shoot Effect")]
    [SerializeField] private MagicCircleEffect magicCircleEffect;

    [Header("Damage Vibration")]
    [SerializeField] private bool useDamageVibration = true;


    private static readonly int IsAttackHash = Animator.StringToHash("IsAttack");
    private static readonly int IsDeathHash = Animator.StringToHash("IsDeath");

    private Coroutine shootCoroutine;
    private readonly Queue<Object_Type> balls = new Queue<Object_Type>();

    private bool IsDead;
    private bool IsStop = true;
    private bool isShooting;
    private bool isFacingRight;

    private Vector2 shootDir = Vector2.up;
    private Vector3 characterVisualBaseScale;

    public Action OnHPChanged;

    private bool blockAimPointerRelease;



    #region Get Set

    public Vector3 Get_FirePos()
    {
        if (firePos == null)
            return transform.position;

        return firePos.position;
    }

    public Collider2D Get_PlayerCollider()
    {
        return Col;
    }

    public int Get_currentHP()
    {
        return currentHP;
    }

    public int Get_maxHP()
    {
        return maxHP;
    }

    public int Get_AdditionalAttackPoint()
    {
        return additionalAttackPoint;
    }

    #endregion Get Set

    private void Awake()
    {
        if (Anim == null)
        {
            Anim = GetComponent<Animator>();
        }

        if (Col == null)
        {
            Col = GetComponent<Collider2D>();
        }

        if (characterVisual != null)
        {
            characterVisualBaseScale = characterVisual.localScale;
        }

        isFacingRight = !originalVisualFacesLeft;
        currentHP = maxHP;
    }

    private void Start()
    {
        Init_Balls();

        if (useAutoHeal)
        {
            StartCoroutine(AutoHealRoutine());
        }

        if (ChoiceManager.Instance != null)
        {
            ChoiceManager.Instance.OnSkillSelected += OnSkillSelected;
        }

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnNextWaveNoticeEnd += OnWaveNoticeEnd;
        }

        UpdatePlayerFacing();
        UpdateWandRotation(true);
    }

    private void Update()
    {
        if (IsDead)
        {
            HideAim();
            return;
        }

        if (IsStop)
        {
            HideAim();
            return;
        }

        UpdateShootDirection();
        UpdatePlayerFacing();
        UpdateWandRotation(false);
        UpdateHeadRotation();

        if (Aim != null && firePos != null)
        {
            Aim.DrawAim(firePos.position, shootDir);
        }

        if (!isShooting && balls.Count > 0)
        {
            shootCoroutine = StartCoroutine(Shoot_AllBalls_Coroutine());
        }
    }

    #region Ball Queue

    private void Init_Balls()
    {
        balls.Clear();

        for (int i = 0; i < startBalls.Count; i++)
        {
            balls.Enqueue(startBalls[i]);
        }
    }

    #endregion Ball Queue

    #region Aim Direction

    private void UpdateShootDirection()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (blockAimPointerRelease)
        {
            if (Mouse.current == null || !Mouse.current.leftButton.isPressed)
                blockAimPointerRelease = false;

            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            shootDir = Get_DirFromScreenPos(Mouse.current.position.ReadValue(), maxAngle);
        }
#endif

#if UNITY_ANDROID || UNITY_IOS
        if (blockAimPointerRelease)
        {
            if (Touchscreen.current == null || !Touchscreen.current.primaryTouch.press.isPressed)
                blockAimPointerRelease = false;

            return;
        }

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            int touchId = touch.touchId.ReadValue();

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touchId))
                return;

            if (touch.press.isPressed)
            {
                shootDir = Get_DirFromScreenPos(touch.position.ReadValue(), maxAngle);
            }
        }
#endif

        if (shootDir.sqrMagnitude <= 0.001f)
            shootDir = Vector2.up;
    }


    public void BlockAimUntilPointerRelease()
    {
        blockAimPointerRelease = true;
    }

    private Vector2 Get_DirFromScreenPos(Vector2 screenPosition, float limitAngle)
    {
        Camera cameraObject = Camera.main;

        if (cameraObject == null)
            return Vector2.up;

        Vector3 aimOrigin = transform.position;
        float zDistance = Mathf.Abs(cameraObject.transform.position.z - aimOrigin.z);
        Vector3 worldPosition = cameraObject.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, zDistance));

        worldPosition.z = aimOrigin.z;

        Vector2 direction = worldPosition - aimOrigin;

        if (direction.sqrMagnitude <= 0.001f)
            return Vector2.up;

        direction.Normalize();

        float angle = Vector2.SignedAngle(Vector2.up, direction);

        if (Mathf.Abs(angle) > limitAngle)
        {
            if (Mathf.Abs(angle) > 179.5f)
            {
                angle = worldPosition.x >= aimOrigin.x ? -limitAngle : limitAngle;
            }
            else
            {
                angle = angle > 0f ? limitAngle : -limitAngle;
            }
        }

        return (Quaternion.Euler(0f, 0f, angle) * Vector2.up).normalized;
    }

    #endregion Aim Direction

    #region Visual Direction

    private void UpdatePlayerFacing()
    {
        if (characterVisual == null)
            return;

        if (Mathf.Abs(shootDir.x) <= facingDeadZone)
            return;

        bool newFacingRight = shootDir.x > 0f;

        if (newFacingRight == isFacingRight)
            return;

        isFacingRight = newFacingRight;

        Vector3 scale = characterVisualBaseScale;
        float absoluteScaleX = Mathf.Abs(characterVisualBaseScale.x);

        if (originalVisualFacesLeft)
        {
            scale.x = isFacingRight ? -absoluteScaleX : absoluteScaleX;
        }
        else
        {
            scale.x = isFacingRight ? absoluteScaleX : -absoluteScaleX;
        }

        characterVisual.localScale = scale;
    }

    private void UpdateWandRotation(bool immediately)
    {
        if (wandPivot == null)
            return;

        if (shootDir.sqrMagnitude <= 0.001f)
            return;

        float targetAngle = Vector2.SignedAngle(Vector2.up, shootDir);
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);

        if (immediately || !smoothWandRotation)
        {
            wandPivot.rotation = targetRotation;
            return;
        }

        wandPivot.rotation = Quaternion.RotateTowards(wandPivot.rotation, targetRotation, wandRotateSpeed * Time.deltaTime);
    }

    private void UpdateHeadRotation()
    {
        if (headAimPivot == null)
            return;

        if (shootDir.sqrMagnitude <= 0.001f)
            return;

        float aimAngle = Vector2.SignedAngle(Vector2.up, shootDir);
        float targetHeadAngle = aimAngle * headAngleMultiplier;

        targetHeadAngle = Mathf.Clamp(targetHeadAngle, -maxHeadAimAngle, maxHeadAimAngle);

        bool visualIsMirrored = characterVisual != null && characterVisual.localScale.x < 0f;

        if (visualIsMirrored)
        {
            targetHeadAngle = -targetHeadAngle;
        }

        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetHeadAngle);
        headAimPivot.localRotation = Quaternion.RotateTowards(headAimPivot.localRotation, targetRotation, headRotateSpeed * Time.deltaTime);
    }

    #endregion Visual Direction

    #region Wave

    private void OnWaveNoticeEnd()
    {
        IsStop = false;
        Debug.Log("Wave notice ended. Player shooting enabled.");
    }

    #endregion Wave

    #region Ball Shoot

    private IEnumerator Shoot_AllBalls_Coroutine()
    {
        isShooting = true;

        if (magicCircleEffect != null)
        {
            magicCircleEffect.PlayEffect();
        }

        int shootCount = balls.Count;
        Vector2 fixedShootDirection = shootDir;

        for (int i = 0; i < shootCount; i++)
        {
            if (IsDead || IsStop)
                break;

            Shoot_OneBall(fixedShootDirection);


            yield return new WaitForSeconds(shootInterval);
        }

        isShooting = false;
        shootCoroutine = null;
    }

    private void Shoot_OneBall(Vector2 direction)
    {
        if (balls.Count <= 0)
            return;

        if (PoolManager.Instance == null)
            return;

        Object_Type ballType = balls.Dequeue();
        GameObject ballObject = PoolManager.Instance.Get_Object(ballType);

        if (ballObject == null)
        {
            Debug.LogWarning($"Ball pool object is null: {ballType}");

            balls.Enqueue(ballType);
            return;
        }

        Ball ball = ballObject.GetComponent<Ball>();

        if (ball == null)
        {
            Debug.LogWarning($"Ball component is null: {ballType}");

            PoolManager.Instance.Return_Object(ballType, ballObject);
            balls.Enqueue(ballType);
            return;
        }

        Vector3 spawnPosition = firePos != null ? firePos.position : transform.position;

        ballObject.transform.position = spawnPosition;
        ballObject.transform.rotation = Quaternion.identity;

        ball.Init(this, ballType, additionalAttackPoint);

        if (Anim != null)
        {
            Anim.ResetTrigger(IsAttackHash);
            Anim.SetTrigger(IsAttackHash);
        }

        if (magicCircleEffect != null)
        {
            magicCircleEffect.PlayEffect();
        }

        ball.Shoot(direction);
    }

    public void Return_Ball(Object_Type ballType, GameObject ballObject)
    {
        if (PoolManager.Instance != null && ballObject != null)
        {
            PoolManager.Instance.Return_Object(ballType, ballObject);
        }

        balls.Enqueue(ballType);
    }

    #endregion Ball Shoot

    #region Skill

    private void OnSkillSelected(SkillData skillData)
    {
        if (skillData == null)
            return;

        if (string.Equals(skillData.name, "AddNormalball", StringComparison.OrdinalIgnoreCase))
        {
            balls.Enqueue(Object_Type.Ball_Normal);
            Debug.Log("Normal Ball Added");
            return;
        }

        string skillType = skillData.type?.Trim().ToLowerInvariant();

        if (skillType != "active")
            return;

        if (skillData.level != 1)
            return;

        AddSkillBall(skillData.name);
    }

    private void AddSkillBall(string skillName)
    {
        if (string.IsNullOrWhiteSpace(skillName))
            return;

        for (int i = 0; i < activeSkillBalls.Count; i++)
        {
            ActiveSkillBallData data = activeSkillBalls[i];

            if (data == null)
                continue;

            bool sameSkillName = string.Equals(data.skillName?.Trim(), skillName.Trim(), StringComparison.OrdinalIgnoreCase);

            if (!sameSkillName)
                continue;

            balls.Enqueue(data.ballType);
            Debug.Log($"Active Skill Ball Added: {skillName} -> {data.ballType}");
            return;
        }

        Debug.LogWarning($"Active skill ball mapping not found: [{skillName}]");
    }

    #endregion Skill

    #region HP

    private IEnumerator AutoHealRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);

            if (IsDead)
                continue;

            Heal(autoHealPer5Second);
        }
    }

    public void Heal(int healAmount)
    {
        if (IsDead)
            return;

        if (healAmount <= 0)
            return;

        if (currentHP >= maxHP)
            return;

        currentHP = Mathf.Min(currentHP + healAmount, maxHP);

        EffectManager.Instance.SpawnHealEffect(transform.position);

        OnHPChanged?.Invoke();
    }

    public void Damaged(int damage)
    {
        if (IsDead)
            return;

        if (damage <= 0)
            return;


        PlayDamageVibration();

        currentHP -= damage;

        if (currentHP <= 0)
        {
            currentHP = 0;
            Dead();
        }

        EffectManager.Instance.SpawnBloodEffect(transform.position);
        OnHPChanged?.Invoke();
    }

    private void PlayDamageVibration()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    if (useDamageVibration)
        Handheld.Vibrate();
#endif
    }


    private void Dead()
    {
        if (IsDead)
            return;

        IsDead = true;
        IsStop = true;
        isShooting = false;

        if (shootCoroutine != null)
        {
            StopCoroutine(shootCoroutine);
            shootCoroutine = null;
        }

        HideAim();

        if (Col != null)
        {
            Col.enabled = false;
        }

        if (Anim != null)
        {
            Anim.ResetTrigger(IsAttackHash);
            Anim.SetBool(IsDeathHash, true);
        }

        Debug.Log("Player Dead");
    }

    public void OnDeathAnimationEnd()
    {
        if (!IsDead)
            return;

        GameManager.Instance?.Defeat();
    }

    #endregion HP

    private void HideAim()
    {
        if (Aim != null)
        {
            Aim.Hide();
        }
    }

    private void OnDestroy()
    {
        if (ChoiceManager.Instance != null)
        {
            ChoiceManager.Instance.OnSkillSelected -= OnSkillSelected;
        }

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnNextWaveNoticeEnd -= OnWaveNoticeEnd;
        }
    }
}