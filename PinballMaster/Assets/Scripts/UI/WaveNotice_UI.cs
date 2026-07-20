using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaveNotice_UI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject noticePanel;
    [SerializeField] private Image backImg;
    [SerializeField] private TMP_Text waveNumber;
    [SerializeField] private TMP_Text timeText;

    [Header("Text")]
    [SerializeField] private string wavePrefix = "WAVE";

    [Header("Wave Animation")]
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float maxScale = 1.25f;
    [SerializeField] private float scaleDuration = 0.18f;
    [SerializeField] private int scaleRepeatCount = 2;

    private Coroutine countdownCoroutine;
    private Coroutine scaleCoroutine;
    private bool isSubscribed;

    private Vector3 waveNumberBaseScale;
    private Vector3 backImgBaseScale;

    private void Awake()
    {
        if (noticePanel == null)
        {
            Debug.LogError("WaveNotice_UI: noticePanel is not assigned.");
            return;
        }

        if (waveNumber != null)
            waveNumberBaseScale = waveNumber.transform.localScale;

        if (backImg != null)
            backImgBaseScale = backImg.transform.localScale;

        noticePanel.SetActive(false);
    }

    private void OnEnable()
    {
        SubscribeWaveEvent();
    }

    private void Start()
    {
        SubscribeWaveEvent();
    }

    private void OnDisable()
    {
        UnsubscribeWaveEvent();
        StopAllNoticeCoroutines();
    }

    private void SubscribeWaveEvent()
    {
        if (isSubscribed || WaveManager.Instance == null)
            return;

        WaveManager.Instance.OnNextWaveNotice += ShowNextWaveNotice;
        WaveManager.Instance.OnNextWaveNoticeEnd += HideNotice;

        isSubscribed = true;
    }

    private void UnsubscribeWaveEvent()
    {
        if (!isSubscribed)
            return;

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnNextWaveNotice -= ShowNextWaveNotice;
            WaveManager.Instance.OnNextWaveNoticeEnd -= HideNotice;
        }

        isSubscribed = false;
    }

    private void ShowNextWaveNotice(int nextWaveNumber, float delay)
    {
        StopAllNoticeCoroutines();

        noticePanel.SetActive(true);
        ResetScale();

        if (waveNumber != null)
            waveNumber.text = $"{wavePrefix} {nextWaveNumber}";

        if (timeText != null)
            timeText.text = Mathf.CeilToInt(delay).ToString();

        scaleCoroutine = StartCoroutine(ScaleAnimationRoutine());
        countdownCoroutine = StartCoroutine(CountdownRoutine(delay));
    }

    private IEnumerator CountdownRoutine(float delay)
    {
        float endTime = Time.time + delay;

        while (Time.time < endTime)
        {
            float remainingTime = endTime - Time.time;
            int displaySecond = Mathf.CeilToInt(remainingTime);

            if (timeText != null)
                timeText.text = displaySecond.ToString();

            yield return null;
        }

        if (timeText != null)
            timeText.text = "0";

        countdownCoroutine = null;
    }

    private IEnumerator ScaleAnimationRoutine()
    {
        for (int repeat = 0; repeat < scaleRepeatCount; repeat++)
        {
            yield return ScaleBoth(normalScale, maxScale);
            yield return ScaleBoth(maxScale, normalScale);
        }

        ResetScale();
        scaleCoroutine = null;
    }

    private IEnumerator ScaleBoth(float startMultiplier, float endMultiplier)
    {
        float elapsedTime = 0f;

        while (elapsedTime < scaleDuration)
        {
            elapsedTime += Time.deltaTime;

            float ratio = Mathf.Clamp01(elapsedTime / scaleDuration);
            float multiplier = Mathf.Lerp(startMultiplier, endMultiplier, ratio);

            ApplyScale(multiplier);

            yield return null;
        }

        ApplyScale(endMultiplier);
    }

    private void ApplyScale(float multiplier)
    {
        if (waveNumber != null)
            waveNumber.transform.localScale = waveNumberBaseScale * multiplier;

        if (backImg != null)
            backImg.transform.localScale = backImgBaseScale * multiplier;
    }

    private void ResetScale()
    {
        if (waveNumber != null)
            waveNumber.transform.localScale = waveNumberBaseScale;

        if (backImg != null)
            backImg.transform.localScale = backImgBaseScale;
    }

    private void StopAllNoticeCoroutines()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
    }

    private void HideNotice()
    {
        StopAllNoticeCoroutines();
        ResetScale();

        if (timeText != null)
            timeText.text = string.Empty;

        if (noticePanel != null)
            noticePanel.SetActive(false);
    }
}