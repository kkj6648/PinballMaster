using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHP_UI : MonoBehaviour
{
    [Header("UI Component")]
    [SerializeField] private Image hpBar;
    [SerializeField] private Image hpLerpBar;
    [SerializeField] private Image hpBack;
    [SerializeField] private Enemy Owner;

    [Header("Animation")]
    [SerializeField] private float lerpSpeed = 0.8f;
    [SerializeField] private float delay = 0.2f;

    private Coroutine hpCoroutine;

    private void OnEnable()
    {
        if (Owner != null)
            Owner.OnHPChanged += Update_HP;

        SetHPUIActive(false);
        Update_HP();
    }

    private void OnDisable()
    {
        if (Owner != null)
            Owner.OnHPChanged -= Update_HP;

        if (hpCoroutine != null)
        {
            StopCoroutine(hpCoroutine);
            hpCoroutine = null;
        }
    }

    private void Update_HP()
    {
        if (Owner == null)
            return;

        if (Owner.Get_ShowHPUI())
            SetHPUIActive(true);

        float fillValue = (float)Owner.Get_currentHP() / Owner.Get_maxHP();
        hpBar.fillAmount = fillValue;

        if (!gameObject.activeInHierarchy)
            return;

        if (hpCoroutine != null)
            StopCoroutine(hpCoroutine);

        hpCoroutine = StartCoroutine(Update_LerpHP(fillValue));
    }

    private IEnumerator Update_LerpHP(float targetFill)
    {
        yield return new WaitForSeconds(delay);

        while (hpLerpBar.fillAmount > targetFill)
        {
            hpLerpBar.fillAmount = Mathf.MoveTowards(hpLerpBar.fillAmount, targetFill, lerpSpeed * Time.deltaTime);
            yield return null;
        }

        hpLerpBar.fillAmount = targetFill;
        hpCoroutine = null;
    }

    private void SetHPUIActive(bool active)
    {
        if (hpBar != null)
            hpBar.enabled = active;

        if (hpBack != null)
            hpBack.enabled = active;

        if (hpLerpBar != null)
            hpLerpBar.enabled = active;
    }
}