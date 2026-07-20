using System;
using System.Collections.Generic;

[Serializable]
public class WaveJsonDatabase
{
    // Grid Info 
    public int gridWidth = 9;
    public int initialSpawnRows = 6;

    public List<WaveJsonData> waves = new List<WaveJsonData>();

}

[Serializable]
public class WaveJsonData
{
    public int waveId;
    public string waveName;

    
    // Wave Init
    public List<EnemySpawnJsonData> initialSpawns = new List<EnemySpawnJsonData>();

    // Current wave End  <-> Next wave 
    public float nextWaveDelay = 6f;

    // Line flow
    public List<WaveUpdateLineJsonData> updateLines = new List<WaveUpdateLineJsonData>();

    // Update Line 1 <--> 2
    public float updateLineInterval = 4f;

}

[Serializable]
public class WaveUpdateLineJsonData
{
    public int line;

    public List<EnemySpawnJsonData> spawns = new List<EnemySpawnJsonData>();

}

[Serializable]
public class EnemySpawnJsonData
{
    public string enemyCode;
    public string sizeType;

    public int gridX;
    public int gridY;
}