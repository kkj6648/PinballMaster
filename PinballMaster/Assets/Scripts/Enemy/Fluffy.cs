using System.Transactions;
using UnityEngine;

public class Fluffy : Enemy
{

    protected override void Awake()
    {
       base.Awake();
        type = Object_Type.Enemy_Fluffy;
        maxHP = 100;
        currentHP = maxHP;
        attackPoint = 10;
        expReward = 10;

    }

    void Start()
    { 
    }

    void Update()
    {
        
    }

}
