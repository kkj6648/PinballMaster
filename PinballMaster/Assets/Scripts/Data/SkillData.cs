using System;
using System.Collections.Generic;

[Serializable]
public class SkillDatabase
{
    public List<SkillData> skills;
}

[Serializable]
public class SkillData
{
    public string name;
    public int level;
    public string type;

    public int damage;
    public float duration;
    public int maxStack;
    public int dotDamage;

    public float rate;
    public float speed;

    public int additionalDamage;
    public int rowDamage;
    public int boomDamage;
    public int fragmentDamage;

    public float criticalRate;
    public string explanation;
}