using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image loadingBar;
    [SerializeField] private TMP_Text loadingText;

    [Header("Setting")]
    [SerializeField] private float minimumLoadingTime = 2f;
    [SerializeField] private float progressLerpSpeed = 8f;

    private float displayProgress;

    private void Start()
    {
        string nextSceneName = SceneLoader.GetNextSceneName();

        if (string.IsNullOrWhiteSpace(nextSceneName))
            return;

        displayProgress = 0f;
        UpdateLoadingUI(0f);

        StartCoroutine(LoadSceneCoroutine(nextSceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        float elapsedTime = 0f;

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        if (operation == null)
        {
            Debug.LogError($"Scene load failed: {sceneName}");
            yield break;
        }

        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float loadProgress = Mathf.Clamp01(operation.progress / 0.9f);
            float timeProgress = Mathf.Clamp01(elapsedTime / minimumLoadingTime);

            float targetProgress = Mathf.Min(loadProgress, timeProgress);

            displayProgress = Mathf.Lerp(
                displayProgress,
                targetProgress,
                progressLerpSpeed * Time.unscaledDeltaTime
            );

            UpdateLoadingUI(displayProgress);

            bool loadCompleted = operation.progress >= 0.9f;
            bool minimumTimePassed = elapsedTime >= minimumLoadingTime;
            bool displayCompleted = displayProgress >= 0.995f;

            if (loadCompleted && minimumTimePassed && displayCompleted)
            {
                UpdateLoadingUI(1f);
                yield return null;

                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    private void UpdateLoadingUI(float progress)
    {
        if (loadingBar != null)
            loadingBar.fillAmount = progress;

        if (loadingText != null)
        {
            int percentage = Mathf.RoundToInt(progress * 100f);
            loadingText.text = $"{percentage}%";
        }
    }
}