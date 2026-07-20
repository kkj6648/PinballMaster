using UnityEngine;

public class IceBall : Ball
{
    protected override string SkillName => "Iceball";

    [Header("Fallback Setting")]
    [SerializeField] private int defaultDamage = 25;
    [SerializeField] private float defaultFreezeChance = 0.3f;
    [SerializeField] private float defaultFreezeDuration = 5f;
    [SerializeField] private float defaultSlowMultiplier = 0.9f;
    [SerializeField] private float defaultBonusDamageRate = 0.1f;

    private float freezeChance;
    private float freezeDuration;
    private float slowMultiplier;
    private float bonusDamageRate;
    private int currentSkillLevel = 1;

    protected override void Awake()
    {
        base.Awake();

        type = Object_Type.Ball_Ice;
        ApplyDefaultData();
    }

    protected override void RefreshSkillLevelData()
    {
        ApplySkillLevelData();
    }

    public override void Init(MyPlayer owner, Object_Type objtype, int attack)
    {
        base.Init(owner, objtype, attack);

        type = Object_Type.Ball_Ice;
        ApplySkillLevelData();
    }

    private void ApplyDefaultData()
    {
        attackPoint = defaultDamage;
        freezeChance = defaultFreezeChance;
        freezeDuration = defaultFreezeDuration;
        slowMultiplier = defaultSlowMultiplier;
        bonusDamageRate = defaultBonusDamageRate;
        currentSkillLevel = 1;
    }

    private void ApplySkillLevelData()
    {
        ApplyDefaultData();

        if (ChoiceManager.Instance == null)
            return;

        int ownedLevel = ChoiceManager.Instance.Get_OwnedSkillLevel(SkillName);

        if (ownedLevel <= 0)
            return;

        currentSkillLevel = ownedLevel;

        if (SkillManager.Instance == null)
            return;

        SkillData skillData = SkillManager.Instance.GetSkillData(SkillName, currentSkillLevel);

        if (skillData == null)
            return;

        attackPoint = skillData.damage;
        freezeChance = skillData.rate * 0.01f;
        freezeDuration = skillData.duration;
        slowMultiplier = 1f - skillData.speed * 0.01f;
        bonusDamageRate = skillData.additionalDamage * 0.01f;
    }

    protected override void HitEnemy(GameObject enemyObj)
    {
        Enemy enemy = enemyObj.GetComponent<Enemy>();

        if (enemy == null)
            return;

        bool isFreezeSuccess = Random.value < freezeChance;
        int rawDamage = attackPoint + additionalAttackPoint;

        if (isFreezeSuccess)
        {
            rawDamage = Mathf.RoundToInt(rawDamage * (1f + bonusDamageRate));
            enemy.ApplyFreeze(freezeDuration, slowMultiplier);
        }

        int finalDamage = CalculateFinalDamage(rawDamage);
        enemy.DamagedByBall(finalDamage, transform.position);
    }
}