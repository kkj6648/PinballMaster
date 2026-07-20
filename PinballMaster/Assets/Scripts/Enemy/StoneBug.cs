using UnityEngine;

public class StoneBug : Enemy
{
    protected override void Awake()
    {
        base.Awake();
        type = Object_Type.Enemy_StoneBug;
        maxHP = 250;
        currentHP = maxHP;
        attackPoint = 15;
        expReward = 30;
    }


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
