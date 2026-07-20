using UnityEngine;

public class ForestDeer : Enemy
{
    protected override void Awake()
    {
        base.Awake();
        type = Object_Type.Enemy_ForestDeer;
        maxHP = 200;
        currentHP = maxHP;
        attackPoint = 15;
        expReward = 20;
    }


    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
