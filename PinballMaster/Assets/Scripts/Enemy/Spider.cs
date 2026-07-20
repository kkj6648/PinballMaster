using UnityEngine;

public class Spider : Enemy
{
    protected override void Awake()
    {
        base.Awake();
        type = Object_Type.Enemy_Spider;
        maxHP = 200;
        currentHP = maxHP;
        attackPoint = 20;
        expReward = 20;
    } 

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
