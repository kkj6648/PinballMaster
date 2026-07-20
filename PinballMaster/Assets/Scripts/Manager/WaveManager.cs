using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Grid Setting")]
    [SerializeField] private int gridWidth = 9;
    [SerializeField] private int gridHeight = 13;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Transform gridTopLeft;

    [Header("Wave Json")]
    [SerializeField] private TextAsset waveJsonFile;

    [Header("Wave Start")]
    [SerializeField] private float firstWaveDelay = 3f;
    [SerializeField] private int startWaveNumber = 1;


    [Header("Wave Result")]
    [SerializeField] private float victoryDelay = 2f;

    public Action<int, float> OnNextWaveNotice;
    public Action OnNextWaveNoticeEnd;
    public Action<float> OnWaveProgressChanged;

    private WaveJsonDatabase database;

    private int currentWaveIndex;
    private int activeEnemyCount;
    private int currentWaveTotalEnemyCount;
    private int currentWaveDeadEnemyCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        LoadWaveJson();

        if (database == null || database.waves == null || database.waves.Count <= 0)
            return;

        startWaveNumber = Mathf.Clamp(startWaveNumber, 1, database.waves.Count);
        currentWaveIndex = startWaveNumber - 1;

        StartCoroutine(RunWaveLoop());
    }

    private void LoadWaveJson()
    {
        if (waveJsonFile == null)
        {
            Debug.LogError("Wave JSON file is not assigned.");
            return;
        }

        database = JsonUtility.FromJson<WaveJsonDatabase>(waveJsonFile.text);

        if (database == null || database.waves == null)
        {
            Debug.LogError("Wave JSON parse failed.");
            return;
        }

        gridWidth = database.gridWidth;

        NormalizeAllUpdateLines();

        Debug.Log($"Wave JSON loaded. Wave Count: {database.waves.Count}");
    }

    #region Update Line Normalize

    private void NormalizeAllUpdateLines()
    {
        for (int i = 0; i < database.waves.Count; i++)
            NormalizeUpdateLines(database.waves[i]);
    }

    private void NormalizeUpdateLines(WaveJsonData wave)
    {
        if (wave == null || wave.updateLines == null || wave.updateLines.Count <= 0)
            return;

        wave.updateLines.Sort((a, b) => b.line.CompareTo(a.line));

        int maxLine = 0;

        for (int i = 0; i < wave.updateLines.Count; i++)
        {
            WaveUpdateLineJsonData line = wave.updateLines[i];

            if (line == null)
                continue;

            maxLine = Mathf.Max(maxLine, line.line);
        }

        if (maxLine <= 0)
            return;

        bool[,] occupied = new bool[gridWidth, maxLine];
        List<WaveUpdateLineJsonData> normalizedLines = new List<WaveUpdateLineJsonData>();

        for (int i = 0; i < wave.updateLines.Count; i++)
        {
            WaveUpdateLineJsonData sourceLine = wave.updateLines[i];

            if (sourceLine == null || sourceLine.line <= 0)
                continue;

            WaveUpdateLineJsonData normalizedLine = new WaveUpdateLineJsonData
            {
                line = sourceLine.line,
                spawns = new List<EnemySpawnJsonData>()
            };

            if (sourceLine.spawns != null)
            {
                sourceLine.spawns.Sort((a, b) => a.gridX.CompareTo(b.gridX));

                for (int j = 0; j < sourceLine.spawns.Count; j++)
                {
                    EnemySpawnJsonData spawn = sourceLine.spawns[j];

                    if (spawn == null)
                        continue;

                    Vector2Int size = GetSizeFromString(spawn.sizeType);
                    int anchorY = sourceLine.line - 1;

                    if (!CanPlaceOnSchedule(spawn.gridX, anchorY, size.x, size.y, occupied))
                    {
                        Debug.LogWarning(
                            $"{wave.waveName} Line {sourceLine.line}: " +
                            $"{spawn.enemyCode} cannot be placed. " +
                            $"X:{spawn.gridX}, Size:{size.x}x{size.y}"
                        );

                        continue;
                    }

                    normalizedLine.spawns.Add(new EnemySpawnJsonData
                    {
                        enemyCode = spawn.enemyCode,
                        sizeType = spawn.sizeType,
                        gridX = spawn.gridX,
                        gridY = 0
                    });

                    MarkScheduleOccupied(spawn.gridX, anchorY, size.x, size.y, occupied);
                }
            }

            normalizedLines.Add(normalizedLine);
        }

        normalizedLines.Sort((a, b) => a.line.CompareTo(b.line));
        wave.updateLines = normalizedLines;
    }

    private bool CanPlaceOnSchedule(int x, int anchorY, int width, int height, bool[,] occupied)
    {
        int scheduleWidth = occupied.GetLength(0);
        int scheduleHeight = occupied.GetLength(1);
        int frontY = anchorY - height + 1;

        if (x < 0 || frontY < 0)
            return false;

        if (x + width > scheduleWidth)
            return false;

        if (anchorY >= scheduleHeight)
            return false;

        for (int ix = x; ix < x + width; ix++)
        {
            for (int iy = frontY; iy <= anchorY; iy++)
            {
                if (occupied[ix, iy])
                    return false;
            }
        }

        return true;
    }

    private void MarkScheduleOccupied(int x, int anchorY, int width, int height, bool[,] occupied)
    {
        int frontY = anchorY - height + 1;

        for (int ix = x; ix < x + width; ix++)
        {
            for (int iy = frontY; iy <= anchorY; iy++)
                occupied[ix, iy] = true;
        }
    }

    #endregion

    #region Wave Flow

    private IEnumerator RunWaveLoop()
    {
        yield return null;

        if (database == null || database.waves == null || database.waves.Count <= 0)
            yield break;

        if (firstWaveDelay > 0f)
        {
            OnNextWaveNotice?.Invoke(currentWaveIndex + 1, firstWaveDelay);

            yield return new WaitForSeconds(firstWaveDelay);

            OnNextWaveNoticeEnd?.Invoke();
        }

        while (currentWaveIndex < database.waves.Count)
        {
            WaveJsonData wave = database.waves[currentWaveIndex];

            yield return StartCoroutine(RunWave(wave));

            currentWaveIndex++;
        }

        Debug.Log("All Waves Clear");

        if (GameManager.Instance != null)
            GameManager.Instance.Victory();
    }

    private IEnumerator RunWave(WaveJsonData wave)
    {
        Debug.Log($"Start {wave.waveName}");

        activeEnemyCount = 0;
        currentWaveTotalEnemyCount = GetTotalEnemyCount(wave);
        currentWaveDeadEnemyCount = 0;

        OnWaveProgressChanged?.Invoke(0f);

        SpawnInitialFormation(wave);

        if (wave.updateLines != null)
        {
            for (int i = 0; i < wave.updateLines.Count; i++)
            {
                WaveUpdateLineJsonData updateLine = wave.updateLines[i];

                yield return new WaitForSeconds(wave.updateLineInterval);

                SpawnUpdateLine(wave, updateLine);
            }
        }

        yield return new WaitUntil(() => activeEnemyCount <= 0);

        Debug.Log($"{wave.waveName} Clear");

        int nextWaveNumber = currentWaveIndex + 2;
        bool hasNextWave = nextWaveNumber <= database.waves.Count;

        if (!hasNextWave)
        {
            yield return new WaitForSeconds(victoryDelay);
            yield break;
        }

        OnNextWaveNotice?.Invoke(nextWaveNumber, wave.nextWaveDelay);

        yield return new WaitForSeconds(wave.nextWaveDelay);

        OnNextWaveNoticeEnd?.Invoke();
    }

    #endregion

    #region Spawn

    private void SpawnInitialFormation(WaveJsonData wave)
    {
        if (wave.initialSpawns == null)
            return;

        for (int i = 0; i < wave.initialSpawns.Count; i++)
        {
            EnemySpawnJsonData spawn = wave.initialSpawns[i];

            if (spawn == null)
                continue;

            Vector2Int size = GetSizeFromString(spawn.sizeType);
            Vector3 position = GridToWorld(spawn.gridX, spawn.gridY, size.x, size.y);
            Object_Type enemyType = CodeToObjectType(spawn.enemyCode);

            SpawnEnemy(enemyType, position);
        }
    }

    private void SpawnUpdateLine(WaveJsonData wave, WaveUpdateLineJsonData updateLine)
    {
        if (updateLine == null || updateLine.spawns == null)
            return;

        Debug.Log($"{wave.waveName} Update Line {updateLine.line}");

        for (int i = 0; i < updateLine.spawns.Count; i++)
        {
            EnemySpawnJsonData spawn = updateLine.spawns[i];

            if (spawn == null)
                continue;

            Vector2Int size = GetSizeFromString(spawn.sizeType);
            Vector3 worldPosition = GridToWorld(spawn.gridX, 0, size.x, size.y);
            Object_Type enemyType = CodeToObjectType(spawn.enemyCode);

            SpawnEnemy(enemyType, worldPosition);
        }
    }

    private bool SpawnEnemy(Object_Type enemyType, Vector3 position)
    {
        if (PoolManager.Instance == null)
        {
            Debug.LogError("PoolManager.Instance is null.");
            return false;
        }

        GameObject enemyObject = PoolManager.Instance.Get_Object(enemyType);

        if (enemyObject == null)
        {
            Debug.LogWarning($"Pool missing: {enemyType}");
            return false;
        }

        enemyObject.transform.position = position;
        enemyObject.transform.rotation = Quaternion.identity;
        enemyObject.SetActive(true);

        Enemy enemy = enemyObject.GetComponent<Enemy>();

        if (enemy != null)
            enemy.SetWaveOwner(this);

        activeEnemyCount++;

        return true;
    }

    #endregion

    #region Grid

    private Vector3 GridToWorld(int x, int y, int width, int height)
    {
        Vector3 origin = gridTopLeft.position;

        float worldX = origin.x + (x + width * 0.5f) * cellSize;
        float worldY = origin.y - (y + height * 0.5f) * cellSize;

        return new Vector3(worldX, worldY, 0f);
    }

    public int GetGridYFromWorld(Vector3 worldPos)
    {
        if (gridTopLeft == null)
            return -1;

        float localY = gridTopLeft.position.y - worldPos.y;
        int y = Mathf.FloorToInt(localY / cellSize);

        if (y < 0 || y >= gridHeight)
            return -1;

        return y;
    }

    public Vector3 GetRowCenterWorldPosition(int gridY)
    {
        if (gridTopLeft == null)
            return Vector3.zero;

        float worldX = gridTopLeft.position.x + gridWidth * cellSize * 0.5f;
        float worldY = gridTopLeft.position.y - (gridY + 0.5f) * cellSize;

        return new Vector3(worldX, worldY, 0f);
    }

    public float Get_CellSize()
    {
        return cellSize;
    }

    #endregion

    #region Enemy Data

    private Vector2Int GetSizeFromString(string sizeType)
    {
        switch (sizeType)
        {
            case "Size_2x1":
                return new Vector2Int(2, 1);

            case "Size_1x2":
                return new Vector2Int(1, 2);

            case "Size_2x2":
                return new Vector2Int(2, 2);

            case "Size_1x1":
            default:
                return new Vector2Int(1, 1);
        }
    }

    private Object_Type CodeToObjectType(string code)
    {
        switch (code)
        {
            case "b":
                return Object_Type.Enemy_Fluffy;

            case "p":
                return Object_Type.Enemy_Spider;

            case "o":
                return Object_Type.Enemy_StoneBug;

            case "g":
                return Object_Type.Enemy_ForestDeer;

            case "r":
                return Object_Type.Enemy_Boss;

            default:
                Debug.LogError($"Unknown enemy code: {code}");
                return Object_Type.Enemy_Fluffy;
        }
    }

    #endregion

    #region Enemy Count

    public int GetTotalEnemyCount(WaveJsonData wave)
    {
        if (wave == null)
            return 0;

        int totalCount = 0;

        if (wave.initialSpawns != null)
            totalCount += wave.initialSpawns.Count;

        if (wave.updateLines != null)
        {
            for (int i = 0; i < wave.updateLines.Count; i++)
            {
                WaveUpdateLineJsonData line = wave.updateLines[i];

                if (line?.spawns == null)
                    continue;

                totalCount += line.spawns.Count;
            }
        }

        return totalCount;
    }

    public void OnEnemyDead(Enemy enemy)
    {
        activeEnemyCount--;

        if (activeEnemyCount < 0)
            activeEnemyCount = 0;

        currentWaveDeadEnemyCount++;

        if (currentWaveDeadEnemyCount > currentWaveTotalEnemyCount)
            currentWaveDeadEnemyCount = currentWaveTotalEnemyCount;

        float progressRatio = 0f;

        if (currentWaveTotalEnemyCount > 0)
            progressRatio = (float)currentWaveDeadEnemyCount / currentWaveTotalEnemyCount;

        Debug.Log(
            $"Wave Progress: {currentWaveDeadEnemyCount} / " +
            $"{currentWaveTotalEnemyCount} = {progressRatio * 100f:F1}%"
        );

        OnWaveProgressChanged?.Invoke(progressRatio);
    }

    #endregion

#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        if (gridTopLeft == null)
            return;

        Gizmos.color = Color.green;

        Vector3 origin = gridTopLeft.position;

        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = origin + new Vector3(x * cellSize, 0f, 0f);
            Vector3 end = origin + new Vector3(x * cellSize, -gridHeight * cellSize, 0f);

            Gizmos.DrawLine(start, end);
        }

        for (int y = 0; y <= gridHeight; y++)
        {
            Vector3 start = origin + new Vector3(0f, -y * cellSize, 0f);
            Vector3 end = origin + new Vector3(gridWidth * cellSize, -y * cellSize, 0f);

            Gizmos.DrawLine(start, end);
        }
    }

#endif

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}