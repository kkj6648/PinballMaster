using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Progress_UI : MonoBehaviour
{
    [Header("UI Component")]
    [SerializeField] private Image progressImg;
    [SerializeField] private TMP_Text percentText;

    private Coroutine subscribeCoroutine;
    private bool isSubscribed;

    private void OnEnable()
    {
        UpdateProgress(0f);
        subscribeCoroutine = StartCoroutine(SubscribeWhenReady());
    }

    private IEnumerator SubscribeWhenReady()
    {
        while (WaveManager.Instance == null)
            yield return null;

        if (!isSubscribed)
        {
            WaveManager.Instance.OnWaveProgressChanged += UpdateProgress;
            isSubscribed = true;
        }

        subscribeCoroutine = null;
    }

    private void OnDisable()
    {
        if (subscribeCoroutine != null)
        {
            StopCoroutine(subscribeCoroutine);
            subscribeCoroutine = null;
        }

        if (isSubscribed && WaveManager.Instance != null)
            WaveManager.Instance.OnWaveProgressChanged -= UpdateProgress;

        isSubscribed = false;
    }

    private void UpdateProgress(float progressRatio)
    {
        progressRatio = Mathf.Clamp01(progressRatio);

        if (progressImg != null)
            progressImg.fillAmount = progressRatio;

        if (percentText != null)
        {
            int percent = Mathf.RoundToInt(progressRatio * 100f);
            percentText.text = $"{percent}%";
        }
    }
}