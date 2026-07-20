using UnityEngine;

public class ClusterBall : Ball
{
    protected override string SkillName => "Clusterball";

    [Header("Fallback Setting")]
    [SerializeField] private int defaultClusterDamage = 27;
    [SerializeField] private float defaultFragmentSpawnChance = 0.4f;
    [SerializeField] private int defaultFragmentDamage = 10;

    [Header("Fragment")]
    [SerializeField] private Object_Type fragmentType;

    private float fragmentSpawnChance;
    private int fragmentDamage;
    private int currentSkillLevel = 1;

    protected override void Awake()
    {
        base.Awake();
        ApplyDefaultData();
    }

    protected override void RefreshSkillLevelData()
    {
        ApplySkillLevelData();
    }

    public override void Init(MyPlayer owner, Object_Type objtype, int attack)
    {
        base.Init(owner, objtype, attack);
        ApplySkillLevelData();
    }

    private void ApplyDefaultData()
    {
        attackPoint = defaultClusterDamage;
        fragmentSpawnChance = defaultFragmentSpawnChance;
        fragmentDamage = defaultFragmentDamage;
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
        fragmentSpawnChance = skillData.rate * 0.01f;
        fragmentDamage = skillData.fragmentDamage;

        Debug.Log($"{SkillName} Lv.{currentSkillLevel} Applied | Damage: {attackPoint}, FragmentChance: {fragmentSpawnChance}, FragmentDamage: {fragmentDamage}");
    }

    protected override void HitEnemy(GameObject enemyObj)
    {
        Enemy enemy = enemyObj.GetComponent<Enemy>();

        if (enemy == null)
            return;

        int rawDamage = attackPoint + additionalAttackPoint;
        int finalDamage = CalculateFinalDamage(rawDamage);

        SpawnHitEffect(transform.position);
        enemy.DamagedByBall(finalDamage, transform.position);

        if (Random.value < fragmentSpawnChance)
            SpawnFragment();
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

    private void SpawnFragment()
    {
        if (PoolManager.Instance == null)
        {
            Debug.LogWarning("PoolManager.Instance is null.");
            return;
        }

        GameObject fragmentObj = PoolManager.Instance.Get_Object(fragmentType);

        if (fragmentObj == null)
        {
            Debug.LogWarning("Fragment pool object is null.");
            return;
        }

        Fragment fragment = fragmentObj.GetComponent<Fragment>();

        if (fragment == null)
        {
            Debug.LogWarning("Fragment component is null.");
            PoolManager.Instance.Return_Object(fragmentType, fragmentObj);
            return;
        }

        Vector2 randomDir = Random.insideUnitCircle.normalized;

        if (randomDir.sqrMagnitude <= 0.001f)
            randomDir = Vector2.up;

        Vector3 spawnPos = transform.position + (Vector3)(randomDir * 0.15f);

        fragment.InitFragment(Owner, fragmentType, spawnPos, randomDir, fragmentDamage);
    }
}