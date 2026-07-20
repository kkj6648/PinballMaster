using System.Collections;
using TMPro;
using UnityEngine;

public class DamageEffect : MonoBehaviour
{
    [Header("Component")]
    [SerializeField] private TMP_Text damageText;

    [Header("Object Type")]
    [SerializeField] private Object_Type objectType;

    [Header("Position")]
    [SerializeField] private Vector2 randomOffsetRange = new Vector2(0.25f, 0.15f);
    [SerializeField] private float moveUpDistance = 0.5f;

    [Header("Animation")]
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private float maxScale = 1.35f;
    [SerializeField] private float scaleUpRatio = 0.35f;

    [Header("Color")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color criticalColor = Color.red;

    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "Effect";
    [SerializeField] private int sortingOrder = 100;

    private MeshRenderer textRenderer;
    private Coroutine animationCoroutine;

    private void Awake()
    {
        if (damageText == null)
            damageText = GetComponent<TMP_Text>();

        textRenderer = GetComponent<MeshRenderer>();

        if (textRenderer != null)
        {
            textRenderer.sortingLayerName = sortingLayerName;
            textRenderer.sortingOrder = sortingOrder;
        }
    }

    public void Init(int damage, Vector3 targetPosition, bool isCritical)
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        float randomX = Random.Range(-randomOffsetRange.x, randomOffsetRange.x);
        float randomY = Random.Range(-randomOffsetRange.y, randomOffsetRange.y);

        transform.position = targetPosition + new Vector3(randomX, randomY, 0f);
        transform.localScale = Vector3.zero;

        damageText.text = isCritical ? $"{damage}!" : damage.ToString();
        damageText.color = isCritical ? criticalColor : normalColor;

        if (textRenderer != null)
        {
            textRenderer.sortingLayerName = sortingLayerName;
            textRenderer.sortingOrder = sortingOrder;
        }

        gameObject.SetActive(true);
        animationCoroutine = StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + Vector3.up * moveUpDistance;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float ratio = Mathf.Clamp01(elapsedTime / duration);

            transform.position = Vector3.Lerp(startPosition, endPosition, ratio);

            float scale;

            if (ratio < scaleUpRatio)
            {
                float scaleRatio = ratio / scaleUpRatio;
                scale = Mathf.Lerp(0f, maxScale, scaleRatio);
            }
            else
            {
                float scaleRatio = (ratio - scaleUpRatio) / (1f - scaleUpRatio);
                scale = Mathf.Lerp(maxScale, 0f, scaleRatio);
            }

            transform.localScale = Vector3.one * scale;

            yield return null;
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        animationCoroutine = null;
        transform.localScale = Vector3.one;

        if (PoolManager.Instance != null)
            PoolManager.Instance.Return_Object(objectType, gameObject);
        else
            gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (animationCoroutine == null)
            return;

        StopCoroutine(animationCoroutine);
        animationCoroutine = null;
    }
}