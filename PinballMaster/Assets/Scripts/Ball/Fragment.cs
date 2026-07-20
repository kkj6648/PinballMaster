using UnityEngine;

public class Fragment : Ball
{
    protected override string SkillName => "Clusterball";

    [Header("Fallback Setting")]
    [SerializeField] private int defaultFragmentDamage = 10;

    private int currentFragmentDamage;

    protected override void Awake()
    {
        base.Awake();
        ApplyDefaultData();
    }

    protected override void RefreshSkillLevelData()
    {
        ApplySkillLevelData();
    }

    private void ApplyDefaultData()
    {
        currentFragmentDamage = defaultFragmentDamage;
        attackPoint = defaultFragmentDamage;
        additionalAttackPoint = 0;
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
            return;

        if (SkillManager.Instance == null)
        {
            Debug.LogWarning("SkillManager.Instance is null.");
            return;
        }

        SkillData skillData = SkillManager.Instance.GetSkillData(SkillName, ownedLevel);

        if (skillData == null)
            return;

        currentFragmentDamage = skillData.fragmentDamage;
        attackPoint = currentFragmentDamage;
    }

    public void InitFragment(MyPlayer owner, Object_Type objtype, Vector3 spawnPos, Vector2 dir, int damage)
    {
        base.Init(owner, objtype, 0);

        currentFragmentDamage = damage > 0 ? damage : defaultFragmentDamage;
        attackPoint = currentFragmentDamage;
        additionalAttackPoint = 0;

        transform.position = spawnPos;
        transform.rotation = Quaternion.identity;

        Shoot(dir);
    }

    protected override void HitEnemy(GameObject enemyObj)
    {
        Enemy enemy = enemyObj.GetComponent<Enemy>();

        if (enemy == null)
            return;

        int finalDamage = CalculateFinalDamage(currentFragmentDamage);

        enemy.DamagedByBall(finalDamage, transform.position);
        Debug.Log($"Fragment Hit Damage: {finalDamage}");
    }

    protected override void Return_ToOwner()
    {
        if (Rb != null)
        {
            Rb.linearVelocity = Vector2.zero;
            Rb.angularVelocity = 0f;
        }

        moveDir = Vector2.zero;

        if (PoolManager.Instance != null)
            PoolManager.Instance.Return_Object(type, gameObject);
    }
}