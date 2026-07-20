using UnityEngine;
using UnityEngine.UI;

public class FireBall : Ball
{
    protected override string SkillName => "Fireball";

    [Header("Fallback Setting")]
    [SerializeField] private int defaultDamage = 21;
    [SerializeField] private float defaultBurnDuration = 4f;
    [SerializeField] private int defaultBurnDamagePerSecond = 8;
    [SerializeField] private int defaultMaxBurnStack = 3;

    private float burnDuration;
    private int burnDamagePerSecond;
    private int maxBurnStack;
    private int currentSkillLevel;

    protected override void Awake()
    {
        base.Awake();

        type = Object_Type.Ball_Fire;
        ApplyDefaultData();
    }

    protected override void RefreshSkillLevelData()
    {
        ApplySkillLevelData();
    }

    public override void Init(MyPlayer owner, Object_Type objType, int playerAttack)
    {
        base.Init(owner, objType, playerAttack);

        type = Object_Type.Ball_Fire;
        ApplySkillLevelData();
    }

    private void ApplyDefaultData()
    {
        attackPoint = defaultDamage;
        burnDuration = defaultBurnDuration;
        burnDamagePerSecond = defaultBurnDamagePerSecond;
        maxBurnStack = defaultMaxBurnStack;
        currentSkillLevel = 1;
    }

    private void ApplySkillLevelData()
    {
        ApplyDefaultData();

        if (ChoiceManager.Instance == null)
        {
            Debug.LogWarning("ChoiceManager.Instance is null.");
            return;
        }

        int ownedLevel = ChoiceManager.Instance.Get_OwnedSkillLevel(SkillName);

        if (ownedLevel <= 0)
        {
            Debug.LogWarning($"{SkillName} is not owned.");
            return;
        }

        currentSkillLevel = ownedLevel;

        if (SkillManager.Instance == null)
        {
            Debug.LogWarning("SkillManager.Instance is null.");
            return;
        }

        SkillData skillData = SkillManager.Instance.GetSkillData(SkillName, currentSkillLevel);

        if (skillData == null)
            return;

        attackPoint = skillData.damage;
        burnDuration = skillData.duration;
        burnDamagePerSecond = skillData.dotDamage;
        maxBurnStack = skillData.maxStack;

        Debug.Log($"{SkillName} Lv.{currentSkillLevel} Applied | Damage: {attackPoint}, Duration: {burnDuration}, DOT: {burnDamagePerSecond}, MaxStack: {maxBurnStack}");
    }

    protected override void HitEnemy(GameObject enemyObj)
    {
        Enemy enemy = enemyObj.GetComponent<Enemy>();

        if (enemy == null)
            return;

        int rawDamage = attackPoint + additionalAttackPoint;
        int finalDamage = CalculateFinalDamage(rawDamage);

        enemy.ApplyFire(burnDuration, burnDamagePerSecond, maxBurnStack);
        SpawnHitEffect(transform.position);
        enemy.DamagedByBall(finalDamage, transform.position);
    }

    private void SpawnHitEffect(Vector3 position)
    {
        if (PoolManager.Instance == null)
            return;

        GameObject effectObject = PoolManager.Instance.Get_Object(Object_Type.Effect_Hit);

        if (effectObject == null)
            return;

        HitEffect hitEffect = effectObject.GetComponent<HitEffect>();

        if (hitEffect == null)
        {
            PoolManager.Instance.Return_Object(Object_Type.Effect_Hit, effectObject);
            return;
        }

        hitEffect.Init(position);
    }
}