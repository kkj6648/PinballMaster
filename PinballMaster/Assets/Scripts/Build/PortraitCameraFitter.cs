using UnityEngine;

public class PortraitCameraFitter : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float referenceAspect = 9f / 16f;
    [SerializeField] private float referenceOrthographicSize = 10f;

    private int lastWidth;
    private int lastHeight;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        ApplyCameraSize();
    }

    private void Update()
    {
        if (Screen.width == lastWidth && Screen.height == lastHeight)
            return;

        ApplyCameraSize();
    }

    private void ApplyCameraSize()
    {
        if (targetCamera == null || !targetCamera.orthographic)
            return;

        float currentAspect = (float)Screen.width / Screen.height;

        if (currentAspect < referenceAspect)
            targetCamera.orthographicSize = referenceOrthographicSize * referenceAspect / currentAspect;
        else
            targetCamera.orthographicSize = referenceOrthographicSize;

        lastWidth = Screen.width;
        lastHeight = Screen.height;
    }
}