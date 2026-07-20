using System;
using System.Collections.Generic;

[Serializable]
public class LevelExpDatabase
{
    public List<LevelExpData> levels = new List<LevelExpData>();
}

[Serializable]
public class LevelExpData
{
    public int level;
    public int requiredExp;
}
