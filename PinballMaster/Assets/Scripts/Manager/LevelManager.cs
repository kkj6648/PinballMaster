using System;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Data")]
    [SerializeField] private TextAsset levelExpJson;

    [Header("Current Level State")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentExp = 0;


    [SerializeField] private GameObject LevelUpPanel;

    private LevelExpDatabase database;

    private bool isWaitingForChoice;


    #region Get Set

    public int Get_CurrentLevel() { return currentLevel; }
    public int Get_CurrentExp() { return currentExp; }
    public bool Get_IsWaitingForChoice() { return isWaitingForChoice; }
    public int GetCurrentRequiredExp() { return Get_RequiredExp(currentLevel); }

    public int Get_RequiredExp(int level)
    {
        if (database == null || database.levels == null)
            return -1;

        for (int i = 0; i < database.levels.Count; i++)
        {
            if (database.levels[i].level == level)
            {
                return database.levels[i].requiredExp;
            }
        }

        return -1;
    }

    public float Get_ExpRatio()
    {
        int requiredExp = Get_RequiredExp(currentLevel);

        if (requiredExp <= 0)
            return 1f;

        return Mathf.Clamp01((float)currentExp / requiredExp);
    }

    #endregion Get Set



    public Action OnExpChanged;
    public Action OnLevelChanged;

    public Action OnLevelUpChoice;    // 3Choice UI Open


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        LoadLevelData();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void LoadLevelData()
    {
        if (levelExpJson == null)
        {
            return;
        }

        database = JsonUtility.FromJson<LevelExpDatabase>(levelExpJson.text);



        if (database == null || database.levels == null || database.levels.Count <= 0)
        {
            Debug.LogError("Level EXP JSON parse failed.");
            return;
        }

    }


    public void AddExp(int exp)
    {
        if (exp <= 0)
            return;

        currentExp += exp;


        if (GameManager.Instance != null && GameManager.Instance.Get_IsGameEnded())
            return;


        bool levelUpStarted = TryLevelUp();


        if (!levelUpStarted)
        {
            OnExpChanged?.Invoke();
        }
    }


    private bool TryLevelUp()
    {


        if (GameManager.Instance != null && GameManager.Instance.Get_IsGameEnded())

            return false;

        if (isWaitingForChoice)
            return false;

        int requiredExp = Get_RequiredExp(currentLevel);

        if (requiredExp <= 0)
            return false;

        if (currentExp < requiredExp)
            return false;


        currentExp -= requiredExp;

        currentLevel++;

        isWaitingForChoice = true;


        OnLevelChanged?.Invoke();
        OnExpChanged?.Invoke();


        LevelUpPanel.SetActive(true);
        OnLevelUpChoice?.Invoke();


        if (GameManager.Instance != null)
        {
            GameManager.Instance.Pause();
        }
        else
        {
            Debug.LogError("GameManager.Instance is null.");
        }

        return true;
    }



    public void CompleteLevelUpChoice()
    {
        if (!isWaitingForChoice)
            return;

        isWaitingForChoice = false;


        bool nextLevelUpStarted = TryLevelUp();

        if (nextLevelUpStarted)
            return;

    
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Resume();
        }
    }


    public void CancelLevelUp()
    {
        isWaitingForChoice = false;

        if (LevelUpPanel != null)
        {
            MyPlayer player = FindFirstObjectByType<MyPlayer>();

            if (player != null)
                player.BlockAimUntilPointerRelease();

            LevelUpPanel.SetActive(false);
        }
          
    }




}