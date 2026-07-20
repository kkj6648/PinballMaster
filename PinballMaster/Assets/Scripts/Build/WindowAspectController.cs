using UnityEngine;

public class AspectRatioWindowResizer : MonoBehaviour
{
    public static AspectRatioWindowResizer Instance { get; private set; }

    [SerializeField] private int minWidth = 360;
    [SerializeField] private int minHeight = 640;
    [SerializeField] private float targetAspect = 9f / 16f;
    [SerializeField] private float resizeDelay = 0.15f;

    private int lastWidth;
    private int lastHeight;
    private float resizeTimer;
    private bool needResize;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;
    }

    private void Update()
    {

        // For windows
#if UNITY_STANDALONE_WIN  && !UNITY_EDITOR

        if (Screen.fullScreenMode != FullScreenMode.Windowed)
            return;

        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            resizeTimer = resizeDelay;
            needResize = true;
        }

        if (!needResize)
            return;

        resizeTimer -= Time.unscaledDeltaTime;

        if (resizeTimer > 0f)
            return;

        needResize = false;
        ApplyAspectRatio();
    
#endif

    }



    private void ApplyAspectRatio()
    {
        int width = Mathf.Max(Screen.width, minWidth);
        int height = Mathf.Max(Screen.height, minHeight);
        float currentAspect = (float)width / height;

        if (currentAspect > targetAspect)
            width = Mathf.RoundToInt(height * targetAspect);
        else
            height = Mathf.RoundToInt(width / targetAspect);

        width = Mathf.Max(width, minWidth);
        height = Mathf.Max(height, minHeight);

        lastWidth = width;
        lastHeight = height;

        Screen.SetResolution(width, height, FullScreenMode.Windowed);
    }

    
 }