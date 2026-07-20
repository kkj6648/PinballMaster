using UnityEngine;

public class SafeAreaFitter : MonoBehaviour
{
    [SerializeField] private RectTransform safeAreaPanel;

    private Rect lastSafeArea;
    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Awake()
    {
        if (safeAreaPanel == null)
            safeAreaPanel = GetComponent<RectTransform>();

        ApplySafeArea();
    }

    private void Update()
    {
        if (lastSafeArea == Screen.safeArea && lastScreenWidth == Screen.width && lastScreenHeight == Screen.height)
            return;

        ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        safeAreaPanel.anchorMin = anchorMin;
        safeAreaPanel.anchorMax = anchorMax;
        safeAreaPanel.offsetMin = Vector2.zero;
        safeAreaPanel.offsetMax = Vector2.zero;

        lastSafeArea = safeArea;
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }
}