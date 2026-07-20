using UnityEngine;

public class LaserBall : Ball
{
    protected override string SkillName => "Laserball";

    [Header("Fallback Setting")]
    [SerializeField] private int defaultLaserDamage = 11;
    [SerializeField] private int defaultSameRowDamage = 7;
    [SerializeField] private bool includeHitEnemyInRowDamage = true;

    private int sameRowDamage;
    private int currentSkillLevel = 1;

    protected override void Awake()
    {
        base.Awake();

        type = Object_Type.Ball_Laser;
        ApplyDefaultData();
    }

    protected override void RefreshSkillLevelData()
    {
        ApplySkillLevelData();
    }

    public override void Init(MyPlayer owner, Object_Type objtype, int attack)
    {
        base.Init(owner, objtype, attack);

        type = Object_Type.Ball_Laser;
        ApplySkillLevelData();
    }

    private void ApplyDefaultData()
    {
        attackPoint = defaultLaserDamage;
        sameRowDamage = defaultSameRowDamage;
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
        sameRowDamage = skillData.rowDamage;

        Debug.Log($"{SkillName} Lv.{currentSkillLevel} Applied | Damage: {attackPoint}, RowDamage: {sameRowDamage}");
    }

    protected override void HitEnemy(GameObject enemyObj)
    {
        Enemy hitEnemy = enemyObj.GetComponent<Enemy>();

        if (hitEnemy == null)
            return;

        int rawDamage = attackPoint + additionalAttackPoint;
        int finalDamage = CalculateFinalDamage(rawDamage);

        hitEnemy.DamagedByBall(finalDamage, transform.position);
        DamageSameRowEnemies(hitEnemy);

        if (WaveManager.Instance == null)
            return;

        int targetRow = WaveManager.Instance.GetGridYFromWorld(hitEnemy.transform.position);

        if (targetRow < 0)
            return;

        float rowWorldY = WaveManager.Instance.GetRowCenterWorldPosition(targetRow).y;
        EffectManager.Instance.SpawnLaserEffect(transform.position, rowWorldY);
    }

    private void DamageSameRowEnemies(Enemy hitEnemy)
    {
        if (WaveManager.Instance == null)
            return;

        int targetRow = WaveManager.Instance.GetGridYFromWorld(hitEnemy.transform.position);

        if (targetRow < 0)
            return;

        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy enemy = enemies[i];

            if (enemy == null)
                continue;

            if (!includeHitEnemyInRowDamage && enemy == hitEnemy)
                continue;

            int enemyRow = WaveManager.Instance.GetGridYFromWorld(enemy.transform.position);

            if (enemyRow != targetRow)
                continue;

            enemy.Damaged(sameRowDamage);
        }
    }
}