using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHP_UI : MonoBehaviour
{

    [SerializeField] Image hpUI;
    [SerializeField] MyPlayer Owner;
    [SerializeField] TMP_Text hpCount;

    void Start()
    {
        Owner.OnHPChanged += Update_HP;
        Update_HP();
    }

    void Update()
    {
        
    }

    private void Update_HP()
    {
        hpCount.text = Owner.Get_currentHP().ToString();
        hpUI.fillAmount = (float)Owner.Get_currentHP() / Owner.Get_maxHP();
    }


    private void OnDestroy()
    {
        if (Owner == null) { return; }
        Owner.OnHPChanged -= Update_HP;
    }


}
