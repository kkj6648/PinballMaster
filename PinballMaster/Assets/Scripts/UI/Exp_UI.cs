using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Exp_UI : MonoBehaviour
{
    [Header("UI Component")]
    [SerializeField] private Image back;
    [SerializeField] private Image expBar;
    [SerializeField] private TMP_Text levelText;

    [Header("Animation")]
    [SerializeField] private float fillSpeed = 1.5f;

    private Coroutine expCoroutine;
    private bool isSubscribed;


    private void OnEnable()
    {
        TrySubscribe();
        RefreshUI();
    }

    private void Start()
    {

        TrySubscribe();
        RefreshUI();
    }

    private void OnDisable()
    {
        Unsubscribe();

        if (expCoroutine != null)
        {
            StopCoroutine(expCoroutine);
            expCoroutine = null;
        }
    }


    private void TrySubscribe()
    {
        if (isSubscribed)
            return;

        if (LevelManager.Instance == null)
            return;

        LevelManager.Instance.OnExpChanged += Update_ExpRatio;
        LevelManager.Instance.OnLevelChanged += Update_Level;

        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed)
            return;

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnExpChanged -= Update_ExpRatio;
            LevelManager.Instance.OnLevelChanged -= Update_Level;
        }

        isSubscribed = false;
    }


    private void RefreshUI()
    {
        if (LevelManager.Instance == null)
            return;

        if (expBar != null)
            expBar.fillAmount = LevelManager.Instance.Get_ExpRatio();

        Update_Level();
    }


    private void Update_ExpRatio()
    {
        if (LevelManager.Instance == null || expBar == null)
            return;

        if (!isActiveAndEnabled)
            return;

        float targetRatio = LevelManager.Instance.Get_ExpRatio();

        if (expCoroutine != null)
            StopCoroutine(expCoroutine);

        expCoroutine = StartCoroutine(Animate_Exp(targetRatio));
    }


    private IEnumerator Animate_Exp(float targetRatio)
    {
        if (targetRatio < expBar.fillAmount)
        {
            while (expBar.fillAmount < 1f)
            {
                expBar.fillAmount = Mathf.MoveTowards(
                    expBar.fillAmount,
                    1f,
                    fillSpeed * Time.unscaledDeltaTime
                );

                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.1f);

            expBar.fillAmount = 0f;
        }

        while (!Mathf.Approximately(expBar.fillAmount, targetRatio))
        {
            expBar.fillAmount = Mathf.MoveTowards(
                expBar.fillAmount,
                targetRatio,
                fillSpeed * Time.unscaledDeltaTime
            );

            yield return null;
        }

        expBar.fillAmount = targetRatio;
        expCoroutine = null;
    }


    private void Update_Level()
    {
        if (LevelManager.Instance == null || levelText == null)
            return;

        levelText.text =
            LevelManager.Instance.Get_CurrentLevel().ToString();
    }
}