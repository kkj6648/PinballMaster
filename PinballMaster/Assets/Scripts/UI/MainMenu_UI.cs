using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu_UI : MonoBehaviour
{


    [SerializeField] private Image titleImg;
    [SerializeField] private TMP_Text mainText;



    [SerializeField] private float scaleSpeed = 2f;
    [SerializeField] private float scaleAmount = 0.08f;



    [Header("Text Fade")]
    [SerializeField] private float fadeSpeed = 1.5f;
    [SerializeField] private float minAlpha = 0.3f;


    private Vector3 originalScale;

    private void Start()
    {
        originalScale = titleImg.rectTransform.localScale;
    }

    private void Update()
    {
        TitleBounce();
        TextFade();
    }

    private void TitleBounce()
    {
        float scale = 1f + Mathf.Sin(Time.time * scaleSpeed) * scaleAmount;
        titleImg.rectTransform.localScale = originalScale * scale;
    }

    private void TextFade()
    {
        Color color = mainText.color;

 
        color.a = Mathf.Lerp(minAlpha, 1f, (Mathf.Sin(Time.time * fadeSpeed) + 1f) * 0.5f);
        mainText.color = color;
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
