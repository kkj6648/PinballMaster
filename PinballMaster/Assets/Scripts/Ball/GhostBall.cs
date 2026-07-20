using System.Collections.Generic;
using UnityEngine;

public class GhostBall : Ball
{
    protected override string SkillName => "Ghostball";

    [Header("Fallback Setting")]
    [SerializeField] private int defaultGhostDamage = 14;

    private int currentSkillLevel = 1;
    private readonly List<Collider2D> hitEnemyColliders = new List<Collider2D>();

    protected override void Awake()
    {
        base.Awake();

        type = Object_Type.Ball_Ghost;
        ApplyDefaultData();
    }

    protected override void RefreshSkillLevelData()
    {
        ApplySkillLevelData();
    }

    public override void Init(MyPlayer owner, Object_Type objtype, int attack)
    {
        base.Init(owner, objtype, attack);

        type = Object_Type.Ball_Ghost;
        ApplySkillLevelData();
        hitEnemyColliders.Clear();
    }

    private void ApplyDefaultData()
    {
        attackPoint = defaultGhostDamage;
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

        Debug.Log($"{SkillName} Lv.{currentSkillLevel} Applied | Damage: {attackPoint}");
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("ReturnWall"))
        {
            base.OnTriggerEnter2D(other);
            return;
        }

        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponent<Enemy>();

        if (enemy == null || !enemy.CanInteractWithBall() || hitEnemyColliders.Contains(other))
            return;

        hitEnemyColliders.Add(other);

        HitEnemy(other.gameObject);
        RestoreVelocity();
    }

    protected override void HitEnemy(GameObject enemyObj)
    {
        Enemy enemy = enemyObj.GetComponent<Enemy>();

        if (enemy == null)
            return;

        int rawDamage = attackPoint + additionalAttackPoint;
        int finalDamage = CalculateFinalDamage(rawDamage);

        enemy.DamagedByBall(finalDamage, transform.position);

        Debug.Log($"{SkillName} Lv.{currentSkillLevel} Hit | Damage: {finalDamage}");
    }

    private void RestoreVelocity()
    {
        if (moveDir.sqrMagnitude <= 0.001f)
            return;

        Rb.linearVelocity = moveDir.normalized * moveSpeed;
        Rb.angularVelocity = 0f;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        hitEnemyColliders.Clear();
    }
}