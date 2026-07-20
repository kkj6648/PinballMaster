using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class SkillIconData
{
    public string skillName;
    public Sprite icon;
}

[Serializable]
public class OwnedSkillData
{
    public string name;
    public string type;
    public int level;

    public OwnedSkillData(SkillData skillData)
    {
        name = skillData.name;
        type = skillData.type;
        level = skillData.level;
    }
}

public class ChoiceManager : MonoBehaviour
{
    public static ChoiceManager Instance { get; private set; }

    [Header("Choice Setting")]
    [SerializeField] private int choiceCount = 3;
    [SerializeField] private string normalBallSkillName = "AddNormalball";

    [Header("Skill Slot Setting")]
    [SerializeField] private int maxActiveSkillCount = 4;
    [SerializeField] private int maxPassiveSkillCount = 2;

    [Header("Choice UI")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private SelectSkillItem[] choiceItems;
    [SerializeField] private Button rerollButton;

    [Header("Skill Icons")]
    [SerializeField] private List<SkillIconData> skillIcons = new List<SkillIconData>();

    private readonly Dictionary<string, Sprite> iconLookup = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, OwnedSkillData> ownedSkillLookup = new Dictionary<string, OwnedSkillData>(StringComparer.OrdinalIgnoreCase);
    private readonly List<SkillData> currentChoices = new List<SkillData>();

    private bool hasRerolledThisChoice;

    public Action<SkillData> OnSkillSelected;

    #region Get Set

    public IReadOnlyList<SkillData> Get_CurrentChoices()
    {
        return currentChoices;
    }

    public int Get_OwnedSkillLevel(string skillName)
    {
        if (string.IsNullOrWhiteSpace(skillName))
            return 0;

        if (ownedSkillLookup.TryGetValue(skillName, out OwnedSkillData ownedSkill))
            return ownedSkill.level;

        return 0;
    }

    public bool HasSkill(string skillName)
    {
        if (string.IsNullOrWhiteSpace(skillName))
            return false;

        return ownedSkillLookup.ContainsKey(skillName);
    }

    public int Get_ActiveSkillCount()
    {
        return GetOwnedSkillCount("active");
    }

    public int Get_PassiveSkillCount()
    {
        return GetOwnedSkillCount("passive");
    }

    public List<OwnedSkillData> Get_OwnedSkills()
    {
        return new List<OwnedSkillData>(ownedSkillLookup.Values);
    }

    #endregion Get Set

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        BuildIconLookup();

        if (choicePanel != null)
            choicePanel.SetActive(false);

        if (rerollButton != null)
        {
            rerollButton.onClick.AddListener(RerollChoices);
            rerollButton.interactable = false;
        }
    }

    private void Start()
    {
        if (LevelManager.Instance == null)
        {
            Debug.LogError("LevelManager.Instance is null.");
            return;
        }

        LevelManager.Instance.OnLevelUpChoice += OpenLevelUpChoices;
    }

    private void OnDestroy()
    {
        if (rerollButton != null)
            rerollButton.onClick.RemoveListener(RerollChoices);

        if (LevelManager.Instance != null)
            LevelManager.Instance.OnLevelUpChoice -= OpenLevelUpChoices;

        if (Instance == this)
            Instance = null;
    }

    private void BuildIconLookup()
    {
        iconLookup.Clear();

        for (int i = 0; i < skillIcons.Count; i++)
        {
            SkillIconData iconData = skillIcons[i];

            if (iconData == null || string.IsNullOrWhiteSpace(iconData.skillName) || iconData.icon == null)
                continue;

            string normalizedName = iconData.skillName.Trim();
            iconLookup[normalizedName] = iconData.icon;
        }
    }

    private void OpenLevelUpChoices()
    {
        hasRerolledThisChoice = false;

        if (rerollButton != null)
            rerollButton.interactable = true;

        GenerateChoices();
    }

    public void GenerateChoices()
    {
        currentChoices.Clear();

        List<SkillData> candidatePool = BuildCandidatePool();
        Shuffle(candidatePool);

        int resultCount = Mathf.Min(choiceCount, candidatePool.Count);

        for (int i = 0; i < resultCount; i++)
        {
            currentChoices.Add(candidatePool[i]);
        }

        FillNormalBallChoices();

        if (currentChoices.Count <= 0)
        {
            if (rerollButton != null)
                rerollButton.interactable = false;

            if (LevelManager.Instance != null)
                LevelManager.Instance.CompleteLevelUpChoice();

            return;
        }

        ShowChoices();
    }

    private void FillNormalBallChoices()
    {
        if (currentChoices.Count >= choiceCount)
            return;

        if (SkillManager.Instance == null)
        {
            Debug.LogError("SkillManager.Instance is null.");
            return;
        }

        if (!SkillManager.Instance.TryGetSkillData(normalBallSkillName, 1, out SkillData normalBallData))
        {
            Debug.LogError($"Normal ball skill data not found: {normalBallSkillName}");
            return;
        }

        while (currentChoices.Count < choiceCount)
        {
            currentChoices.Add(normalBallData);
        }
    }

    private List<SkillData> BuildCandidatePool()
    {
        List<SkillData> candidatePool = new List<SkillData>();

        if (SkillManager.Instance == null)
        {
            Debug.LogError("SkillManager.Instance is null.");
            return candidatePool;
        }

        IReadOnlyList<SkillData> allSkillData = SkillManager.Instance.Get_AllSkills();

        int activeSkillCount = GetOwnedSkillCount("active");
        int passiveSkillCount = GetOwnedSkillCount("passive");

        HashSet<string> checkedSkillNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < allSkillData.Count; i++)
        {
            SkillData skillData = allSkillData[i];

            if (skillData == null || string.IsNullOrWhiteSpace(skillData.name) || IsNormalBallChoice(skillData))
                continue;

            if (!checkedSkillNames.Add(skillData.name))
                continue;

            if (ownedSkillLookup.TryGetValue(skillData.name, out OwnedSkillData ownedSkill))
            {
                int nextLevel = ownedSkill.level + 1;

                if (SkillManager.Instance.TryGetSkillData(ownedSkill.name, nextLevel, out SkillData nextLevelData))
                    candidatePool.Add(nextLevelData);

                continue;
            }

            if (!SkillManager.Instance.TryGetSkillData(skillData.name, 1, out SkillData levelOneData))
                continue;

            string skillType = NormalizeSkillType(levelOneData.type);

            if (skillType == "active")
            {
                if (activeSkillCount < maxActiveSkillCount)
                    candidatePool.Add(levelOneData);

                continue;
            }

            if (skillType == "passive" && passiveSkillCount < maxPassiveSkillCount)
                candidatePool.Add(levelOneData);
        }

        return candidatePool;
    }

    private void ShowChoices()
    {
        if (choicePanel != null)
            choicePanel.SetActive(true);

        if (choiceItems == null)
            return;

        for (int i = 0; i < choiceItems.Length; i++)
        {
            SelectSkillItem item = choiceItems[i];

            if (item == null)
                continue;

            if (i >= currentChoices.Count)
            {
                item.Hide();
                continue;
            }

            SkillData skillData = currentChoices[i];
            SkillData previousSkillData = GetPreviousSkillData(skillData);
            Sprite skillIcon = GetSkillIcon(skillData.name);

            item.SetData(skillData, previousSkillData, skillIcon, i, SelectChoice);
        }
    }

    public void RerollChoices()
    {
        if (choicePanel == null || !choicePanel.activeSelf || hasRerolledThisChoice)
            return;

        hasRerolledThisChoice = true;

        if (rerollButton != null)
            rerollButton.interactable = false;

        GenerateChoices();
    }

    private SkillData GetPreviousSkillData(SkillData currentSkillData)
    {
        if (currentSkillData == null || IsNormalBallChoice(currentSkillData) || currentSkillData.level <= 1 || SkillManager.Instance == null)
            return null;

        SkillManager.Instance.TryGetSkillData(currentSkillData.name, currentSkillData.level - 1, out SkillData previousSkillData);

        return previousSkillData;
    }

    public Sprite GetSkillIcon(string skillName)
    {
        if (string.IsNullOrWhiteSpace(skillName))
            return null;

        string normalizedName = skillName.Trim();

        if (iconLookup.TryGetValue(normalizedName, out Sprite skillIcon))
            return skillIcon;

        return null;
    }

    public void SelectChoice(int choiceIndex)
    {
        if (choiceIndex < 0 || choiceIndex >= currentChoices.Count)
            return;

        SkillData selectedSkill = currentChoices[choiceIndex];

        if (IsNormalBallChoice(selectedSkill))
        {
            OnSkillSelected?.Invoke(selectedSkill);
            CompleteChoice();
            return;
        }

        if (!CanAcquireSkill(selectedSkill))
            return;

        if (ownedSkillLookup.TryGetValue(selectedSkill.name, out OwnedSkillData ownedSkill))
            ownedSkill.level = selectedSkill.level;
        else
            ownedSkillLookup.Add(selectedSkill.name, new OwnedSkillData(selectedSkill));

        OnSkillSelected?.Invoke(selectedSkill);
        CompleteChoice();
    }

    private void CompleteChoice()
    {
        currentChoices.Clear();
        HideChoicePanel();

        if (LevelManager.Instance != null)
            LevelManager.Instance.CompleteLevelUpChoice();
    }

    private bool IsNormalBallChoice(SkillData skillData)
    {
        if (skillData == null || string.IsNullOrWhiteSpace(skillData.name))
            return false;

        return string.Equals(skillData.name.Trim(), normalBallSkillName.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private bool CanAcquireSkill(SkillData skillData)
    {
        if (skillData == null || string.IsNullOrWhiteSpace(skillData.name))
            return false;

        if (IsNormalBallChoice(skillData))
            return true;

        if (ownedSkillLookup.TryGetValue(skillData.name, out OwnedSkillData ownedSkill))
            return skillData.level == ownedSkill.level + 1;

        if (skillData.level != 1)
            return false;

        string skillType = NormalizeSkillType(skillData.type);

        if (skillType == "active")
            return GetOwnedSkillCount("active") < maxActiveSkillCount;

        if (skillType == "passive")
            return GetOwnedSkillCount("passive") < maxPassiveSkillCount;

        return false;
    }

    private void HideChoicePanel()
    {
        if (choiceItems != null)
        {
            for (int i = 0; i < choiceItems.Length; i++)
            {
                if (choiceItems[i] != null)
                    choiceItems[i].Hide();
            }
        }

        if (rerollButton != null)
            rerollButton.interactable = false;

        if (choicePanel == null)
            return;

        MyPlayer player = FindFirstObjectByType<MyPlayer>();

        if (player != null)
            player.BlockAimUntilPointerRelease();

        choicePanel.SetActive(false);
    }

    private int GetOwnedSkillCount(string skillType)
    {
        int count = 0;

        foreach (OwnedSkillData ownedSkill in ownedSkillLookup.Values)
        {
            if (ownedSkill == null)
                continue;

            if (string.Equals(ownedSkill.type, skillType, StringComparison.OrdinalIgnoreCase))
                count++;
        }

        return count;
    }

    private string NormalizeSkillType(string skillType)
    {
        if (string.IsNullOrWhiteSpace(skillType))
            return "";

        return skillType.Trim().ToLowerInvariant();
    }

    private void Shuffle(List<SkillData> list)
    {
        if (list == null)
            return;

        for (int i = 0; i < list.Count - 1; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, list.Count);
            SkillData temp = list[i];

            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}