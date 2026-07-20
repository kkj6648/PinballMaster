using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    [Header("Skill Data")]
    [SerializeField] private TextAsset skillDataJson;

    private SkillDatabase database;

    private readonly Dictionary<string, Dictionary<int, SkillData>>
        skillLookup = new Dictionary<string, Dictionary<int, SkillData>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        LoadSkillData();
    }

    private void LoadSkillData()
    {
        if (skillDataJson == null)
        {
            return;
        }

        database = JsonUtility.FromJson<SkillDatabase>(skillDataJson.text);

        if (database == null || database.skills == null)
        {
            return;
        }

        skillLookup.Clear();

        for (int i = 0; i < database.skills.Count; i++)
        {
            SkillData data = database.skills[i];

            if (data == null || string.IsNullOrWhiteSpace(data.name))
                continue;

            if (!skillLookup.TryGetValue(data.name, out Dictionary<int, SkillData> levelLookup))
            {
                levelLookup = new Dictionary<int, SkillData>();
                skillLookup.Add(data.name, levelLookup);
            }

            if (levelLookup.ContainsKey(data.level))
            {
                levelLookup[data.level] = data;
                continue;
            }

            levelLookup.Add(data.level, data);
        }
    }

    public bool TryGetSkillData(string skillName, int level, out SkillData data)
    {
        data = null;

        if (!skillLookup.TryGetValue(skillName, out Dictionary<int, SkillData> levelLookup))
        {
            return false;
        }

        return levelLookup.TryGetValue(level, out data);
    }

    public SkillData GetSkillData(string skillName, int level)
    {
        if (TryGetSkillData(skillName, level, out SkillData data))
        {
            return data;
        }

  
        return null;
    }


    public IReadOnlyList<SkillData> Get_AllSkills()
    {
        if (database == null || database.skills == null)
            return System.Array.Empty<SkillData>();

        return database.skills;
    }

}