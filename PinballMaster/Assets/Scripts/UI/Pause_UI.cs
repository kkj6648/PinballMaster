using UnityEngine;
using UnityEngine.UI;

public class Pause_UI : MonoBehaviour
{
    [SerializeField] Button continueButton;
    [SerializeField] Button restartButton;
    [SerializeField] Button quitButton;



    public void OnClickedContinueButton()
    {
        MyPlayer player = FindFirstObjectByType<MyPlayer>();

        player.BlockAimUntilPointerRelease();

        GameManager.Instance.Resume();
        gameObject.SetActive(false);

    }

    public void OnClickedRestartButton()
    {
        GameManager.Instance.Restart();
    }

    public void OnClickedQuitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }



}
