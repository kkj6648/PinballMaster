using UnityEngine.SceneManagement;

public static class SceneLoader
{
    private static string nextSceneName;

    public static void LoadScene(string sceneName)
    {
        nextSceneName = sceneName;

        SceneManager.LoadScene("Loading");
    }

    public static string GetNextSceneName()
    {
        return nextSceneName;
    }

}