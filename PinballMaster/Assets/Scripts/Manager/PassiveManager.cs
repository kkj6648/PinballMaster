using UnityEngine;

public class PassiveManager : MonoBehaviour
{
    public static PassiveManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    public SkillData GetOwnedPassiveData(string skillName)
    {
        if (ChoiceManager.Instance == null)
            return null;

        int level =
            ChoiceManager.Instance.Get_OwnedSkillLevel(skillName);

        if (level <= 0)
            return null;

        if (SkillManager.Instance == null)
            return null;

        return SkillManager.Instance.GetSkillData(skillName, level);


    }


    public float GetNormalBallDamageMultiplier()
    {
        SkillData data = GetOwnedPassiveData("Hotstealheart");


        if (data == null)
            return 1f;

        return 1f + data.additionalDamage * 0.01f;
    }


    public float GetMagicMirrorDamageRate()
    {
        SkillData data = GetOwnedPassiveData("MagicMirror");

        if (data == null)
            return 0f;

        return data.additionalDamage * 0.01f;
    }


    public float GetFrontCriticalRate()
    {
        SkillData data = GetOwnedPassiveData("AmethystSword");


        if (data == null)
            return 0f;

        return data.criticalRate * 0.01f;
    }


    public float GetBackCriticalRate()
    {
        SkillData data = GetOwnedPassiveData("EmeraldSword");


        if (data == null)
            return 0f;

        return data.criticalRate * 0.01f;
    }


    public int GetDeathExplosionDamage()
    {
        SkillData data = GetOwnedPassiveData("FinallMatchstick");

        if (data == null)
            return 0;

        return data.boomDamage;
    }
}