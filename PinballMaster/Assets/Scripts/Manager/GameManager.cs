using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private GameObject resultUIObject;

    [Header("Result")]
    [SerializeField] private float defeatResultDelay = 1f;

    private bool IsPaused;
    private bool IsRestarting;
    private bool IsGameEnded;
    private bool IsVictory;

    private ResultUI resultUI;
    private Coroutine resultCoroutine;

    #region Get Set

    public bool Get_IsPaused() { return IsPaused; }
    public bool Get_IsVictory() { return IsVictory; }
    public bool Get_IsGameEnded() { return IsGameEnded; }


    #endregion Get Set

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Time.timeScale = 1f;

        if (pauseUI != null)
            pauseUI.SetActive(false);

        if (resultUIObject != null)
        {
            resultUI = resultUIObject.GetComponent<ResultUI>();
            resultUIObject.SetActive(false);
        }
    }

    public void Pause()
    {
        if (IsPaused)
            return;

        IsPaused = true;
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        if (!IsPaused || IsGameEnded)
            return;

        IsPaused = false;
        Time.timeScale = 1f;

        if (pauseUI != null)
            pauseUI.SetActive(false);
    }

    public void Restart()
    {
        if (IsRestarting)
            return;

        IsRestarting = true;
        IsPaused = false;

        Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void OnClickedPauseButton()
    {
        if (IsGameEnded)
            return;

        Pause();

        if (pauseUI != null)
            pauseUI.SetActive(true);
    }

    public void Victory()
    {
        if (IsGameEnded)
            return;

        IsGameEnded = true;
        IsVictory = true;

        ShowResult();
    }

    public void Defeat()
    {
        if (IsGameEnded)
            return;


        IsGameEnded = true;
        IsVictory = false;

        if (LevelManager.Instance != null)
            LevelManager.Instance.CancelLevelUp();


        if (resultCoroutine != null)
            StopCoroutine(resultCoroutine);

        resultCoroutine = StartCoroutine(DefeatRoutine());
    }

    private IEnumerator DefeatRoutine()
    {
        yield return new WaitForSeconds(defeatResultDelay);

    
        ShowResult();

        resultCoroutine = null;
    }

    private void ShowResult()
    {
        if (resultUIObject != null)
        {
            resultUIObject.SetActive(true);

            if (resultUI == null)
                resultUI = resultUIObject.GetComponent<ResultUI>();

            if (resultUI != null)
                resultUI.Init_Image(IsVictory);
        }
        else
        {
            Debug.LogError("Result UI Object is not assigned.");
        }

        Pause();
    }

    public void OnClickedQuitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }



    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        Time.timeScale = 1f;
    }
}