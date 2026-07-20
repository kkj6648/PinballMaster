using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectSkillItem : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button selectButton;

    [Header("Basic Information")]
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private Image icon;
    [SerializeField] private Image backImage;
    [SerializeField] private TMP_Text explanationText;

    [Header("Damage Comparison")]
    [SerializeField] private TMP_Text beforeDamageText;
    [SerializeField] private TMP_Text arrowText;
    [SerializeField] private TMP_Text afterDamageText;

    [Header("Level")]
    [SerializeField] private Image level1;
    [SerializeField] private Image level2;
    [SerializeField] private Image level3;

    [Header("Level Alpha")]
    [SerializeField] private float inactiveLevelAlpha = 0.25f;

    [Header("Back Sprites")]
    [SerializeField] private Sprite[] backImages;

    private SkillData skillData;
    private int choiceIndex;
    private Action<int> onSelected;


    private void Awake()
    {
        if (selectButton == null)
        {
            selectButton = GetComponent<Button>();
        }
    }


    public void SetData(SkillData newSkillData, SkillData previousSkillData, Sprite skillIcon, int index, Action<int> selectedCallback)
    {
        if (newSkillData == null)
        {
            Hide();
            return;
        }

        skillData = newSkillData;
        choiceIndex = index;
        onSelected = selectedCallback;

        gameObject.SetActive(true);

        SetBasicInformation(skillIcon);
        SetDamageInformation(previousSkillData);
        SetLevelInformation();
        SetButton();
    }


    private void SetBasicInformation(Sprite skillIcon)
    {
        if (skillNameText != null)
        {
            skillNameText.text = $"{skillData.name}\nLv.{skillData.level}";
        }

        if (explanationText != null)
        {
            explanationText.text = skillData.explanation;
        }

        if (icon != null)
        {
            icon.sprite = skillIcon;
            icon.enabled = skillIcon != null;
            icon.SetNativeSize();

            if(skillData.type == "active")
            {
                icon.transform.localScale = new Vector3(2, 2, 2);
                backImage.sprite = backImages[0];
            }
            else if(skillData.type == "passive")
            {
                icon.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                backImage.sprite = backImages[1];
            }
            else
            {
                // fallback
                icon.transform.localScale = new Vector3(2, 2, 2);
                backImage.sprite = backImages[1];
            }

        }
    }


    private void SetDamageInformation(SkillData previousSkillData)
    {
 
        if (beforeDamageText != null)
        {
            if (previousSkillData == null)
            {
                beforeDamageText.text = "-";
            }
            else
            {

                if(previousSkillData.type =="passive")
                {
                    beforeDamageText.text = "-";
                }
                else
                {
                    beforeDamageText.text = previousSkillData.damage.ToString();
                }
                    
            }
        }

        if (afterDamageText != null)
        {

            if (skillData.damage > 0)
            {
                afterDamageText.text = skillData.damage.ToString();
            }
            else
            {
                afterDamageText.text = "-";
            }
        }

        if(beforeDamageText.text == "-" && afterDamageText.text == "-")
        {
            arrowText.text = "";
        }
        else
        {
            arrowText.text = "->";
        }




    }


    private void SetLevelInformation()
    {
        SetLevelImage(level1, skillData.level >= 1);
        SetLevelImage(level2, skillData.level >= 2);
        SetLevelImage(level3, skillData.level >= 3);
    }


    private void SetLevelImage(Image levelImage, bool active)
    {
        if (levelImage == null)
            return;

        Color color = levelImage.color;
        color.a = active ? 1f : inactiveLevelAlpha;
        levelImage.color = color;
    }


    private void SetButton()
    {
        if (selectButton == null)
            return;

        selectButton.onClick.RemoveListener(Select);
        selectButton.onClick.AddListener(Select);
        selectButton.interactable = true;
    }


    private void Select()
    {
        if (skillData == null)
            return;

        if (selectButton != null)
        {
            selectButton.interactable = false;
        }

        onSelected?.Invoke(choiceIndex);
    }


    public void Hide()
    {
        skillData = null;
        onSelected = null;

        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(Select);
        }

        gameObject.SetActive(false);
    }


    private void OnDestroy()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(Select);
        }
    }
}