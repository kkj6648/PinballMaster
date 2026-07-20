using UnityEngine;
using UnityEngine.UI;

public class ResultUI : MonoBehaviour
{
    [SerializeField] private Image resultImg;
    [SerializeField] private Sprite[] sprites;

    [Header("Pulse")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseScale = 0.1f;

    private Vector3 originalScale;

    private void Awake()
    {
        if (resultImg != null)
            originalScale = resultImg.rectTransform.localScale;
    }

    private void Update()
    {
        Pulse();
    }

    public void Init_Image(bool victory)
    {
        if (resultImg == null)
            return;

        if (sprites == null || sprites.Length < 2)
            return;

        resultImg.sprite = victory ? sprites[0] : sprites[1];
    }

    private void Pulse()
    {
        if (resultImg == null)
            return;

        float scale = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseScale;
        resultImg.rectTransform.localScale = originalScale * scale;
    }
}