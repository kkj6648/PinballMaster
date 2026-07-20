using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainSceneManager : MonoBehaviour
{
    [Header("Object")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform player;
    [SerializeField] private GameObject mainUI;
    [SerializeField] private GameObject magicCircle;
    [SerializeField] private GameObject QuitButton;

    [Header("Camera")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 0, -10);
    [SerializeField] private float targetSize = 3f;
    [SerializeField] private float moveDuration = 2f;

    [Header("Animation")]
    [SerializeField] private Animator playerAnimator;
    private string animationHash = "IsStart";

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private float fadeDuration = 1f;



    private bool isStart = false;

    private void Start()
    {
        fadeGroup.alpha = 0f;
    }

    private void Update()
    {
        if (isStart)
            return;

    }

    public void OnClickedStartButton()
    {

        isStart = true;
        StartCoroutine(StartSequence());
    }


    IEnumerator StartSequence()
    {
        mainUI.SetActive(false);
        QuitButton.SetActive(false);

        // Zoom
        yield return StartCoroutine(CameraMove());

        // Stop
        yield return new WaitForSeconds(0.3f);

        // Anim
        playerAnimator.SetBool(animationHash, true);
        magicCircle.SetActive(true);

        yield return new WaitForSeconds(1f);
      
        // Up move
        StartCoroutine(CameraMoveUp());
        // Fade out
        yield return StartCoroutine(FadeOut());

        yield return new WaitForSeconds(1f);

        // NextScene
        SceneLoader.LoadScene("InGame");

    }

    IEnumerator CameraMove()
    {
        Vector3 startPos = mainCamera.transform.position;
        Vector3 endPos = player.position + cameraOffset;

        float startSize = mainCamera.orthographicSize;

        mainUI.gameObject.SetActive(false);


        float t = 0;

        while (t < moveDuration)
        {
            t += Time.deltaTime;

            float lerp = Mathf.SmoothStep(0, 1, t / moveDuration);

            mainCamera.transform.position = Vector3.Lerp(startPos, endPos, lerp);
            mainCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, lerp);

            yield return null;
        }

        mainCamera.transform.position = endPos;
        mainCamera.orthographicSize = targetSize;
    }
    IEnumerator CameraMoveUp()
    {
        Vector3 start = mainCamera.transform.position;
        Vector3 end = start + Vector3.up * 6f;

        float duration = 2f;
        float t = 0;

        while (t < duration)
        {
            t += Time.deltaTime;

            float lerp = Mathf.SmoothStep(0, 1, t / duration);

            mainCamera.transform.position =
                Vector3.Lerp(start, end, lerp);

            yield return null;
        }

        mainCamera.transform.position = end;
    }


    IEnumerator FadeOut()
    {
        float t = 0;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;

            fadeGroup.alpha = Mathf.Lerp(0, 1, t / fadeDuration);

            yield return null;
        }

        fadeGroup.alpha = 1f;
    }

}