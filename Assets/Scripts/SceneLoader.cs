using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Tooltip("Exact name of the scene to load")]
    public string sceneName;

    // Call this using the Inspector field above
    public void LoadConfiguredScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    // Or pass the scene name directly inside the UnityEvent
    public void LoadSceneByName(string targetScene)
    {
        SceneManager.LoadScene(targetScene);
    }
}
