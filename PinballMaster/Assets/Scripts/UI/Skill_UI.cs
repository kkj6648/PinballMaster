using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Skill_UI : MonoBehaviour
{
    [Header("Active Slots")]
    [SerializeField] private Image activeSlot1Img;
    [SerializeField] private TMP_Text activeSlot1Text;
    [SerializeField] private Image activeSlot2Img;
    [SerializeField] private TMP_Text activeSlot2Text;
    [SerializeField] private Image activeSlot3Img;
    [SerializeField] private TMP_Text activeSlot3Text;
    [SerializeField] private Image activeSlot4Img;
    [SerializeField] private TMP_Text activeSlot4Text;

    [Header("Passive Slots")]
    [SerializeField] private Image passiveSlot1Img;
    [SerializeField] private TMP_Text passiveSlot1Text;
    [SerializeField] private Image passiveSlot2Img;
    [SerializeField] private TMP_Text passiveSlot2Text;

    private Image[] activeImages;
    private TMP_Text[] activeTexts;
    private Image[] passiveImages;
    private TMP_Text[] passiveTexts;

    private void Awake()
    {
        activeImages = new Image[] { activeSlot1Img, activeSlot2Img, activeSlot3Img, activeSlot4Img };
        activeTexts = new TMP_Text[] { activeSlot1Text, activeSlot2Text, activeSlot3Text, activeSlot4Text };

        passiveImages = new Image[] { passiveSlot1Img, passiveSlot2Img };
        passiveTexts = new TMP_Text[] { passiveSlot1Text, passiveSlot2Text };
    }

    private void Start()
    {
        if (ChoiceManager.Instance == null)
        {
            Debug.LogError("ChoiceManager.Instance is null.");
            return;
        }

        ChoiceManager.Instance.OnSkillSelected += OnSkillSelected;
        RefreshSkillUI();
    }

    private void OnDestroy()
    {
        if (ChoiceManager.Instance != null)
            ChoiceManager.Instance.OnSkillSelected -= OnSkillSelected;
    }

    private void OnSkillSelected(SkillData selectedSkill)
    {
        RefreshSkillUI();
    }

    private void RefreshSkillUI()
    {
        ClearSlots();

        if (ChoiceManager.Instance == null)
            return;

        List<OwnedSkillData> ownedSkills = ChoiceManager.Instance.Get_OwnedSkills();

        int activeIndex = 0;
        int passiveIndex = 0;

        for (int i = 0; i < ownedSkills.Count; i++)
        {
            OwnedSkillData ownedSkill = ownedSkills[i];

            if (ownedSkill == null)
                continue;

            string skillType = ownedSkill.type?.Trim().ToLowerInvariant();
            Sprite skillIcon = ChoiceManager.Instance.GetSkillIcon(ownedSkill.name);

            if (skillType == "active")
            {
                if (activeIndex >= activeImages.Length)
                    continue;

                SetSlot(activeImages[activeIndex], activeTexts[activeIndex], skillIcon, ownedSkill.level);
                activeIndex++;
            }
            else if (skillType == "passive")
            {
                if (passiveIndex >= passiveImages.Length)
                    continue;

                SetSlot(passiveImages[passiveIndex], passiveTexts[passiveIndex], skillIcon, ownedSkill.level);
                passiveIndex++;
            }
        }
    }

    private void SetSlot(Image slotImage, TMP_Text slotText, Sprite skillIcon, int skillLevel)
    {
        if (slotImage != null)
        {
            slotImage.sprite = skillIcon;
            slotImage.enabled = skillIcon != null;
            slotImage.preserveAspect = true;
        }

        if (slotText != null)
        {
            slotText.text = skillLevel >= 3 ? "Max" : $"Lv.{skillLevel}";
            slotText.gameObject.SetActive(true);
        }
    }

    private void ClearSlots()
    {
        ClearSlotGroup(activeImages, activeTexts);
        ClearSlotGroup(passiveImages, passiveTexts);
    }

    private void ClearSlotGroup(Image[] images, TMP_Text[] texts)
    {
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null)
            {
                images[i].sprite = null;
                images[i].enabled = false;
            }

            if (texts[i] != null)
            {
                texts[i].text = string.Empty;
                texts[i].gameObject.SetActive(false);
            }
        }
    }
}